# 03 — Arquitectura técnica

> Stack: **.NET 10** (Vertical Slice Architecture + EF Core + Postgres) · **Next.js 16** (App Router, next-intl es/en) · **VPS Ubuntu único** (Nginx + systemd, sin Docker).

## Topología de deploy
```
Internet → Cloudflare (DNS proxied, TLS, CDN/cache, DDoS)
        → Nginx (:443, Origin Cert, SSL Full strict) reverse proxy
             ├── azulyoro.com.ar      → Next.js (node standalone, 127.0.0.1:3000)
             └── api.azulyoro.com.ar   → .NET Kestrel (127.0.0.1:5000)
        → Postgres local (127.0.0.1:5432) — schema app + schema hangfire
```
- **.NET:** `dotnet publish -c Release`, Kestrel en loopback, `systemd` (`Restart=always`, user no-root, `EnvironmentFile` para secrets). Configurar `ForwardedHeaders` (esquema/host reales → cookies Secure + redirects).
- **Next:** `output: 'standalone'`, copiar `.next/static` + `public/` a standalone, `node server.js` en :3000, `systemd`. Nginx sirve `/_next/static` y `/public` directo.
- **TLS:** Cloudflare proxied + **Origin Certificate** en Nginx (sin loop Certbot), SSL **Full (strict)**, firewall de origen restringido a IPs de Cloudflare.
- **Backups:** `pg_dump` off-box vía systemd timer/cron.

## 1. Background jobs (.NET 10) — híbrido
- **Backbone: Hangfire** (Postgres-backed vía `Hangfire.PostgreSql`, dashboard `/hangfire` **admin-guarded**, nunca público).
  - **Scraping de noticias:** `RecurringJob` cron `*/20 * * * *`. Retries + backoff + visibilidad de fallos gratis. `[DisableConcurrentExecution]` para evitar overlap.
- **Live-match polling:** `BackgroundService` + `PeriodicTimer`. Wake grueso 60s → chequea en Postgres si hay fixture live → si sí, loop interno 30–60s hasta `FT`; si no, duerme. (Alternativa: Hangfire agenda la ventana de activación al kickoff.)
- Single-VPS → sin lock distribuido. Quartz.NET sólo si más adelante hace falta calendarios/timezone/multi-nodo (no ahora).

## 2. Scraping (.NET)
- **RSS-first** (regla #1): la mayoría de los medios exponen RSS/Atom → estable, barato, legalmente más limpio, ya deduplicado. Parsear con `System.ServiceModel.Syndication` (sin dep extra). HTML sólo como fallback.
- **Parser HTML:** **AngleSharp** (CSS selectors, async, mantenido) por default; HtmlAgilityPack (XPath) alternativa.
- **Playwright** sólo per-source para páginas JS-rendered (headless Chromium pesado → flag por fuente, reusar un browser).
- **Politeness:** honrar `robots.txt` (cachear parse); rate-limit por dominio (1 req/2–5s + jitter, `SemaphoreSlim`/token-bucket por host); **User-Agent descriptivo** `AzulYOroBot/1.0 (+https://azulyoro.com.ar/bot)`; `IHttpClientFactory` + **Polly** (backoff+jitter, respetar `Retry-After`); requests condicionales (`If-Modified-Since`/`ETag`).
- **Dedup:** hash de URL canónica normalizada (strip UTM) como PK + SimHash de título normalizado para misma nota en distinta URL. Unique index → re-scrapes idempotentes.
- **Storage:** insertar en `staging_articles` (`status=Pending`, `source`, `source_url`, `url_hash`, `title`, `excerpt`, `clean_content`, `image_url`, `scraped_at`). Editor revisa en CMS y **promueve** a `articles` (slug propio, traducciones, schema). Sanitizar HTML al ingerir (HtmlSanitizer).

## 3. Email (newsletter + transaccional)
- **Elegido: Brevo** — cubre transaccional (verificación cuenta, reset) **y** newsletter con double-opt-in nativo, en un free tier generoso (300/día ongoing). Alternativa dev-céntrica: **Resend** (mejor API, React Email). Escala futura: mover bulk a **SES**.
- **Double opt-in:** (1) submit email → `status=pending` + token firmado single-use con expiración; (2) mail con `/newsletter/confirm?token=…`; (3) al click → verificar → `status=confirmed` + `confirmed_at`+IP (prueba de consentimiento). Nunca mailear no-confirmados. Mismo patrón de token para verificación de cuenta. **Baja one-click** (`List-Unsubscribe`) en cada newsletter.
- **Deliverability (DNS):** **SPF** (TXT), **DKIM** (CNAME/TXT del proveedor), **DMARC** (`p=none`+`rua=` → tighten). Subdominio de envío dedicado `mail.azulyoro.com.ar`.

## 4. Next.js 16 — SEO
- **Estrategia de render por volatilidad:**
  - Historia / jugador / equipo / club → **SSG + ISR** (`generateStaticParams`, `revalidate` de horas).
  - **Partido en vivo** → **SSR / streaming** (`dynamic='force-dynamic'` o `revalidate=0`) + React Suspense (shell/SEO instantáneo, score streamea). Polling client / SSE para updates in-page.
  - Noticias → **ISR + on-demand revalidation**: CMS publica → webhook → Route Handler → `revalidateTag('article:{id}')`. Taggear fetches. Proteger webhook con secret.
- **i18n (next-intl es/en):** rutas localizadas `/es/...` `/en/...`; middleware inyecta `hreflang`; en `generateMetadata` setear title/description/OG **por locale** + `alternates.canonical` + `alternates.languages` (incl `x-default`).
- **Structured data (JSON-LD):** `SportsEvent` (partidos), `SportsTeam`/`SportsOrganization` (equipo/club), `NewsArticle` (noticias), `BreadcrumbList` site-wide.
- **Sitemap/robots:** `sitemap.ts` dinámico desde DB (con alternates por locale) + `robots.ts`. `metadataBase` + OG/Twitter images.
- **Imágenes:** `next/image` (AVIF/WebP, lazy). En VPS sin Docker, `sharp` es pesado → offload a **Cloudflare** (Polish/Resizing) o pre-generar tamaños en storage (`unoptimized` para esos). Siempre `width/height` (anti-CLS), `priority` sólo en hero LCP.

## 5. Auth (cuentas gratis de socios)
- **ASP.NET Core Identity + sesión por cookie `HttpOnly`** (no JWT en localStorage). Da registro, hashing, email confirmation, lockout, 2FA. Se integra con verificación por Brevo.
- **OAuth social:** Google (opc. Apple/Facebook) vía external auth — buen boost de conversión, mantener como opción junto a email/password.
- **Cross-subdomain** (`api.` ↔ root): cookie `Domain=.azulyoro.com.ar` + **`SameSite=Lax`** (siblings = same-site, sin `None`). Siempre `Secure`+`HttpOnly`. CORS con `AllowCredentials()` + allow-list explícita (sin wildcard).
- **CSRF:** antiforgery de ASP.NET — token XSRF que el front reenvía en header `X-XSRF-TOKEN` en mutaciones. `SameSite=Lax` cubre el POST cross-site clásico.
- **Front → API: directo, sin BFF** en v1 (comparten dominio padre, cookie fluye nativa). Server Components forwardean el cookie header. BFF completo sólo si aparece token OAuth de terceros o cliente mobile.

## Resumen de decisiones
| Área | Decisión |
|---|---|
| Jobs | Hangfire (scraping + orquestación, dashboard admin) + BackgroundService/PeriodicTimer (live poll) |
| Scraping | RSS-first + AngleSharp + Playwright per-source · robots.txt + rate-limit + dedup → `staging_articles` moderación |
| Email | Brevo (transaccional + DOI newsletter) · SPF/DKIM/DMARC en `mail.` |
| SEO | SSG+ISR evergreen · SSR+streaming live · `revalidateTag` on publish · next-intl hreflang · JSON-LD · sitemap dinámico |
| Auth | Identity + cookie `HttpOnly` `SameSite=Lax` `Domain=.azulyoro.com.ar` · CORS credentials · CSRF token · sin BFF |
| Deploy | Ubuntu sin Docker · Kestrel+systemd (`api.`) · Next standalone+systemd · Postgres local · Cloudflare Origin Cert Full(strict) |

## Fuentes
- Hangfire vs Quartz vs Hosted: https://boldsign.com/blogs/aspnet-core-background-jobs-hosted-services-hangfire-quartz/
- Next revalidation/ISR: https://nextjs.org/docs/app/guides/how-revalidation-works
- next-intl i18n: https://nextjslaunchpad.com/article/nextjs-internationalization-next-intl-app-router-i18n-guide
- SameSite cookies ASP.NET: https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-10.0
- Deploy Next VPS: https://servercompass.app/blog/deploy-nextjs-to-vps-complete-guide
