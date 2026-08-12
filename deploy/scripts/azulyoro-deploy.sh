#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

if [[ "$(id -u)" -ne 0 ]]; then
    echo "azulyoro-deploy must run as root (use sudo)." >&2
    exit 1
fi

app_root=/var/www/azulyoro
repo_root="$app_root"
releases="$app_root/releases"
current="$app_root/current"
api_env=/etc/azulyoro/api.env
web_env=/etc/azulyoro/web.env
expected_sha="${1:-}"
lock_file=/run/lock/azulyoro-deploy.lock

log() { printf '[azulyoro-deploy] %s\n' "$*"; }
die() { printf '[azulyoro-deploy] ERROR: %s\n' "$*" >&2; exit 1; }

[[ -d "$repo_root/.git" ]] || die "Git checkout not found at $repo_root"
[[ -f "$api_env" && -f "$web_env" ]] || die "production env files are missing"
[[ "$(stat -c '%U:%a' "$api_env")" == "root:600" ]] || die "$api_env must be root-owned with mode 600"
[[ "$(stat -c '%U:%a' "$web_env")" == "root:600" ]] || die "$web_env must be root-owned with mode 600"

get_env() {
    local file="$1" key="$2"
    awk -v key="$key" 'index($0, key "=") == 1 { sub("^[^=]*=", ""); print; exit }' "$file"
}

require_env() {
    local file="$1" key="$2" value
    value="$(get_env "$file" "$key")"
    [[ -n "$value" ]] || die "$key is empty in $file"
}

for item in \
    "${api_env}:ConnectionStrings__Postgres" \
    "${api_env}:AllowedHosts" \
    "${api_env}:ApiFootball__Key" \
    "${api_env}:ApiFootball__BaseUrl" \
    "${api_env}:Brevo__ApiKey" \
    "${api_env}:Brevo__FromEmail" \
    "${api_env}:Frontend__BaseUrl" \
    "${api_env}:Frontend__RevalidateSecret" \
    "${api_env}:Auth__CookieDomain" \
    "${api_env}:Cors__Origins__0" \
    "${web_env}:NEXT_PUBLIC_API_URL" \
    "${web_env}:NEXT_PUBLIC_SITE_URL" \
    "${web_env}:REVALIDATE_SECRET"; do
    require_env "${item%%:*}" "${item#*:}"
done

[[ "$(get_env "$api_env" Frontend__RevalidateSecret)" == "$(get_env "$web_env" REVALIDATE_SECRET)" ]] ||
    die "Frontend__RevalidateSecret and REVALIDATE_SECRET must match"
[[ "$(get_env "$api_env" Frontend__BaseUrl)" == "$(get_env "$web_env" NEXT_PUBLIC_SITE_URL)" ]] ||
    die "Frontend__BaseUrl and NEXT_PUBLIC_SITE_URL must match"

install -d -m 755 -o root -g root "$releases"
exec 9>"$lock_file"
flock -n 9 || die "another deployment is already running"

cd "$repo_root"
[[ -z "$(git status --porcelain)" ]] || die "checkout has local changes; refusing to overwrite production files"

git fetch --prune origin main
target_sha="$(git rev-parse origin/main)"
[[ -z "$expected_sha" || "$expected_sha" == "$target_sha" ]] ||
    die "requested SHA $expected_sha is not origin/main ($target_sha)"

if [[ "$(git rev-parse HEAD)" != "$target_sha" ]]; then
    git pull --ff-only origin main
fi

[[ -z "$(git status --porcelain)" ]] || die "checkout became dirty after update"

install -o root -g root -m 0750 deploy/scripts/azulyoro-deploy.sh /usr/local/sbin/azulyoro-deploy

release="$releases/$target_sha"
if [[ ! -f "$release/.complete" ]]; then
    stage="$releases/.build-${target_sha}.$$"
    mkdir -p "$stage/api" "$stage/front"
    trap 'rm -rf -- "$stage"' EXIT

    log "publishing API $target_sha"
    dotnet restore back/Azulyoro.slnx
    dotnet publish back/src/Azulyoro.Api/Azulyoro.Api.csproj \
        --configuration Release --no-restore --output "$stage/api" --no-self-contained

    log "building Next.js $target_sha"
    export NEXT_PUBLIC_API_URL="$(get_env "$web_env" NEXT_PUBLIC_API_URL)"
    export NEXT_PUBLIC_SITE_URL="$(get_env "$web_env" NEXT_PUBLIC_SITE_URL)"
    pnpm --dir front install --frozen-lockfile
    pnpm --dir front build
    cp -a front/.next/standalone/. "$stage/front/"
    cp -a front/.next/static "$stage/front/.next/static"
    cp -a front/public "$stage/front/public"

    chown -R root:root "$stage"
    find "$stage" -type d -exec chmod 755 {} +
    find "$stage" -type f -exec chmod 644 {} +
    printf '%s\n' "$target_sha" > "$stage/.complete"
    mv "$stage" "$release"
    trap - EXIT
fi

log "running database migrations for $target_sha"
systemctl start --wait "azulyoro-migrate@${target_sha}.service"

previous=""
if [[ -L "$current" ]]; then
    previous="$(readlink -f "$current")"
fi
new_link="$app_root/.current-${target_sha}.$$"
ln -s "$release" "$new_link"
mv -Tf "$new_link" "$current"

rollback() {
    [[ -n "$previous" && -d "$previous" ]] || return 0
    local rollback_link="$app_root/.rollback-$$"
    ln -s "$previous" "$rollback_link"
    mv -Tf "$rollback_link" "$current"
    systemctl restart --wait azulyoro-api.service || true
    systemctl restart --wait azulyoro-web.service || true
}

log "starting API and web release"
if ! systemctl restart --wait azulyoro-api.service; then
    rollback
    die "API failed to start; previous release restored"
fi
if ! systemctl restart --wait azulyoro-web.service; then
    rollback
    die "web failed to start; previous release restored"
fi

curl --fail --silent --show-error --max-time 10 http://127.0.0.1:5000/health >/dev/null || {
    rollback
    die "API health check failed; previous release restored"
}
curl --fail --silent --show-error --max-time 10 http://127.0.0.1:3000/ >/dev/null || {
    rollback
    die "web health check failed; previous release restored"
}

log "deployed $target_sha successfully"
