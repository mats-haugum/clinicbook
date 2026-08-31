#!/usr/bin/env bash
# Pulls the latest commit on the deployed branch and rebuilds this project's
# containers. Triggered by the shared webhook listener (deploy/edge/webhook/)
# on every push, but safe to run by hand too.
set -euo pipefail

# EDIT THIS to wherever this repo is actually checked out on the server.
REPO_DIR="/opt/apps/ep-2-Delvjn"

# Must match the branch the webhook gates on in deploy/edge/webhook/hooks.json
# (refs/heads/main) and the branch CI runs against in .github/workflows/ci.yml.
BRANCH="main"

cd "$REPO_DIR"

git fetch origin "$BRANCH"

# Hard reset rather than `git pull`, so a force-push or a diverged local
# state (e.g. someone edited .env by hand, which is gitignored and untouched
# by this) can never leave the server stuck on a broken merge.
git reset --hard "origin/$BRANCH"

docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build

echo "$(date -Iseconds) deployed $(git rev-parse --short HEAD)" >> /var/log/clinicbook-deploy.log
