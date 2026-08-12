#!/usr/bin/env bash
# Daily Postgres backup for azulyoro, copied off-box.
set -euo pipefail

STAMP="$(date -u +%Y%m%d-%H%M%S)"
OUT="/var/backups/azulyoro/azulyoro-${STAMP}.sql.gz"
mkdir -p "$(dirname "$OUT")"

PGPASSWORD="${PGPASSWORD:?set PGPASSWORD}" pg_dump -h 127.0.0.1 -U azulyoro azulyoro | gzip > "$OUT"

# Ship off-box (configure destination): e.g. rclone/scp/s3.
# rclone copy "$OUT" remote:azulyoro-backups/

# Retain last 14 local dumps.
ls -1t /var/backups/azulyoro/*.sql.gz | tail -n +15 | xargs -r rm -f
echo "backup written: $OUT"
