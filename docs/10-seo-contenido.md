# 10 — SEO & estrategia de contenido

> Sitio NO oficial, bilingüe ES-AR / EN, Next 16. Regla madre: **long-tail primero, pillars segundo**. Los head terms de marca los dominan medios oficiales (Olé/TyC/ESPN, DA 70-90+) — no se pueden ganar 12-24 meses. **El gran hueco = contenido editorial de Boca en INGLÉS** (hoy sólo lo cubren agregadores sin voz).

## 1. Keywords ES (AR) por intención
- **Informacional Partidos:** `boca hoy`, `próximo partido de boca`, `resultado de boca`, `boca en vivo`, `hora del partido de boca hoy` (extrema competencia — usar widget/ISR, no pelear head).
- **Plantel/Jugadores:** `plantel de boca 2026`, `[jugador] boca juniors`, `capitán de boca 2026` (long-tail rankeable, FAQPage).
- **Historia/Estadio:** `historia de boca juniors`, `palmarés boca títulos`, `capacidad la bombonera` (featured snippet).
- **Posiciones:** `posiciones liga profesional 2026`, `tabla de posiciones boca` (widget Google — contextualizar).
- **Transaccional:** `entradas la bombonera` (NO competir → link-out oficial/Ticketek), `cómo comprar entradas boca` (guía).
- **Fichajes:** `fichajes boca 2026`, `rumores boca`, `[jugador] a boca rumor` (artículos individuales = mejor ángulo).

**Long-tail rankeable 6-12m:** `cuántos títulos tiene boca juniors`, `boca vs river historial completo`, `goleador histórico de boca`, `[jugador] ficha técnica boca`, `cómo llegar a la bombonera`, `historia del superclásico`, `formación de boca hoy`.

## 2. Keywords EN (diferenciación principal)
`boca juniors squad/roster 2026`, `boca juniors fixtures/results 2026`, `boca juniors news`, `boca juniors history`, `boca juniors vs river plate` (guía Superclásico evergreen), `la bombonera stadium`, `boca juniors titles/trophies/founded` (FAQ fast-win), `boca juniors tickets / stadium tour` (turismo, baja competencia), `[player] boca juniors profile`.
**Discover EN:** "why boca juniors is the biggest club in argentina", "el superclásico explained for international fans", "boca juniors transfer rumors: what's true".

## 3. Pillars & arquitectura (render)
| Pillar | Páginas | Render | Revalidate |
|---|---|---|---|
| Partidos | próximos, resultados, `/partido/[slug]` | ISR (SSR pre-match live) | 5min pre / on-demand post |
| Plantel | hub, `/jugadores/[slug]`, cuerpo técnico | SSG + ISR | 24h / ventana pases |
| Competencias | posiciones, copas | ISR | 1h días de partido |
| Fichajes | hub, `/rumores/[slug]` | ISR | 6h |
| Noticias | index, `/noticias/[slug]` | ISR | 1h (Discover) |
| Historia | hub, palmarés, `/superclasico`, eras | SSG | deploy / al ganar título |
| Estadio | guía, cómo llegar, entradas, tour | SSG | — |
| Glosario/Fans intl | glosario, `/para-fans-internacionales`, `/en/for-fans` | SSG (FAQPage) | — |

> **Nota routing:** estos slugs SEO-preferidos (`/partido/[slug]`, `/jugadores/[slug]` sin ID) refinan a `docs/05`. Decisión: usar slug SEO puro + ID interno resuelto server-side (evitar exponer UUID en URL, alineado a regla UI global). **Reconciliar `05` con estos slugs.**
> **Live scores:** NO replicar Sofascore/Flashscore — embeber su widget (lo permiten) o deep-link. El valor propio es el **contexto editorial**, no el dato.

**Internal linking:** match↔jugadores↔competición↔noticias↔H2H; breadcrumbs (schema) en todas; bloque "También leé" (3 relacionadas) al pie.

## 4. Competencia — dónde SÍ se puede ganar
- **Tier 1 (0-6m):** contenido Boca en **inglés** (todos los pillars); long-tail ES; FAQPage; guía estadio para turistas.
- **Tier 2 (6-18m):** análisis/opinión de rumores (con "credibility rating"), perfiles narrativos de jugadores, deep-dive Superclásico, páginas de campaña Libertadores/Sudamericana.
- **Tier 3 (18m+, con backlinks):** fixtures/resultados junto a agregadores.
- **NO targetear:** `boca juniors`, `boca hoy`, `ver boca en vivo`, `la bombonera entradas`, breaking news vs Olé/TyC.
- **Ángulos diferenciales:** "the international fan's home for Boca" · profundidad > velocidad (análisis táctico 1.200 palabras 2h después vs recap de 150) · perfiles narrativos · autoridad Superclásico · periodismo de opinión de fichajes · voz de comunidad (encuestas, MOTM).

## 5. SEO técnico
- **JSON-LD:** `SportsEvent` (match), `SportsTeam` (club/plantel), `NewsArticle` (noticias), `FAQPage` (historia/estadio/glosario), `BreadcrumbList` (todas). Validar en Rich Results Test. (Ejemplos completos en `docs/03` y anexo below.)
- **hreflang:** `es-AR` primario, `en` (no en-US), `x-default`→ES. Cada ES tiene par EN y viceversa — **no** hreflang parcial (Google lo marca error). Vía `alternates.languages` en `generateMetadata`.
- **Sitemap segmentado:** index → static / players / matches (ventana 6m) / news (últimas 1000, horario) / en. Priority: home 1.0, news 0.9, match 0.8, player 0.7, static 0.6.
- **Core Web Vitals (live-score):** LCP<2.5s (preload hero/next-match, `next/image priority`, no iframes pesados above-fold); CLS<0.1 (reservar alto de iframes/ads, contenedor fijo para embeds Sofascore); INP<200ms (RSC en perfiles/historia, mínima hidratación, diferir widgets sociales).
- **Canonical:** filtros → canonical al no-filtrado; paginado self-canonical; strip UTM. ES/EN traducciones reales (no MT thin).
- **robots.txt:** Allow /, Disallow `/api/ /_next/ /admin/ /*?*utm_*`, Sitemap.
- **Google News:** un fan site califica si cumple Publisher Center (ownership transparente, bylines, análisis original, `NewsArticle` schema) → submit en Publisher Center.
- **Discover:** E-E-A-T + imágenes 1200px+ + `<meta name="robots" content="max-image-preview:large, max-snippet:-1, max-video-preview:-1">` en artículos.

## 6. Cadencia & E-E-A-T (sitio no oficial)
- **Bylines obligatorios:** cada artículo con autor + página de perfil (nombre, foto, bio "hincha de Boca desde…", redes, listado de notas).
- **About:** quién opera azulyoro, disclaimer de no-afiliación, estándares editoriales (fuentes, fact-check, correcciones), contacto/tip line.
- **Sourcing:** nunca republicar wire/gacetilla textual; atribuir a fuente primaria; rumores citando al periodista/medio; política de correcciones visible.
- **Cadencia:** previa por partido (2-3/sem), reporte post-partido (<4h, más largo que competidores), análisis de rumores (2-3/sem con credibility rating), perfil jugador (2-3/sem narrativo), historia/cultura (1-2/sem evergreen), **EN 3-5/sem** (prioridad sin competencia EN), update semanal de plantel/lesiones.
- **Evitar thin content:** test por página → *"¿esto me dice algo que no obtengo de Transfermarkt/Sofascore/Wikipedia en 10s?"*. Si no, es thin.
- **Link building realista:** X community AR, r/soccer + r/BocaJuniors, cross-links Substack, podcasts CABJ con byline, ser citado en Wikipedia. **No** intercambio/pago de links.

## 7. Quick wins (primeros 30 días)
`SportsTeam` JSON-LD home+plantel · `FAQPage` en 10 preguntas fácticas · `BreadcrumbList` global · hreflang ES/EN OK · `max-image-preview:large` en artículos · robots.txt + sitemap a Search Console · About + disclaimer · perfiles de autor · submit a Publisher Center · canonicals en filtros/paginado · **5 explainers EN** de competencia cero · **guía La Bombonera (EN)** turismo long-tail.

## Anexo — JSON-LD (ver ejemplos completos)
`SportsEvent` (name, startDate, location StadiumOrArena, competitor[SportsTeam], eventStatus), `SportsTeam` (foundingDate 1905-04-03, homeLocation La Bombonera, sameAs oficial+wikidata Q48278), `NewsArticle` (headline, datePublished/Modified, author.url, publisher.logo, inLanguage es-AR), `FAQPage`, `BreadcrumbList`. Implementar como `<script type="application/ld+json">` en server component.

## Fuentes
planetabj.com · lanumero12.com.ar · sofascore/flashscore/transfermarkt · Google Search Console + Publisher Center policies.
