# TODO · azulyoro

> Backlog **granular y detallado** por fases. Cada tarea es atómica y verificable.
> Fuente de verdad persistente — sincronizado con la task list del harness.
> Contexto en `../docs/`. Marcar `[x]` **sólo** cuando esté verificado (build/test/prueba real).
> Formato por tarea: **Qué** · **Pasos clave** · **DoD** (Definition of Done) · **Deps**.

**Convenciones transversales (aplican a TODAS las tareas):**
- Commits `type(module): mensaje` en inglés, uno por unidad de trabajo.
- Nada de `window.confirm/alert` → `useConfirm()`/`useToast()`. Campos obligatorios con `(*)`.
- IDs en UI: UUID corto (6 chars, mayúsculas, sin guiones). Nunca UUID completo salvo pedido.
- Tailwind v4: custom CSS dentro de `@layer`. **Sin escudo del club** en ningún asset (incl. favicon).
- Datos servidos desde Postgres propio, **nunca** pegarle a API-Football en page-load.

---

## Fase 0 — Fundaciones (setup)

- [ ] **F0-1 · Confirmar decisiones bloqueantes**
  - Qué: cerrar las 3 decisiones pendientes con el usuario (ver §Decisiones).
  - Pasos: presentar defaults (whitelist noticias, responsable legal, edad mínima); registrar respuestas.
  - DoD: las 3 respuestas escritas en §Decisiones de este archivo.
  - Deps: —

- [x] **F0-2 · git init + .gitignore (.NET + Next)**
  - Qué: inicializar repo y `.gitignore` combinando plantillas .NET y Next.
  - Pasos: `git init`; `.gitignore` con `bin/ obj/ *.user appsettings.*.Local.json` (.NET) + `node_modules/ .next/ out/ .env*` excepto `.env.example` (Next); verificar con `git check-ignore -v` que no se trague carpetas feature (gotcha `[Rr]elease/`).
  - DoD: repo inicializado · `.gitignore` correcto · commit `chore(repo): init scaffolding`.
  - Deps: —

- [x] **F0-3 · Scaffold back .NET 10 (VSA)**
  - Qué: solución + 3 proyectos base (Api host + Domain + Infrastructure), estructura VSA (`Features/`).
  - Pasos: `dotnet new sln -n Azulyoro`; `Azulyoro.Api` (webapi `--use-minimal-apis`) en `src/`; `Azulyoro.Domain` + `Azulyoro.Infrastructure` (classlib); `dotnet sln add`; crear carpetas `Features/{Matches,Players,Standings,Articles,Newsletter,Auth,Members,Admin,Legal}`.
  - DoD: `dotnet build` verde · estructura de `docs/07` creada.
  - Deps: F0-2

- [x] **F0-4 · Paquetes NuGet clave**
  - Qué: instalar dependencias del stack.
  - Pasos: Infra → `Npgsql.EntityFrameworkCore.PostgreSQL`, `AngleSharp`, `Polly`/`Microsoft.Extensions.Http.Resilience`, `HtmlSanitizer`; Api → `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Hangfire.AspNetCore`, `Hangfire.PostgreSql`, `ErrorOr`.
  - DoD: `dotnet restore` + `dotnet build` verdes.
  - Deps: F0-3

- [x] **F0-5 · DbContext base + Postgres local**
  - Qué: `AppDbContext` mínimo + conexión a Postgres 17 local.
  - Pasos: crear DB `azulyoro`; `DbContext` en Infra + registro en Api con Npgsql; connection string vía `user-secrets`; migración vacía de prueba.
  - DoD: `dotnet ef database update` aplica sin error (schema vacío/base creado).
  - Deps: F0-4

- [x] **F0-6 · Scaffold front Next 16 + next-intl + Tailwind v4**
  - Qué: app Next con TS, App Router, Tailwind v4.
  - Pasos: `pnpm create next-app@latest . --ts --app --tailwind --eslint --src-dir=false --import-alias "@/*"`; `pnpm add next-intl`; `next.config.ts` con `output: 'standalone'`.
  - DoD: `pnpm build` verde.
  - Deps: F0-2

- [x] **F0-7 · i18n [locale] + middleware + LocaleSwitcher**
  - Qué: routing localizado es/en con next-intl.
  - Pasos: estructura `app/[locale]/...`; middleware next-intl (locales `es`/`en`, `defaultLocale=es`, `x-default`→es); `messages/{es,en}.json` base; `LocaleSwitcher` en `components/ui/`; `<html lang>` por locale.
  - DoD: `/es` y `/en` responden 200 · switch de idioma preserva ruta.
  - Deps: F0-6

- [x] **F0-8 · Design system base "Azul y Oro"**
  - Qué: identidad visual propia (sin escudo) + tokens.
  - Pasos: tokens azul/oro OKLCH (de `docs/11`) en `@layer base`; light + **dark mode de arranque**; tipografías display (Space Grotesk/Archivo) + sans (Inter/Manrope) con `font-display: swap` + `tabular-nums` para stats; **wordmark/monograma propio** "AyO"; favicon monograma (no escudo); contraste WCAG AA (oro texto = `--oro-600` sobre claro).
  - DoD: tokens aplicados · sin escudo/tipografía oficial en ningún asset · dark mode funciona.
  - Deps: F0-7

- [x] **F0-9 · Shell/layout (Header + Footer)**
  - Qué: layout global con nav + disclaimer no oficial.
  - Pasos: Header (logo, links de `docs/05` §Nav, badge "En vivo" placeholder, `LocaleSwitcher`, Login); Footer con `UnofficialDisclaimer` (texto exacto `02-legal §1`), links legales, atribución "Datos deportivos por API-Football".
  - DoD: layout render en es/en · disclaimer no oficial visible en todas las páginas.
  - Deps: F0-8

- [x] **F0-10 · Secrets dev + .env.example**
  - Qué: gestión de secrets sin commitearlos.
  - Pasos: `dotnet user-secrets` (ApiFootball:Key, ConnectionStrings:Postgres, Brevo:ApiKey, Frontend:RevalidateSecret); front `.env.local` (`NEXT_PUBLIC_API_URL`, `REVALIDATE_SECRET`); `.env.example` commiteado con placeholders.
  - DoD: builds toman config · `git status` no muestra secrets reales.
  - Deps: F0-5, F0-6

- [x] **F0-11 · Verificación Fase 0 (DoD `docs/07`)**
  - Qué: gate de cierre de fase.
  - Pasos: correr `dotnet build` + `dotnet ef database update` + `pnpm build`; abrir `/es` y `/en`; confirmar footer disclaimer.
  - DoD: back build verde · schema aplica · front build verde · `/es` y `/en` OK · disclaimer visible. Commit `chore(scaffold): phase 0 baseline`.
  - Deps: F0-2..F0-10

## Fase 1 — Módulo Deportivo (backend + sync)

- [ ] **F1-1 · Entidades Domain**
  - Qué: modelo de dominio deportivo de `docs/04`.
  - Pasos: crear entidades `seasons, competitions, teams, players, fixtures, fixture_events, fixture_lineups, fixture_lineup_players, fixture_player_stats, player_season_stats, standings, sync_state`; enums (`FixtureStatus`, `PlayerPosition`, `EventType`, `CompetitionType`); campos `*_ext_id` + `created_at`/`updated_at`.
  - DoD: proyecto Domain compila.
  - Deps: F0-11

- [ ] **F1-2 · EF config + migración Initial**
  - Qué: mapeo EF + schema en Postgres.
  - Pasos: `IEntityTypeConfiguration` por entidad; PK **uuid v7**; unique index en `ext_id` (upsert); índices `(date_utc)`,`(status)`,`(is_boca,date_utc)`, uniques de `docs/04`; `dotnet ef migrations add Initial`.
  - DoD: `dotnet ef database update` crea todas las tablas + índices.
  - Deps: F1-1

- [ ] **F1-3 · Cliente API-Football (typed + Polly)**
  - Qué: cliente HTTP resiliente hacia `v3.football.api-sports.io`.
  - Pasos: typed client vía `IHttpClientFactory`; header `x-apisports-key`; Polly retry + circuit-breaker en 429/5xx respetando `Retry-After`; leer y loguear `x-ratelimit-requests-remaining` / `X-RateLimit-Remaining`; DTOs de respuesta.
  - DoD: unit test con `HttpMessageHandler` mock (retry en 429, parseo de payload).
  - Deps: F1-2

- [ ] **F1-4 · Seed competiciones + Boca + verificación de IDs**
  - Qué: datos base + validar IDs reales.
  - Pasos: seed `competitions` (Primera=128, Copa Arg=130, Libertadores=13, Sudamericana=11) + `teams` Boca (451, `is_tracked=true`); comando manual que llame `/teams?search=Boca` y `/leagues?country=Argentina` y compare IDs.
  - DoD: sync manual devuelve `team=451`/`league=128` confirmados y persiste seed. **(Requiere API key.)**
  - Deps: F1-3

- [ ] **F1-5 · Sync estático (teams/players/plantel)**
  - Qué: ingesta de datos que cambian poco (cache 12–24h).
  - Pasos: servicio que llama `/players/squads?team=451` + `/teams?id=451`; upsert por `ext_id`; setear `last_synced_at`; job diario.
  - DoD: `players` de Boca poblados en DB desde la API.
  - Deps: F1-4

- [ ] **F1-6 · Sync semi (standings/fixtures/season stats)**
  - Qué: ingesta 30–60 min.
  - Pasos: `/standings`, `/fixtures?team=451&season=`, `/players?team=451&season=` (paginado); upsert; `sync_state` por recurso.
  - DoD: `fixtures` (próximos+pasados) y `standings` de Boca en DB.
  - Deps: F1-4

- [ ] **F1-7 · Sync live (BackgroundService)**
  - Qué: polling sólo mientras hay partido.
  - Pasos: `BackgroundService` + `PeriodicTimer` (wake 60s); chequear en DB fixtures del día con `status ∈ {1H,HT,2H,ET,P}`; si hay, poll `/fixtures?id={id}` (bundle events+lineups+stats) cada 30–60s; upsert incremental; **cortar en FT**; backoff según rate-limit header.
  - DoD: test con fixture live simulado (mock) → eventos/marcador se actualizan y corta en FT.
  - Deps: F1-6

- [ ] **F1-8 · Hangfire + dashboard + sync_state**
  - Qué: orquestación y visibilidad de jobs.
  - Pasos: Hangfire sobre Postgres (`Hangfire.PostgreSql`, schema `hangfire`); `RecurringJob` estático (diario) y semi (30–60m); dashboard `/hangfire` **admin-guarded** (auth filter, nunca público); actualizar `sync_state` (last_run/last_ok/last_error).
  - DoD: dashboard requiere rol Admin · jobs recurrentes registrados y ejecutan.
  - Deps: F1-5, F1-6

- [ ] **F1-9 · Endpoints API deportivos (VSA)**
  - Qué: API pública servida desde DB.
  - Pasos: slice por endpoint (ErrorOr + ProblemDetails, camelCase, envelope `{items,page,pageSize,total}`): `GET /api/matches` (status/competition/from/to/paginado), `/matches/next`, `/matches/live` (204 si none), `/matches/{id}`, `/matches/{id}/{events,lineups,player-stats}`, `/squad`, `/players/{id}`, `/players/{id}/stats?season=`, `/standings`, `/competitions`; `Cache-Control` alineado a sync.
  - DoD: cada endpoint devuelve JSON correcto desde Postgres (probado con datos sync).
  - Deps: F1-5, F1-6

- [ ] **F1-10 · Config transversal (CORS/CSRF/rate-limit)**
  - Qué: base de seguridad para consumo del front.
  - Pasos: CORS allow-list (`https://azulyoro.com.ar` + `http://localhost:3000` dev) + `AllowCredentials`; antiforgery configurado; rate-limiter en POST públicos (prep para register/login/subscribe); `ForwardedHeaders`.
  - DoD: preflight CORS OK con credentials · rate-limit responde 429 en umbral.
  - Deps: F1-9

- [ ] **F1-11 · Verificación Fase 1**
  - DoD: `GET /api/squad` devuelve plantel real y `GET /api/matches/next` el próximo partido real. Commit `feat(sports): backend + api-football sync`.
  - Deps: F1-1..F1-10

## Fase 2 — Front deportivo (SEO)

- [ ] **F2-1 · API client front + estados base**
  - Qué: capa de datos + UX de carga/vacío/error.
  - Pasos: `lib/api` (fetch con `NEXT_PUBLIC_API_URL`, tags de cache, forward de cookies en RSC) + tipos TS espejo del contrato `docs/06`; `QueryState`, `Skeleton`, `EmptyState` en `components/ui/`.
  - DoD: fetch de `/api/squad` renderiza en una página de prueba.
  - Deps: F1-9, F0-11

- [ ] **F2-2 · Componentes UI deportivos**
  - Qué: piezas reutilizables de `docs/05`/`docs/11`.
  - Pasos: `MatchCard`, `LiveScoreBadge` (pill roja `--live`, pulso, respeta `prefers-reduced-motion`), `FixtureList`, `StandingsTable` (Boca resaltado, `tabular-nums`), `PlayerCard`, `PlayerStatsTable`, `Breadcrumbs` (con schema).
  - DoD: página demo renderiza todos con datos reales.
  - Deps: F2-1

- [ ] **F2-3 · Home (ISR)**
  - Qué: portada.
  - Pasos: próximo partido + últimos resultados + últimas noticias (placeholder hasta Fase 3); `revalidate` corto; hero LCP con `priority`.
  - DoD: render con datos reales, sin CLS visible.
  - Deps: F2-2

- [ ] **F2-4 · Partidos hub + fixture/calendario (SSG+ISR)**
  - Pasos: `/partidos` (`/en/matches`) con calendario + próximos; `/partidos/fixture` por competición/temporada.
  - DoD: render, navegable, hreflang par es/en.
  - Deps: F2-2

- [ ] **F2-5 · Resultados (histórico)**
  - Pasos: `/partidos/resultados` (`/en/matches/results`) agrupado por fecha/competición.
  - DoD: render con resultados reales.
  - Deps: F2-2

- [ ] **F2-6 · Detalle partido (ISR→SSR live)**
  - Qué: página estrella del módulo.
  - Pasos: `/partido/{slug}` (`/en/match/{slug}`), **slug SEO sin UUID** (ID resuelto server-side); si live → `dynamic='force-dynamic'` + Suspense (shell/SEO instantáneo, score streamea) + polling/SSE; secciones eventos, formaciones, stats; al FT pasa a ISR.
  - DoD: slug resuelve · sin UUID en URL · live actualiza in-page.
  - Deps: F2-2

- [ ] **F2-7 · En vivo (SSR streaming)**
  - Pasos: `/en-vivo` (`/en/live`); si hay partido live redirige a su detalle; si no, próximo + estado.
  - DoD: render y redirección correctos.
  - Deps: F2-6

- [ ] **F2-8 · Plantel + fichas jugador (SSG+ISR)**
  - Pasos: `/plantel` (`/en/squad`) grid por posición; `/jugadores/{slug}` (`/en/players/{slug}`) bio + stats temporada/partido; `generateStaticParams`; slug sin UUID.
  - DoD: grid y ficha renderizan con datos reales.
  - Deps: F2-2

- [ ] **F2-9 · Posiciones (ISR)**
  - Pasos: `/posiciones` (`/en/standings`) con `StandingsTable`, fila Boca resaltada.
  - DoD: tabla real, revalidate 1h en días de partido.
  - Deps: F2-2

- [ ] **F2-10 · SEO técnico**
  - Qué: fundaciones de posicionamiento (de `docs/10`).
  - Pasos: JSON-LD `SportsEvent` (partido), `SportsTeam` (club/plantel), `BreadcrumbList` (global) como server component; `sitemap.ts` dinámico segmentado desde DB con alternates por locale; `robots.ts` (Disallow `/api/ /_next/ /admin/ /*?*utm_*`); hreflang `es-AR`/`en`/`x-default` completo (sin parcial) vía `alternates.languages`; `generateMetadata` por locale (title/desc/OG + canonical); canonical en filtros/paginado; `metadataBase`.
  - DoD: Rich Results Test válido · `/sitemap.xml` y `/robots.txt` responden · hreflang bidireccional.
  - Deps: F2-3..F2-9

- [ ] **F2-11 · Verificación Fase 2**
  - DoD: `pnpm build` verde · Lighthouse SEO alto · hreflang sin errores. Commit `feat(front-sports): sports pages + seo`.
  - Deps: F2-1..F2-10

## Fase 3 — Noticias + CMS moderación

- [ ] **F3-1 · Entidades contenido + migración**
  - Pasos: `staging_articles`, `articles`, `article_translations` (unique `(article_id,locale)`), `tags`/`article_tags`, `sources` (campos de `docs/09`: `rss_url`,`type`,`active`,`rate_limit_seconds`,`keyword_filter`,`robots_ok`); migración.
  - DoD: schema aplicado.
  - Deps: F1-2

- [ ] **F3-2 · Scraper RSS-first**
  - Qué: ingesta legalmente limpia (regla: reescribir, nunca pegar cuerpos).
  - Pasos: RSS con `System.ServiceModel.Syndication`; AngleSharp fallback HTML por-fuente; cache de `robots.txt`; rate-limit por host (`SemaphoreSlim`/token-bucket, 1 req/2–5s + jitter); UA `AzulYOroBot/1.0 (+https://azulyoro.com.ar/bot)`; requests condicionales (`If-Modified-Since`/`ETag`); dedup (`url_hash` canónico strip-UTM + `title_hash` simhash); `HtmlSanitizer` al ingerir; guardar sólo `title/excerpt/source_url` (no cuerpo completo) → `staging_articles` `status=Pending`.
  - DoD: ingesta real desde La Número 12 (`/feed/`) crea filas en staging sin duplicar en re-run.
  - Deps: F3-1, F0-1 (whitelist)

- [ ] **F3-3 · Seed sources + keyword filter**
  - Pasos: seed whitelist confirmada (VERIFIED activas; INFERRED inactivas hasta verificar server); `keyword_filter` (`Boca|Xeneize|<jugadores>`) para feeds de sección; marcar `robots_ok`.
  - DoD: `sources` activas en DB, filtro aplicado en ingesta.
  - Deps: F3-2, decisión (a)

- [ ] **F3-4 · Job Hangfire scraping**
  - Pasos: `RecurringJob` cron `*/20 * * * *` + `[DisableConcurrentExecution]`; retries/backoff; visibilidad de fallos en dashboard.
  - DoD: job corre en schedule y puebla staging sin overlap.
  - Deps: F3-2

- [ ] **F3-5 · CMS admin — cola de moderación**
  - Qué: flujo humano staging→published (nunca auto-publica cuerpos).
  - Pasos: vista `/admin/moderacion` (list Pending, filtros por status/categoría); acciones aprobar (promueve a `articles` Draft con `staging_id`), rechazar, editar/reescribir; editor de artículo `/admin/articulos/{id}` con traducciones es/en (title/summary/body_html/meta); `SourceAttribution` obligatorio; publish; rumores → `category=rumor`.
  - DoD: un item recorre staging→Draft→Published con traducción es/en.
  - Deps: F3-3

- [ ] **F3-6 · Endpoints contenido (público + admin)**
  - Pasos: público `GET /api/articles` (category/locale/paginado), `/articles/{slug}?locale=`, `/articles/featured`; admin `moderation` (list/approve/reject), `articles` CRUD + `publish`, `sources` CRUD.
  - DoD: endpoints devuelven JSON correcto por locale.
  - Deps: F3-5

- [ ] **F3-7 · Front noticias + fichajes**
  - Pasos: `/noticias` (list ISR + paginado) y `/noticias/{slug}` (detalle ISR, `SourceAttribution` "Fuente: X" + link-out, byline autor); `/fichajes` (`/en/transfers`) con `RumorBadge` "No confirmado"; `ArticleCard` 16:9; bloque "También leé".
  - DoD: publicar en CMS hace visible la nota en el front.
  - Deps: F3-6, F2-11

- [ ] **F3-8 · Revalidation on-publish**
  - Pasos: Route Handler `POST {FRONT}/api/revalidate` protegido por secret; back llama al publicar/actualizar → `revalidateTag('article:{id}')` / `news-list`; taggear fetches de noticias.
  - DoD: publish en back revalida la página sin redeploy.
  - Deps: F3-7

- [ ] **F3-9 · Verificación Fase 3**
  - DoD: E2E scrape→moderar/reescribir→publicar→revalidar visible. Commit `feat(news): scraper + moderation cms + front`.
  - Deps: F3-1..F3-8

## Fase 4 — Socios + Newsletter + Legales

- [ ] **F4-1 · ASP.NET Identity**
  - Pasos: extender `users` (display_name, role `Member|Editor|Admin`, locale_pref); Identity con EF; registro/login; lockout; tokens de email confirm; hashing.
  - DoD: registro + login funcionan (integration test).
  - Deps: F1-2

- [ ] **F4-2 · Integración Brevo (transaccional)**
  - Pasos: email service (Brevo API) para verificación de cuenta y reset; plantillas; envío sandbox/dev.
  - DoD: mail de verificación se genera/envía en dev.
  - Deps: F4-1

- [ ] **F4-3 · Cookie auth + CSRF + endpoints auth**
  - Pasos: cookie `HttpOnly; Secure; SameSite=Lax; Domain=.azulyoro.com.ar`; antiforgery (`X-XSRF-TOKEN` en mutaciones); CORS credentials; endpoints `register/verify-email/login/logout/me/forgot-password/reset-password/csrf`.
  - DoD: sesión por cookie persiste cross-subdomain (dev localhost equivalente) · CSRF exigido en POST.
  - Deps: F4-1

- [ ] **F4-4 · Google OAuth (opcional)**
  - Pasos: external auth Google (`/api/auth/google` + callback); enlazar a cuenta.
  - DoD: login con Google crea/inicia sesión.
  - Deps: F4-3

- [ ] **F4-5 · Zona privada socios**
  - Pasos: `is_members_only` en `articles`; `GET /api/members/content` (auth requerida); front `/socios/*` gated (SSR auth); redirect a login si 401.
  - DoD: contenido members-only sólo visible autenticado.
  - Deps: F4-3, F3-6

- [ ] **F4-6 · Newsletter double opt-in**
  - Pasos: entidad `newsletter_subscribers`; `POST /api/newsletter/subscribe` → `pending` + token firmado single-use con expiración + mail; `GET /confirm?token=` → `confirmed` + `confirmed_at`+IP; `GET /unsubscribe?token=` one-click; header `List-Unsubscribe`; nunca mailear no-confirmados.
  - DoD: E2E subscribe→email→confirm→confirmed · baja one-click funciona.
  - Deps: F4-2

- [ ] **F4-7 · Front auth + newsletter**
  - Pasos: `/ingresar`, `/registrarse` (+verificación), `/perfil` (auth); `NewsletterForm` con checkbox opt-in **destildado** + estado DOI; páginas `/newsletter/confirmar` y `/newsletter/baja`; campos `(*)`; toasts (no alerts).
  - DoD: flujos registro/login/perfil/alta-baja newsletter E2E en front.
  - Deps: F4-3, F4-6, F2-11

- [ ] **F4-8 · Páginas legales bilingües + banner cookies**
  - Pasos: entidad `legal_pages` (slug/locale/body_html/version/effective_date); seed desde `docs/borradores-legales/*` **resolviendo placeholders** con decisiones (b)(c) (responsable, emails, edad, CABA, Brevo, Plausible); páginas `/terminos /privacidad /aviso-legal /cookies` (es/en) desde DB; banner cookies (Plausible cookieless → banner mínimo; no-esenciales no disparan sin consentimiento); disclaimer no oficial en footer + About.
  - DoD: 4 páginas legales render es/en con datos resueltos (sin `[[...]]`).
  - Deps: decisión (b)(c)

- [ ] **F4-9 · DNS email (SPF/DKIM/DMARC)**
  - Pasos: documentar registros SPF (TXT), DKIM (del proveedor), DMARC (`p=none`+`rua=`) para `mail.azulyoro.com.ar`; se aplican en deploy.
  - DoD: registros documentados y listos para cargar en DNS.
  - Deps: F4-2

- [ ] **F4-10 · Rate-limit auth/newsletter**
  - Pasos: aplicar rate-limiter a `register/login/forgot-password/subscribe`.
  - DoD: 429 tras umbral en cada endpoint.
  - Deps: F4-3, F4-6

- [ ] **F4-11 · Verificación Fase 4**
  - DoD: signup→email→confirm, DOI newsletter, legales visibles es/en. Commit `feat(members): auth + newsletter + legal pages`.
  - Deps: F4-1..F4-10

## Fase 5 — Deploy VPS (requiere acceso al VPS del usuario)

- [ ] **F5-1 · Nginx reverse proxy**
  - Pasos: server blocks `azulyoro.com.ar`→Next `127.0.0.1:3000` y `api.azulyoro.com.ar`→Kestrel `127.0.0.1:5000`; Nginx sirve `/_next/static` y `/public` directo; `ForwardedHeaders` en .NET (esquema/host reales → cookies Secure).
  - DoD: proxy responde ambos hosts.
  - Deps: F0–F4

- [ ] **F5-2 · systemd units**
  - Pasos: `.NET dotnet publish -c Release` + unit (`Restart=always`, user no-root, `EnvironmentFile`); Next standalone (`node server.js`) + unit; copiar `.next/static`+`public/`.
  - DoD: ambos servicios `active (running)` y sobreviven reboot.
  - Deps: F5-1

- [ ] **F5-3 · Postgres prod + backups**
  - Pasos: Postgres local (schema app + hangfire); `pg_dump` vía systemd timer/cron **off-box**.
  - DoD: backup se genera y se copia fuera del box.
  - Deps: F5-2

- [ ] **F5-4 · Cloudflare + TLS**
  - Pasos: DNS proxied; Origin Certificate en Nginx; SSL **Full (strict)**; firewall de origen restringido a IPs de Cloudflare.
  - DoD: HTTPS Full(strict) activo · origen sólo accesible vía CF.
  - Deps: F5-1

- [ ] **F5-5 · Verificación E2E producción**
  - DoD: partido live actualiza; publish→revalidate visible; signup→email; newsletter DOI — todo en prod.
  - Deps: F5-1..F5-4

---

## Decisiones (defaults acordados + pendientes)
**Acordadas (prompt):** Analytics = **Plausible** (cookieless) · ads fuera de v1 · PK = **uuid v7** · rutas **slug SEO sin UUID** · API-Football **free tier** dev (Pro lo gestiona el usuario) · Entradas = **POST-MVP** (link-out oficial).

**Pendientes (bloquean fases indicadas, NO bloquean Fase 0–2):**
1. **(a) Whitelist noticias** — bloquea F3-3. _Default:_ La Número 12, La Nación Fútbol, Infobae Deportes, Doble Amarilla, Olé Boca, ESPN Deportes (`docs/09`).
2. **(b) Responsable legal** (nombre/entidad + `legal@`/`privacidad@` + jurisdicción) — bloquea F4-8. _Default:_ Xenova + `legal@azulyoro.com.ar` / `privacidad@azulyoro.com.ar`, CABA.
3. **(c) Edad mínima de registro** — bloquea F4-1/F4-8. _Default:_ 16 años.

## Review / bitácora
- 2026-08-12: documentación base (docs 00–11 + borradores legales).
- 2026-08-12 (iter A): docs 05–11 + borradores + README. Rutas con slugs SEO sin UUID.
- 2026-08-12 (iter B — planificación): contexto 100% leído. Prerrequisitos verificados (.NET 10.0.201, dotnet-ef, Node 22.19, pnpm 10.33, Postgres 17.9, git 2.51). **Backlog granular y detallado de 58 tareas (6 fases) generado** en este archivo; 57 tareas de build (F0-2…F5-5) espejadas en la task list del harness (F0-1 = decisión, sólo acá). Sin implementar — a la espera de indicación del usuario para arrancar.
- 2026-08-12 (iter C — **Fase 0 COMPLETA y verificada**): F0-2…F0-11 implementadas y probadas. Back .NET 10 VSA (`.slnx`, Api+Domain+Infrastructure), paquetes stack, `AppDbContext` (schema `app`) + Npgsql + migración `InitialCreate` aplicada a Postgres `azulyoro`, API `/health` 200. Front Next 16 + Tailwind v4 + next-intl v4 (`[locale]` es/en, `proxy.ts`, pathnames localizados, LocaleSwitcher), design system azul/oro OKLCH + fuentes Space Grotesk/Manrope + wordmark/monograma "AyO" (sin escudo) + dark mode, shell Header+Footer con disclaimer no oficial exacto (es/en) y atribución API-Football. Secrets vía user-secrets + `.env.example`. **Verificado:** back build, `ef database update` idempotente, API `/health` 200, front `pnpm build`, `/es` y `/en` 200, `/`→307 `/es`, disclaimer visible ambos locales. F0-1 (3 decisiones) diferida: se avanza con defaults documentados; se confirmarán antes de Fase 3/4 (no bloquean 0–2). **Próximo: Fase 1 (deportivo back + sync).**
