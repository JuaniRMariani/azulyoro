#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

if [[ "$(id -u)" -ne 0 ]]; then
    echo "azulyoro-autodeploy must run as root." >&2
    exit 1
fi

readonly repo_dir=/var/www/azulyoro
readonly lock_file=/run/lock/azulyoro-autodeploy.lock
readonly deployed_revision_file=/var/lib/azulyoro/deployed-revision

exec 9>"$lock_file"
flock -n 9 || exit 0

log() { printf '[azulyoro-autodeploy] %s\n' "$*"; }

cd "$repo_dir"

if [[ -n "$(git status --porcelain)" ]]; then
    log "refusing automatic deployment: working tree has local changes"
    exit 1
fi

git fetch --quiet --prune origin main
local_revision="$(git rev-parse HEAD)"
remote_revision="$(git rev-parse origin/main)"

if [[ "$local_revision" != "$remote_revision" ]]; then
    git merge --ff-only origin/main
    local_revision="$(git rev-parse HEAD)"
fi

deployed_revision="$(cat "$deployed_revision_file" 2>/dev/null || true)"
if [[ "$local_revision" == "$deployed_revision" ]]; then
    log "already deployed $local_revision"
    exit 0
fi

log "deploying $local_revision"
/usr/local/sbin/azulyoro-deploy "$local_revision"

install -d -o root -g root -m 0750 "$(dirname "$deployed_revision_file")"
revision_tmp="$(mktemp "${deployed_revision_file}.XXXXXX")"
trap 'rm -f -- "$revision_tmp"' EXIT
printf '%s\n' "$local_revision" > "$revision_tmp"
chown root:root "$revision_tmp"
chmod 0640 "$revision_tmp"
mv -f -- "$revision_tmp" "$deployed_revision_file"
trap - EXIT
log "recorded deployed revision $local_revision"
