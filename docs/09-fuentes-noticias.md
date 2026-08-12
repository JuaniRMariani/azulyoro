# 09 — Fuentes de noticias (scraping RSS-first)

> Verificado 2026-08-12. "VERIFIED" = feed fetcheado devolviendo RSS 2.0 válido con items recientes. "INFERRED" = patrón fuerte (mismo Arc XP que un hermano verificado) pero el WAF bloqueó el fetch de research → **verificar desde el server** (UA realista, egress AR).

## Nota de plataforma (clave para el scraper)
La mayoría de los medios grandes AR corren **Arc XP**. Feed en path predecible:
`https://<dominio>/arc/outboundfeeds/rss/?outputType=xml` (site-wide) y
`.../arc/outboundfeeds/rss/category/<path>/?outputType=xml` (por sección).
Confirmado en **La Nación** e **Infobae**. Olé, Clarín, TyC, TN probablemente exponen lo mismo (WAF bloqueó al fetcher de research; un scraper real con UA normal debería llegar).

## Tabla

| Fuente | Sección Boca | RSS | Estado / notas |
|---|---|---|---|
| **La Número 12** (fan) | lanumero12.com.ar | `https://www.lanumero12.com.ar/feed/` | **VERIFIED** — 100% Boca, horario (WordPress). Mejor RSS puro-Boca. |
| **La Nación Fútbol** | lanacion.com.ar/deportes/futbol/ | `https://www.lanacion.com.ar/arc/outboundfeeds/rss/category/deportes/futbol/?outputType=xml` | **VERIFIED** — calidad + robots-friendly. No Boca-específico → filtrar por keyword. |
| **Infobae Deportes** | infobae.com/deportes/ | `https://www.infobae.com/arc/outboundfeeds/rss/category/deportes/?outputType=xml` | **VERIFIED** — horario, robots-friendly. Filtrar Boca. |
| **Doble Amarilla** | dobleamarilla.com.ar | `https://www.dobleamarilla.com.ar/rss` | **VERIFIED** — 20 items, fuerte en **mercado de pases/rumores**. (`/feed` da 404; usar `/rss`). |
| **Olé Boca** | ole.com.ar/boca-juniors | `https://www.ole.com.ar/arc/outboundfeeds/rss/?outputType=xml` (+ probar `/category/boca-juniors/`) | **INFERRED** (Arc XP). WAF bloqueó. Marca #1 deportiva AR + sección Boca dedicada → vale verificar. |
| **Clarín Deportes** | clarin.com/deportes | `https://www.clarin.com/arc/outboundfeeds/rss/category/deportes/?outputType=xml` | **INFERRED** (Arc XP). WAF. |
| **TN Deportes** | tn.com.ar/deportes/ | `https://www.tn.com.ar/arc/outboundfeeds/rss/?outputType=xml` | **INFERRED** (Arc XP, Grupo Clarín). Verificar server. |
| **ESPN Deportes** | espndeportes.espn.com | `https://espndeportes.espn.com/espn/rss/news` | **VERIFIED** — 40+ items ES, LatAm amplio (no Boca ni AR) → filtro fuerte. |
| **TyC Sports Boca** | tycsports.com/boca-juniors.html | probar Arc; sino HTML | **UNCONFIRMED / HTML-scrape**. Arc XP pero sin RSS público surgido; WAF pesado. |
| **Diario Popular** | diariopopular.com.ar/deportes | — | Arc 404 → **HTML-scrape only**. |
| **Planeta Boca Juniors** (fan) | planetabj.com | — (`/rss/feed/` da HTML) | **HTML-scrape only**. Contenido puro-Boca. |
| **SoyBoca** (fan) | soyboca.com.ar | `https://www.soyboca.com.ar/rss.xml` | **INFERRED** (Feedspot); fetch ECONNREFUSED transitorio. Re-testear. |
| **Cadena 3 Deportes** | cadena3.com | `http://cadena3.com/rss/Deportes.xml` | **VERIFIED** (índice). Nacional, no Boca. Opcional. |
| ~~"Mundo Azul y Oro"~~ | — | — | **No existe** como medio (es apodo). Skip. |

## Rumores (mercado de pases)
- **Doble Amarilla** (tiene RSS) = mejor fuente scrapeable de rumores.
- Periodistas viven en **X/Twitter** (@cesarluismerlo, @gastonedul, @dobleamarilla, etc.) → **X no tiene RSS usable** y su API es paga → **no** intentar RSS. Capturar el feed del medio donde escriben columnas.

## Whitelist recomendada MVP (5–8)
1. **La Número 12** — `/feed/` — VERIFIED, puro Boca. (RSS)
2. **La Nación Fútbol** — Arc feed — VERIFIED, calidad, robots-friendly. (RSS)
3. **Infobae Deportes** — Arc feed — VERIFIED, robots-friendly. (RSS)
4. **Doble Amarilla** — `/rss` — VERIFIED, rumores. (RSS)
5. **Olé Boca** — Arc feed — INFERRED, verificar server; autoridad de marca. (RSS/HTML)
6. **ESPN Deportes** — `/espn/rss/news` — VERIFIED; sólo con filtro Boca fuerte. (RSS)
7. *(opc.)* **TN Deportes** — Arc feed — INFERRED, verificar server. (RSS)
8. *(opc.)* **SoyBoca** — `rss.xml` — INFERRED, re-testear. (RSS)

## Caveats de build
- **WAF:** todas las propiedades de Grupo Clarín (Olé, Clarín, TyC, TN) bloquearon al fetcher de research. El scraper necesita **UA realista** e idealmente **egress residencial/AR** para llegar a feeds y HTML. Presupuestar esto.
- **Ningún feed de medio grande está filtrado por Boca** — son de sección (Fútbol/Deportes) → **filtrado por keyword** al ingerir (`Boca`, nombres de jugadores, `Xeneize`).
- **robots.txt** verificado OK para La Nación, Infobae, Doble Amarilla (no bloquean feed/sección). Re-chequear robots de sitios Clarín-group desde server antes de HTML fallback.
- **Legal:** RSS = diseñado para sindicación (lo más seguro). En HTML fallback: guardar link + titular + excerpt corto, atribuir, link-back. Nunca republicar el artículo completo. (Ver `02-legal §2`.)

## Config de fuentes (tabla `sources` del admin)
Campos: `name`, `homepage`, `boca_section_url`, `rss_url` (nullable), `type` (`rss|html`), `active`, `rate_limit_seconds`, `keyword_filter` (`Boca|Xeneize|...`), `robots_ok` (bool), `notes`.
Seed inicial: las 4–6 VERIFIED como `rss` activas; las INFERRED como `rss` inactivas hasta verificar en server; TyC/Planeta como `html` inactivas (fase posterior).

## Fuentes
- Feedspot Boca: https://rss.feedspot.com/boca_juniors_rss_feeds/
- Feedspot La Nación: https://rss.feedspot.com/lanacion_rss_feeds/
