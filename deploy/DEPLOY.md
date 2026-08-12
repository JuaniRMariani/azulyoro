# Deploy — azulyoro (VPS Ubuntu, Nginx + systemd, sin Docker)

> Requiere acceso al VPS del usuario. Estos son los pasos y artefactos; el
> deploy real lo ejecuta el usuario. Topología: Cloudflare (proxied, Full
> strict) → Nginx (443) → Next `127.0.0.1:3000` + Kestrel `127.0.0.1:5000` →
> Postgres local.

## 0. Prerrequisitos en el VPS
- .NET 10 runtime (`dotnet`), Node 22+, PostgreSQL 16/17, Nginx.
- Usuario de servicio no-root `azulyoro`. Directorios `/var/www/azulyoro/{api,front}`.

## 1. Base de datos (F5-3)
```bash
sudo -u postgres createuser azulyoro --pwprompt
sudo -u postgres createdb azulyoro -O azulyoro
```
Aplicar migraciones: publicar el API y correr `dotnet ef database update` con la
connection string de prod, o incluir `db.Database.Migrate()` en el arranque.
**Backups off-box**: `deploy/backup/pg-backup.sh` + timer (ver abajo).

## 2. Build & publish
```bash
# API
dotnet publish back/src/Azulyoro.Api -c Release -o /var/www/azulyoro/api
# Front (standalone)
cd front && pnpm install && pnpm build
cp -r .next/standalone/* /var/www/azulyoro/front/
cp -r .next/static /var/www/azulyoro/front/.next/static
cp -r public /var/www/azulyoro/front/public
```

## 3. Secrets (EnvironmentFile, root-only)
`/etc/azulyoro/api.env`:
```
ConnectionStrings__Postgres=Host=127.0.0.1;Database=azulyoro;Username=azulyoro;Password=***
ApiFootball__Key=***
Brevo__ApiKey=***
Frontend__RevalidateSecret=***
Cors__Origins__0=https://azulyoro.com.ar
```
`/etc/azulyoro/web.env`:
```
NEXT_PUBLIC_API_URL=https://api.azulyoro.com.ar
NEXT_PUBLIC_SITE_URL=https://azulyoro.com.ar
REVALIDATE_SECRET=***  # == Frontend__RevalidateSecret
```
```bash
sudo chmod 600 /etc/azulyoro/*.env && sudo chown root:root /etc/azulyoro/*.env
```

## 4. systemd (F5-2)
```bash
sudo cp deploy/systemd/azulyoro-api.service /etc/systemd/system/
sudo cp deploy/systemd/azulyoro-web.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now azulyoro-api azulyoro-web
```
Ambos deben quedar `active (running)` y sobrevivir reboot.

## 5. Nginx + TLS (F5-1, F5-4)
```bash
sudo cp deploy/nginx/azulyoro.conf /etc/nginx/sites-available/azulyoro
sudo ln -s /etc/nginx/sites-available/azulyoro /etc/nginx/sites-enabled/
# Cloudflare Origin Certificate en /etc/nginx/ssl/azulyoro.com.ar.{pem,key}
sudo nginx -t && sudo systemctl reload nginx
```
- Cloudflare: DNS proxied (naranja), SSL/TLS = **Full (strict)**.
- Completar `set_real_ip_from` con los rangos de https://www.cloudflare.com/ips/.
- Firewall (ufw): permitir 443/80 sólo desde IPs de Cloudflare.

## 6. Email DNS (F4-9) — `mail.azulyoro.com.ar` (Brevo)
- **SPF** (TXT en el dominio de envío): `v=spf1 include:spf.brevo.com ~all`
- **DKIM**: cargar el registro CNAME/TXT que provee Brevo (`brevo1._domainkey`, `brevo2._domainkey`).
- **DMARC** (TXT `_dmarc`): `v=DMARC1; p=none; rua=mailto:dmarc@azulyoro.com.ar; adkim=r; aspf=r`
  (subir a `p=quarantine` tras validar alineación).

## 7. Verificación E2E prod (F5-5)
- `https://azulyoro.com.ar` y `https://api.azulyoro.com.ar/health` responden por CF (Full strict).
- Partido live actualiza; publish en CMS → revalidate visible; signup → email; newsletter DOI.
- Origen sólo accesible vía Cloudflare (probar IP directa → bloqueada).

## Backups (F5-3)
`deploy/backup/pg-backup.sh` hace `pg_dump` y lo copia off-box; correr vía
systemd timer diario (`deploy/systemd/azulyoro-backup.{service,timer}`).
