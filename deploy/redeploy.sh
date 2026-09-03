#!/usr/bin/env bash
# Pulls the latest commit on the deployed branch and rebuilds this project's
# containers. Triggered by the shared webhook listener (deploy/edge/webhook/)
# on every push, but safe to run by hand too.
set -euo pipefail

# Derived from this script's own location rather than hardcoded, so the
# checkout path stays out of this public repo and the script works wherever
# it is cloned. `git reset --hard` below would clobber a hardcoded path
# anyway on the first deploy after someone edits it.
REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Must match the branch the webhook gates on in deploy/edge/webhook/hooks.json
# (refs/heads/main) and the branch CI runs against in .github/workflows/ci.yml.
BRANCH="${DEPLOY_BRANCH:-main}"

# Overridable via webhook.env so the server's log location isn't published
# here either. Defaults inside the repo, which always exists and is writable
# by whoever runs the deploy.
DEPLOY_LOG="${DEPLOY_LOG:-$REPO_DIR/deploy/deploy.log}"

cd "$REPO_DIR"

git fetch origin "$BRANCH"

# Hard reset rather than `git pull`, so a force-push or a diverged local
# state (e.g. someone edited .env by hand, which is gitignored and untouched
# by this) can never leave the server stuck on a broken merge.
git reset --hard "origin/$BRANCH"

docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build

echo "$(date -Iseconds) deployed $(git rev-parse --short HEAD)" >> "$DEPLOY_LOG"
