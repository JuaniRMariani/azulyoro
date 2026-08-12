#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

: "${PGHOST:?PGHOST is required}"
: "${PGPORT:?PGPORT is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${BACKUP_REMOTE:?BACKUP_REMOTE is required for off-box backup}"

RCLONE_BIN="${RCLONE_BIN:-/usr/bin/rclone}"
[[ -x "$RCLONE_BIN" ]] || { echo "rclone not found: $RCLONE_BIN" >&2; exit 1; }

backup_dir=/var/backups/azulyoro
install -d -m 700 "$backup_dir"
stamp="$(date -u +%Y%m%d-%H%M%S)"
out="$backup_dir/azulyoro-${stamp}.sql.gz"

pg_dump --no-owner --no-privileges --format=plain |
    gzip --best > "$out"

"$RCLONE_BIN" copy "$out" "$BACKUP_REMOTE"

mapfile -t old_dumps < <(
    find "$backup_dir" -maxdepth 1 -type f -name 'azulyoro-*.sql.gz' -printf '%T@ %p\n' |
        sort -nr | awk 'NR > 14 { $1=""; sub(/^ /, ""); print }'
)
for dump in "${old_dumps[@]}"; do
    case "$dump" in
        "$backup_dir"/azulyoro-*.sql.gz) rm -f -- "$dump" ;;
    esac
done

echo "backup written and copied off-box: $out"
