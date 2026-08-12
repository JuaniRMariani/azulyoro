# 06 — Contrato de API (back .NET → front Next)

> API REST en `api.azulyoro.com.ar`. VSA (un slice por endpoint). Respuestas JSON, camelCase. Auth por cookie `HttpOnly` (Identity). Errores con `ErrorOr`/ProblemDetails. Paginación `?page=&pageSize=`. Locale de contenido editorial vía `?locale=es|en` (default `es`).

## Convenciones
- Base pública (datos servidos desde Postgres, nunca proxy directo a API-Football).
- `GET` cacheable (headers `Cache-Control` alineados a la estrategia de sync).
- IDs propios (uuid). Fechas ISO-8601 UTC.
- Envelope de lista: `{ items: [...], page, pageSize, total }`.

## Público — Deportivo
```
GET  /api/matches?status=upcoming|finished|live&competitionId=&from=&to=&page=&pageSize=
GET  /api/matches/next                 → próximo partido de Boca
GET  /api/matches/live                 → partido(s) en curso (o 204 si ninguno)
GET  /api/matches/{id}                 → detalle (marcador, venue, estado)
GET  /api/matches/{id}/events          → goles/tarjetas/cambios (ordenados)
GET  /api/matches/{id}/lineups         → formaciones + titulares/suplentes
GET  /api/matches/{id}/player-stats    → stats por jugador del partido
GET  /api/squad                        → plantel actual (agrupable por posición)
GET  /api/players/{id}                 → ficha jugador
GET  /api/players/{id}/stats?season=   → stats temporada por competición
GET  /api/standings?competitionId=&season=  → tabla posiciones
GET  /api/competitions                 → competiciones (Primera/Copas)
```

## Público — Contenido
```
GET  /api/articles?category=news|rumor|editorial&locale=&page=&pageSize=
GET  /api/articles/{slug}?locale=       → detalle (body, fuente, link-out)
GET  /api/articles/featured?locale=     → destacadas home
GET  /api/legal/{slug}?locale=          → terms|privacy|legal-notice|cookies
```

## Público — Newsletter
```
POST /api/newsletter/subscribe          { email, locale }           → 202 (envía DOI)
GET  /api/newsletter/confirm?token=     → confirma (redirige a página)
GET  /api/newsletter/unsubscribe?token= → baja one-click
```

## Auth / Socios (cookie)
```
POST /api/auth/register                 { email, password, displayName, locale } → envía verificación
GET  /api/auth/verify-email?token=      → confirma cuenta
POST /api/auth/login                    { email, password }         → set cookie
POST /api/auth/logout
GET  /api/auth/me                       → perfil sesión actual (o 401)
POST /api/auth/forgot-password          { email }
POST /api/auth/reset-password           { token, password }
GET  /api/auth/google                   → OAuth start
GET  /api/auth/google/callback
GET  /api/auth/csrf                     → token antiforgery (X-XSRF-TOKEN)
PUT  /api/me/profile                    { displayName, localePref }
POST /api/me/change-password            { current, new }
GET  /api/members/content?page=         → artículos is_members_only (auth requerida)
```

## Admin CMS (rol Editor/Admin)
```
GET  /api/admin/moderation?status=Pending&category=   → cola staging
POST /api/admin/moderation/{id}/approve  → promueve a articles (borrador)
POST /api/admin/moderation/{id}/reject
GET  /api/admin/articles / POST / PUT /{id} / DELETE   → CRUD + traducciones es/en
POST /api/admin/articles/{id}/publish    → publica + dispara revalidate webhook
GET  /api/admin/sources / POST / PUT     → fuentes de scraping (url, rss, activo, rate)
GET  /api/admin/newsletter/subscribers?status=
GET  /api/admin/legal / PUT /{slug}/{locale}
GET  /api/admin/users / PUT /{id}/role
GET  /api/admin/sync/state               → estado jobs de sync
POST /api/admin/sync/{resource}/run      → forzar sync manual
```

## Revalidation (Next ← back)
```
POST {FRONT}/api/revalidate  { secret, tags: ["article:{id}","news-list"] }
```
El back llama al Route Handler de Next al publicar/actualizar contenido. Secret compartido.

## Seguridad transversal
- CORS: origin allow-list (`https://azulyoro.com.ar`) + `AllowCredentials`.
- Cookie: `Secure; HttpOnly; SameSite=Lax; Domain=.azulyoro.com.ar`.
- CSRF: antiforgery en todas las mutaciones (header `X-XSRF-TOKEN`).
- Rate-limit en `POST` públicos (register, login, subscribe).
- `/admin/*` y `/hangfire` detrás de rol Admin/Editor.
