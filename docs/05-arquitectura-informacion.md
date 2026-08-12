# 05 — Arquitectura de información (sitemap + UX)

> Inventario de páginas, rutas (bilingües), estrategia de render y flujos de usuario. Base para front Next 16.

## Rutas (localizadas es/en)
Prefijo de locale: `/es/...` y `/en/...`. `x-default` → `/es`. Slugs de rutas localizados donde aporta SEO.

| Sección | Ruta ES | Ruta EN | Render | Nota |
|---|---|---|---|---|
| Home | `/es` | `/en` | ISR (revalidate corto) | Próximo partido + últimos resultados + noticias |
| Partidos (hub) | `/es/partidos` | `/en/matches` | ISR | Calendario + resultados + próximos |
| Fixture/calendario | `/es/partidos/fixture` | `/en/matches/fixture` | ISR | Por competición/temporada |
| Resultados | `/es/partidos/resultados` | `/en/matches/results` | ISR | Históricos |
| Detalle partido | `/es/partido/{slug}` | `/en/match/{slug}` | ISR→**SSR si live** | Slug SEO (ej. `boca-river-2026-08-24`); ID interno resuelto server-side, **sin UUID en URL**. Eventos, formaciones, stats |
| **En vivo** | `/es/en-vivo` | `/en/live` | SSR/streaming | Redirige al partido live si hay |
| Plantel | `/es/plantel` | `/en/squad` | SSG+ISR | Grid jugadores por posición |
| Ficha jugador | `/es/jugadores/{slug}` | `/en/players/{slug}` | SSG+ISR | Slug SEO (ej. `leandro-paredes`), **sin UUID en URL**. Bio + stats temporada/partido |
| Posiciones | `/es/posiciones` | `/en/standings` | ISR | Tabla liga |
| Noticias (hub) | `/es/noticias` | `/en/news` | ISR + revalidateTag | Listado paginado |
| Detalle noticia | `/es/noticias/{slug}` | `/en/news/{slug}` | ISR + revalidateTag | Reescrito + "Fuente" + link-out |
| Rumores/fichajes | `/es/fichajes` | `/en/transfers` | ISR | Etiquetados "no confirmado" |
| Historia | `/es/historia` | `/en/history` | SSG | Timeline + palmarés |
| Palmarés | `/es/palmares` | `/en/honours` | SSG | Títulos |
| La Bombonera | `/es/la-bombonera` | `/en/stadium` | SSG | Estadio |
| Ídolos | `/es/idolos` | `/en/legends` | SSG | Leyendas |
| Newsletter | `/es/newsletter` | `/en/newsletter` | SSG + form | Alta double opt-in |
| Confirmar newsletter | `/es/newsletter/confirmar` | `/en/newsletter/confirm` | SSR | `?token=` |
| Baja newsletter | `/es/newsletter/baja` | `/en/newsletter/unsubscribe` | SSR | one-click |
| **Socios (zona privada)** | `/es/socios/*` | `/en/members/*` | SSR (auth) | Contenido exclusivo |
| Login | `/es/ingresar` | `/en/login` | SSR | email/pass + Google |
| Registro | `/es/registrarse` | `/en/register` | SSR | + verificación email |
| Perfil | `/es/perfil` | `/en/profile` | SSR (auth) | Datos + preferencias |
| Sobre el sitio | `/es/sobre-el-sitio` | `/en/about` | SSG | Disclaimer no oficial |
| Términos | `/es/terminos` | `/en/terms` | SSG (desde `legal_pages`) | |
| Privacidad | `/es/privacidad` | `/en/privacy` | SSG | |
| Aviso legal | `/es/aviso-legal` | `/en/legal-notice` | SSG | + takedown |
| Cookies | `/es/cookies` | `/en/cookies` | SSG | |
| Contacto | `/es/contacto` | `/en/contact` | SSR (form) | |
| Sitemap/robots | `/sitemap.xml` `/robots.txt` | — | dinámico | Incluye alternates |

## Admin CMS (no localizado, `api.` o subruta protegida)
| Vista | Ruta | Rol |
|---|---|---|
| Login admin | `/admin/login` | Editor/Admin |
| Dashboard | `/admin` | Editor/Admin |
| Cola moderación noticias | `/admin/moderacion` | Editor |
| Editor de artículo (es/en) | `/admin/articulos/{id}` | Editor |
| Gestión fuentes scraping | `/admin/fuentes` | Admin |
| Suscriptores newsletter | `/admin/newsletter` | Admin |
| Páginas legales | `/admin/legales` | Admin |
| Usuarios/roles | `/admin/usuarios` | Admin |
| Hangfire dashboard | `/hangfire` | Admin |
| Sync deportivo (estado) | `/admin/sync` | Admin |

## Navegación global
- **Header:** Logo "Azul y Oro" · Partidos · Plantel · Posiciones · Noticias · Fichajes · Historia · [En vivo badge si hay partido] · switch idioma · Login/Socios.
- **Footer:** disclaimer no oficial · legales · newsletter · atribución API-Football · redes · contacto/takedown.

## Flujos clave
1. **Ver partido en vivo:** Home badge "EN VIVO" → `/en-vivo` → detalle con score+eventos actualizando (polling/SSE) → al `FT` pasa a ISR.
2. **Registro socio:** `/registrarse` → email+pass → mail verificación (Brevo, token) → confirma → sesión cookie → acceso `/socios`.
3. **Alta newsletter (DOI):** form → `pending` + mail → click confirm → `confirmed`. Cada envío con baja one-click.
4. **Publicar noticia:** scraper → `staging` → editor modera/reescribe/traduce → publica → `revalidateTag` → visible en `/noticias`.

## Componentes UI reutilizables (front)
`MatchCard`, `LiveScoreBadge`, `FixtureList`, `StandingsTable`, `PlayerCard`, `PlayerStatsTable`, `ArticleCard`, `SourceAttribution`, `RumorBadge`, `NewsletterForm`, `LocaleSwitcher`, `UnofficialDisclaimer`, `Breadcrumbs`, `Skeleton`, `EmptyState`.
