#!/usr/bin/env bash
# Checks out a commit on the deployed branch and rebuilds this project's
# containers. Triggered by the shared webhook listener (deploy/edge/webhook/)
# when the CI workflow succeeds on main, but safe to run by hand too.
#
# Usage:
#   redeploy.sh <sha>   deploy that exact commit (what the webhook passes -
#                       the commit CI actually tested)
#   redeploy.sh         deploy the latest commit on the branch (manual use)
set -euo pipefail

# Derived from this script's own location rather than hardcoded, so the
# checkout path stays out of this public repo and the script works wherever
# it is cloned. `git reset --hard` below would clobber a hardcoded path
# anyway on the first deploy after someone edits it.
REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Must match the branch the webhook gates on in deploy/edge/webhook/hooks.json
# (workflow_run.head_branch == main) and the branch CI runs against in
# .github/workflows/ci.yml.
BRANCH="${DEPLOY_BRANCH:-main}"

# ${1:-} is the first argument, or an empty string if none was given. The
# `:-` default matters because `set -u` above makes an unset $1 a fatal error.
TARGET_SHA="${1:-}"

# Overridable via webhook.env so the server's log location isn't published
# here either. Defaults inside the repo, which always exists and is writable
# by whoever runs the deploy.
DEPLOY_LOG="${DEPLOY_LOG:-$REPO_DIR/deploy/deploy.log}"

cd "$REPO_DIR"

git fetch origin "$BRANCH"

if [[ -z "$TARGET_SHA" ]]; then
  # No argument: manual run, take whatever is newest on the branch.
  TARGET_SHA="$(git rev-parse "origin/$BRANCH")"
else
  # The payload is HMAC-verified, but this value still ends up in git
  # commands, so only accept a full 40-character hex commit hash.
  if [[ ! "$TARGET_SHA" =~ ^[0-9a-f]{40}$ ]]; then
    echo "refusing to deploy: '$TARGET_SHA' is not a commit hash" >&2
    exit 1
  fi

  # --is-ancestor exits 0 if the first commit is part of the second's history.
  # This makes sure the commit really is on the deployed branch.
  if ! git merge-base --is-ancestor "$TARGET_SHA" "origin/$BRANCH"; then
    echo "refusing to deploy: $TARGET_SHA is not on origin/$BRANCH" >&2
    exit 1
  fi

  # Two quick pushes start two CI runs, and they can finish in either order.
  # If the server is already on this commit or a newer one, deploying this
  # older one would roll the site back, so skip it.
  if git merge-base --is-ancestor "$TARGET_SHA" HEAD; then
    echo "$(date -Iseconds) skipped $(git rev-parse --short "$TARGET_SHA") (already deployed or older)" >> "$DEPLOY_LOG"
    exit 0
  fi
fi

# Hard reset rather than `git pull`, so a force-push or a diverged local
# state (e.g. someone edited .env by hand, which is gitignored and untouched
# by this) can never leave the server stuck on a broken merge.
git reset --hard "$TARGET_SHA"

docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build

echo "$(date -Iseconds) deployed $(git rev-parse --short HEAD)" >> "$DEPLOY_LOG"
