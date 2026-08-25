# Shared edge stack

One Caddy + one Cloudflare Tunnel for the whole domain, shared by every
project. Set this up **once** on the server, before deploying any individual
project's `docker-compose.yml`.

## Why this exists

A Cloudflare Tunnel points at exactly one target. To host multiple projects
on `app.matshaugum.com` under different paths (`/projects/clinicbook`,
`/projects/other-app`, ...), the tunnel points at this shared Caddy, and this
Caddy looks at the request path to decide which project's own container to
forward to.

Individual projects never talk to the tunnel directly, and never need their
own `TUNNEL_TOKEN`.

## Setup

```bash
# 1. Create the shared network. Every project's `web` container and this
#    stack's `caddy` container both join it - this is how edge-caddy can
#    reach e.g. clinicbook-web by name, even though they're separate
#    docker-compose projects.
docker network create public-edge

# 2. Configure the tunnel token.
cd deploy/edge
cp .env.example .env
nano .env
chmod 600 .env

# 3. Start the stack.
docker compose up -d
docker compose logs -f tunnel
```

In the Cloudflare dashboard, the tunnel's Public Hostname should point at:

- Service: `HTTP` → `edge-caddy:80`

(not at any individual project's container).

## Adding a new project

1. Deploy the project as normal (its own `docker-compose.yml`, joining the
   `public-edge` network, with a fixed `container_name`).
2. Add a `handle_path /projects/<name>/*` block to `./Caddyfile` pointing at
   that container name.
3. `docker compose up -d --build` here to reload Caddy with the new route.
4. If it should auto-deploy on push, follow the webhook setup below and add
   its own entry to `webhook/hooks.json`.

## Webhook auto-deploy

GitHub can notify the server the instant you push, instead of the server
polling. One listener (the `webhook` binary by adnanh, packaged for
Debian/Ubuntu) serves every project, each at its own URL path
(`/hooks/<project-id>`), each with its own secret. It runs **on the host**,
not in a container - see `webhook/webhook.service` for why.

### 1. Install and configure

```bash
sudo apt update && sudo apt install -y webhook

# A dedicated, low-privilege user for the service to run as.
sudo useradd --system --create-home --shell /usr/sbin/nologin deploy
sudo usermod -aG docker deploy

sudo mkdir -p /opt/webhook
sudo cp webhook/hooks.json /opt/webhook/hooks.json
sudo cp webhook/webhook.env.example /opt/webhook/webhook.env
sudo nano /opt/webhook/webhook.env      # paste in real secrets
sudo chmod 600 /opt/webhook/webhook.env
sudo chown -R deploy:deploy /opt/webhook

sudo cp webhook/webhook.service /etc/systemd/system/webhook.service
sudo systemctl daemon-reload
sudo systemctl enable --now webhook
sudo systemctl status webhook
```

Each project also needs its deploy script runnable by the `deploy` user and
a writable log file:

```bash
sudo chown deploy:deploy /opt/apps/ep-2-Delvjn/deploy/redeploy.sh
sudo chmod +x /opt/apps/ep-2-Delvjn/deploy/redeploy.sh
sudo touch /var/log/clinicbook-deploy.log
sudo chown deploy:deploy /var/log/clinicbook-deploy.log
```

### 2. Configure the webhook in GitHub

Repo → **Settings** → **Webhooks** → **Add webhook**:

- Payload URL: `https://app.matshaugum.com/hooks/clinicbook`
- Content type: `application/json`
- Secret: the same value as `WEBHOOK_SECRET_CLINICBOOK` in `webhook.env`
- Which events: **Just the push event**

GitHub signs every payload with that secret (`X-Hub-Signature-256`); `webhook`
verifies it before running anything, so a request without a valid signature
never touches `redeploy.sh`.

### 3. Adding another project's hook

- Add its own `deploy/redeploy.sh` in that project's repo.
- Add an entry to `webhook/hooks.json` with a new `id`, its script path, and
  a new `{{ getenv "WEBHOOK_SECRET_..." }}` reference.
- Add the matching `WEBHOOK_SECRET_...` line to `/opt/webhook/webhook.env`.
- `sudo systemctl restart webhook`.
- Register the new webhook in that project's GitHub repo, pointing at
  `/hooks/<its-id>`.
