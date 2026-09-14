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
4. If it should auto-deploy, follow the webhook setup below and add
   its own entry to `webhook/hooks.json`.

## Webhook auto-deploy

GitHub can notify the server when a CI run finishes, instead of the server
polling. Deploys are gated on CI: the hook fires on the `workflow_run` event
and `hooks.json` only runs `redeploy.sh` when the `CI` workflow completed
successfully for a push to `main`. The tested commit's hash
(`workflow_run.head_sha`) is passed to the script, so the server deploys
exactly what CI tested, never a newer untested commit.

One listener (the `webhook` binary by adnanh, packaged for
Debian/Ubuntu) serves every project, each at its own URL path
(`/hooks/<project-id>`), each with its own secret. It runs **on the host**,
not in a container - see `webhook/webhook.service.example` for why.

Server-specific values (checkout path, service account, log location) are
deliberately kept out of this repo, which is public. They live in
`/opt/webhook/webhook.env` and in the copy of the unit file under
`/etc/systemd/system/`; the versions committed here are `.example` templates.

### 1. Install and configure

Pick the account the service will run as first. It has to own this checkout
(`redeploy.sh` does a `git reset --hard` and a Docker build inside it) and be
in the `docker` group. The account that already owns the clone is the least
fuss; a dedicated `deploy` user is tighter, but then the checkout has to live
somewhere that user can traverse and write, which a home directory is not.

```bash
sudo apt update && sudo apt install -y webhook

sudo mkdir -p /opt/webhook
sudo cp webhook/hooks.json /opt/webhook/hooks.json
sudo cp webhook/webhook.env.example /opt/webhook/webhook.env
sudo chmod 600 /opt/webhook/webhook.env
sudoedit /opt/webhook/webhook.env    # real secret + CLINICBOOK_REPO_DIR

sudo cp webhook/webhook.service.example /etc/systemd/system/webhook.service
sudoedit /etc/systemd/system/webhook.service   # set User=/Group= to that account

sudo systemctl daemon-reload
sudo systemctl enable --now webhook
systemctl status webhook
```

The deploy script just needs to be executable - it works out its own location,
so there is nothing to configure in it:

```bash
chmod +x deploy/redeploy.sh
```

`redeploy.sh` writes a one-line record per deploy to `<repo>/deploy/deploy.log`
(gitignored). Point `DEPLOY_LOG` in `webhook.env` somewhere else if you would
rather it lived outside the checkout; that file must be writable by the
service account.

### 2. Configure the webhook in GitHub

Repo → **Settings** → **Webhooks** → **Add webhook**:

- Payload URL: `https://app.matshaugum.com/hooks/clinicbook`
- Content type: `application/json`
- Secret: the same value as `WEBHOOK_SECRET_CLINICBOOK` in `webhook.env`
- Which events: **Let me select individual events** → tick **Workflow runs**
  only (untick **Pushes**)

GitHub signs every payload with that secret (`X-Hub-Signature-256`); `webhook`
verifies it before running anything, so a request without a valid signature
never touches `redeploy.sh`.

### What a deploy looks like

```
git push ─► CI runs ─► workflow_run webhook ─► hooks.json rules ─► redeploy.sh <sha>
```

Every push to `main` produces **three** `workflow_run` deliveries:

| Delivery (`action`) | When | Listener log |
|---|---|---|
| `requested` | right after the push | `trigger rules were not satisfied` (expected) |
| `in_progress` | a few seconds later | `trigger rules were not satisfied` (expected) |
| `completed` | when CI finishes | CI passed: `hook triggered successfully` → `executing .../redeploy.sh [<sha>]`. CI failed: `trigger rules were not satisfied`, nothing deploys |

On success `deploy/deploy.log` gets a `deployed <short-sha>` line matching the
pushed commit. `redeploy.sh <sha>` also guards the hash it is given:

- It refuses anything that isn't a 40-character hash, or a commit not on
  `origin/main`.
- If two pushes' CI runs finish out of order, the older one logs
  `skipped <sha> (already deployed or older)` instead of rolling the site back.

### Troubleshooting

- **Watch deliveries arrive:** `journalctl -u webhook -f`. GitHub's side is
  under Settings → Webhooks → Recent Deliveries, which also has **Redeliver**.
- **Every delivery is rejected, even after green CI:** check the rule values
  in `hooks.json` still match reality. The workflow must be named exactly `CI`
  (`name:` in `.github/workflows/ci.yml`), so renaming it silently stops
  deploys. The GitHub hook must send **Workflow runs** with Content type
  `application/json`; a form-encoded body fails every rule while the
  signature still validates.
- **Changed `hooks.json` in the repo but nothing changed:** deploys update the
  repo copy, never the live `/opt/webhook/hooks.json`. Copy it over (see the
  diff warning below) and `sudo systemctl restart webhook`.
- **`Hook not found` on every request:** the hooks file failed to load. Check
  `journalctl -u webhook | grep -E 'loaded|couldn'`.

### 3. Adding another project's hook

- Add its own `deploy/redeploy.sh` in that project's repo.
- Add an entry to `webhook/hooks.json` with a new `id`, its script path, and
  a new ``{{ getenv `WEBHOOK_SECRET_...` }}`` reference. Use backticks, not
  `"`: JSON forces `\"` escaping, which Go templates reject, and the whole
  file then fails to load.
- Add the matching `WEBHOOK_SECRET_...` line to `/opt/webhook/webhook.env`.
- Update the live `/opt/webhook/hooks.json`. It is shared by every project, so
  `diff` it against your copy first and only `sudo cp` if the diff touches
  nothing but your entry; otherwise `sudoedit` just that entry.
- `sudo systemctl restart webhook`.
- Register the new webhook in that project's GitHub repo, pointing at
  `/hooks/<its-id>`.
