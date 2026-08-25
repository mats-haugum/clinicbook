#!/usr/bin/env bash
#
# Nightly MSSQL backup. Run from cron on the host:
#   0 3 * * * /path/to/deploy/backup.sh >> /var/log/appbackup.log 2>&1
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# shellcheck disable=SC1091
source .env

STAMP="$(date +%Y%m%d-%H%M%S)"
BAK_NAME="${DB_NAME}-${STAMP}.bak"
LOCAL_DIR="${SCRIPT_DIR}/backups"
RETENTION_DAYS=14

# Set this to an rclone remote to enable offsite copies, e.g. "b2:my-bucket/db"
# Leave empty to keep local-only backups.
RCLONE_REMOTE="${RCLONE_REMOTE:-}"

mkdir -p "$LOCAL_DIR"

echo "[$(date -Is)] starting backup of ${DB_NAME}"

docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd \
	-S localhost -U sa -P "$SA_PASSWORD" -C \
	-Q "BACKUP DATABASE [${DB_NAME}] TO DISK = N'/backups/${BAK_NAME}' WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10"

if [ ! -f "${LOCAL_DIR}/${BAK_NAME}" ]; then
	echo "[$(date -Is)] ERROR: backup file was not created" >&2
	exit 1
fi

SIZE="$(du -h "${LOCAL_DIR}/${BAK_NAME}" | cut -f1)"
echo "[$(date -Is)] wrote ${BAK_NAME} (${SIZE})"

if [ -n "$RCLONE_REMOTE" ]; then
	echo "[$(date -Is)] syncing to ${RCLONE_REMOTE}"
	rclone copy "${LOCAL_DIR}/${BAK_NAME}" "$RCLONE_REMOTE" --stats-one-line
fi

echo "[$(date -Is)] pruning local backups older than ${RETENTION_DAYS} days"
find "$LOCAL_DIR" -name "${DB_NAME}-*.bak" -type f -mtime "+${RETENTION_DAYS}" -delete

echo "[$(date -Is)] done"
