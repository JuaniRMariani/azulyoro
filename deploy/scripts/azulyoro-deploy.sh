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
lock_dir=/run/lock/azulyoro-deploy.lock.d

log() { printf '[azulyoro-deploy] %s\n' "$*"; }
die() { printf '[azulyoro-deploy] ERROR: %s\n' "$*" >&2; exit 1; }

if ! mkdir "$lock_dir" 2>/dev/null; then
    die "another deployment is already running"
fi
cleanup_lock() { rmdir "$lock_dir" 2>/dev/null || true; }
trap cleanup_lock EXIT

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
    "${web_env}:REVALIDATE_SECRET" \
    "${web_env}:API_INTERNAL_URL"; do
    require_env "${item%%:*}" "${item#*:}"
done

[[ "$(get_env "$api_env" Frontend__RevalidateSecret)" == "$(get_env "$web_env" REVALIDATE_SECRET)" ]] ||
    die "Frontend__RevalidateSecret and REVALIDATE_SECRET must match"
[[ "$(get_env "$api_env" Frontend__BaseUrl)" == "$(get_env "$web_env" NEXT_PUBLIC_SITE_URL)" ]] ||
    die "Frontend__BaseUrl and NEXT_PUBLIC_SITE_URL must match"

install -d -m 755 -o root -g root "$releases"

cd "$repo_root"
[[ -z "$(git status --porcelain)" ]] || die "checkout has local changes; refusing to overwrite production files"

git fetch --prune origin main
remote_sha="$(git rev-parse origin/main)"
if [[ -n "$expected_sha" ]]; then
    target_sha="$expected_sha"
    [[ "$expected_sha" == "$remote_sha" ]] ||
        die "requested SHA $expected_sha is not origin/main ($remote_sha)"
else
    # Manual deployments use the exact clean checkout currently prepared on
    # the VPS. CI deployments pass the GitHub SHA explicitly and validate it
    # against origin/main above.
    target_sha="$(git rev-parse HEAD)"
fi

git cat-file -e "$target_sha^{commit}" || die "commit $target_sha is not available locally"
[[ "$(git rev-parse HEAD)" == "$target_sha" ]] ||
    die "checkout HEAD does not match requested commit $target_sha"

[[ -z "$(git status --porcelain)" ]] || die "checkout became dirty after update"

install -o root -g root -m 0750 deploy/scripts/azulyoro-deploy.sh /usr/local/sbin/azulyoro-deploy

release="$releases/$target_sha"
if [[ ! -f "$release/.complete" ]]; then
    stage="$releases/.build-${target_sha}.$$"
    mkdir -p "$stage/api" "$stage/front"
    build_unit="azulyoro-build-api-${target_sha:0:12}"
    cleanup_build() {
        systemctl stop "$build_unit.service" 2>/dev/null || true
        if [[ -d "$stage" ]]; then
            rm -rf -- "$stage"
        fi
        if [[ -d "$release" && ! -f "$release/.complete" ]]; then
            rm -rf -- "$release"
        fi
    }
    trap 'cleanup_build; cleanup_lock' EXIT
    [[ ! -e "$release" ]] || die "incomplete release already exists: $release"

    log "publishing API $target_sha"
    dotnet restore back/Azulyoro.slnx
    dotnet publish back/src/Azulyoro.Api/Azulyoro.Api.csproj \
        --configuration Release --no-restore --output "$stage/api" --no-self-contained

    # Put the API in its final release path first: migrations and the temporary
    # build server must execute the exact bits that will become live.
    mkdir -p "$release"
    mv "$stage/api" "$release/api"
    mkdir -p "$release/front"
    # sudo may run with a restrictive umask. The service account needs to
    # traverse the immutable release, but must never own or write it.
    find "$release" -type d -exec chmod 755 {} +
    find "$release" -type f -exec chmod 644 {} +

    log "running database migrations for $target_sha"
    systemctl start --wait "azulyoro-migrate@${target_sha}.service"

    log "starting temporary API for Next.js data collection"
    systemd-run --quiet --unit="$build_unit" --collect \
        --property=User=azulyoro \
        --property=Group=azulyoro \
        --property=WorkingDirectory="$release/api" \
        --property=EnvironmentFile="$api_env" \
        --property=Environment=ASPNETCORE_ENVIRONMENT=Production \
        --property=Environment=ASPNETCORE_URLS=http://127.0.0.1:5000 \
        --property=Restart=no \
        /usr/bin/dotnet "$release/api/Azulyoro.Api.dll"

    for attempt in {1..60}; do
        if curl --fail --silent --max-time 2 -H 'Host: api.azulyoro.com.ar' http://127.0.0.1:5000/health >/dev/null; then
            break
        fi
        if [[ "$attempt" == 60 ]]; then
            journalctl -u "$build_unit.service" -n 80 --no-pager >&2 || true
            die "temporary API did not become healthy"
        fi
        sleep 1
    done

    log "building Next.js $target_sha"
    export NEXT_PUBLIC_API_URL="$(get_env "$web_env" NEXT_PUBLIC_API_URL)"
    export NEXT_PUBLIC_SITE_URL="$(get_env "$web_env" NEXT_PUBLIC_SITE_URL)"
    export API_INTERNAL_URL="$(get_env "$web_env" API_INTERNAL_URL)"
    pnpm --dir front install --frozen-lockfile
    pnpm --dir front build
    cp -a front/.next/standalone/. "$release/front/"
    cp -a front/.next/static "$release/front/.next/static"
    cp -a front/public "$release/front/public"

    systemctl stop "$build_unit.service" 2>/dev/null || true

    chown -R root:root "$release"
    find "$release" -type d -exec chmod 755 {} +
    find "$release" -type f -exec chmod 644 {} +
    printf '%s\n' "$target_sha" > "$release/.complete"
    rm -rf -- "$stage"
    trap cleanup_lock EXIT
fi

if [[ ! -f "$release/.complete" ]]; then
    log "running database migrations for $target_sha"
    systemctl start --wait "azulyoro-migrate@${target_sha}.service"
fi

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
    systemctl restart azulyoro-api.service || true
    systemctl restart azulyoro-web.service || true
}

log "starting API and web release"
if ! systemctl restart azulyoro-api.service; then
    rollback
    die "API failed to start; previous release restored"
fi
if ! systemctl restart azulyoro-web.service; then
    rollback
    die "web failed to start; previous release restored"
fi

curl --fail --silent --show-error --max-time 10 -H 'Host: api.azulyoro.com.ar' http://127.0.0.1:5000/health >/dev/null || {
    rollback
    die "API health check failed; previous release restored"
}
curl --fail --silent --show-error --max-time 10 http://127.0.0.1:3102/ >/dev/null || {
    rollback
    die "web health check failed; previous release restored"
}

log "deployed $target_sha successfully"
