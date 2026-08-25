# Self-hosted deployment: React + ASP.NET Core + MSSQL

Everything runs on one Ubuntu box behind a Cloudflare Tunnel. No ports are
opened on your router, and your home IP is never published in DNS.

## Repository layout

These files assume the following structure. Put the `deploy/` folder at the
repo root, alongside your two existing folders.

```
your-repo/
├── frontend/              your React app
├── backend/               your C# / EF Core app
└── deploy/
    ├── docker-compose.yml
    ├── Caddyfile
    ├── frontend.Dockerfile
    ├── backend.Dockerfile
    ├── backup.sh
    ├── .env.example
    ├── .env               (you create this - never commit it)
    └── edge/              shared reverse proxy + tunnel for ALL projects
                            on the domain - set up once, see edge/README.md
```

Copy `Program.reference.cs` contents into your existing `backend/Program.cs`
rather than replacing the file.

## Request flow

This app is one of potentially several projects hosted under
`app.matshaugum.com/projects/<name>`, so there are two Caddy hops: one shared
"edge" Caddy that routes by path, and this project's own Caddy that serves
its build and proxies its own API.

```
browser
  → Cloudflare edge          TLS, CDN cache, WAF, rate limiting
  → Cloudflare Tunnel        outbound-only connection from your server
  → edge Caddy container     picks a project by URL path, strips the prefix
                              (deploy/edge/ - shared by every project)
  → this project's Caddy     static React files, /api/* proxied onward
  → ASP.NET Core container   Kestrel on :8080
  → MSSQL + Redis            internal Docker network, not reachable externally
```

Because the frontend and API share one origin, there is no CORS configuration
and no cross-subdomain cookie handling to get wrong. Set up `deploy/edge/`
**once** per server (see its own README) before deploying any project below.

## Before you start

`backend.Dockerfile` and `frontend.Dockerfile` are already wired up for this
project (assembly name, target framework, Vite `dist` output). If you copy
this `deploy/` folder into a different project, update those placeholders to
match it.

The frontend reads its API base URL from `VITE_API_URL` (see
`frontend.Dockerfile`) rather than hardcoding `/api`, since this app is
served under a path prefix (`/projects/clinicbook`) - a plain domain-root
`/api/...` fetch would miss the edge Caddy's routing for this project. See
`Frontend/vite.config.ts` and `Frontend/src/main.tsx` for the matching
`base` / `basename` setup.

## 1. Install prerequisites

```bash
sudo apt update && sudo apt install -y docker.io docker-compose-v2 rclone
sudo usermod -aG docker $USER    # log out and back in afterwards
```

## 2. Set up the shared edge stack (once per server)

The Cloudflare Tunnel and its reverse proxy are shared by every project on
the domain, so they live in `deploy/edge/`, not here. Follow
[`deploy/edge/README.md`](edge/README.md) first - it covers creating the
tunnel in the Cloudflare dashboard, the `public-edge` Docker network, and
starting that stack. Its Public Hostname points at `edge-caddy:80`, and the
DNS record it creates for `app.matshaugum.com` is shared by every project too
- you don't repeat this step per project.

## 3. Configure secrets

```bash
cd deploy
cp .env.example .env
openssl rand -base64 24        # use this for SA_PASSWORD
nano .env                      # paste in SA_PASSWORD
chmod 600 .env
```

Confirm `.env` is gitignored:

```bash
echo "deploy/.env" >> ../.gitignore
echo "deploy/backups/" >> ../.gitignore
```

## 4. First run

```bash
docker compose up -d --build
docker compose ps
```

Confirm the edge stack (`deploy/edge/`) is already running from step 2 -
`docker compose -p edge logs -f tunnel` there should show a registered
connection. Visit `https://app.matshaugum.com/projects/clinicbook` -
Cloudflare issues the certificate automatically.

## 5. Apply EF Core migrations

Run migrations explicitly rather than on app startup, so a bad migration can't
take down a running app:

```bash
docker compose exec api dotnet ClinicAppointmentBookingSystem.dll --migrate
```

If your app doesn't support a migrate flag, generate an idempotent SQL script
locally and apply it:

```bash
cd backend
dotnet ef migrations script --idempotent -o migrate.sql
docker compose cp migrate.sql db:/tmp/migrate.sql
docker compose exec db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C -d AppDb -i /tmp/migrate.sql
```

## 6. Backups

```bash
chmod +x backup.sh
./backup.sh                    # verify it works before scheduling
crontab -e
```

Add:

```
0 3 * * * /full/path/to/deploy/backup.sh >> /var/log/appbackup.log 2>&1
```

For offsite copies, configure rclone (`rclone config`) against Backblaze B2 or
S3, then set `RCLONE_REMOTE` in the script or as an environment variable.

**Test a restore at least once.** An untested backup is a guess:

```bash
docker compose exec db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "RESTORE DATABASE [AppDb_test] FROM DISK = N'/backups/AppDb-20260820-030000.bak' WITH MOVE 'AppDb' TO '/var/opt/mssql/data/AppDb_test.mdf', MOVE 'AppDb_log' TO '/var/opt/mssql/data/AppDb_test.ldf', RECOVERY"
```

## 7. Harden the host

Since the tunnel needs no inbound ports, close everything:

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow from 192.168.0.0/16 to any port 22 proto tcp   # LAN SSH only
sudo ufw enable
```

Disable SSH password auth in `/etc/ssh/sshd_config`:

```
PasswordAuthentication no
PermitRootLogin no
```

Then `sudo systemctl restart ssh`. Enable automatic security patches:

```bash
sudo apt install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

You can now remove the port forwarding rules from your TP-Link router entirely.

## 8. Cloudflare edge rules

In the dashboard for `matshaugum.com`:

- **Caching → Cache Rules**: cache `/assets/*` aggressively; bypass cache for `/api/*`
- **Security → WAF → Rate limiting rules**: e.g. 300 requests per minute per IP
  on `/api/*`. This is the layer that protects your home bandwidth, since it
  rejects abuse before it ever reaches the tunnel.
- **Zero Trust → Access**: optionally gate `/swagger` or any admin route behind
  SSO, which is free for small numbers of users.

## Updating

```bash
git pull
docker compose up -d --build
```

To automate, add a GitHub Actions workflow that SSHes in and runs the above, or
build images in CI, push to GHCR, and pull them on the server.

## Notes and gotchas

- **MSSQL RAM**: the container wants ~2 GB. A 4 GB machine is the practical
  floor once Redis and the API are also running.
- **MSSQL Express** caps databases at 10 GB. Developer edition is unlimited and
  free but licensed for non-production use only.
- **sqlcmd path**: newer MSSQL images use `/opt/mssql-tools18/bin/sqlcmd`; older
  ones use `/opt/mssql-tools/bin/sqlcmd`. Adjust if the healthcheck fails.
- **ISP terms**: some residential contracts prohibit running servers. Worth a
  check before you rely on this for anything important.
- **Uptime** is bounded by your home power and internet connection.
