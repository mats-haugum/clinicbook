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
