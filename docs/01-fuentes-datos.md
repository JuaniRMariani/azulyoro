# 01 — Fuentes de datos deportivos

> Proyecto **azulyoro** (sitio NO oficial de Boca Juniors). Investigación: ago 2026.
> Objetivo: alimentar fixtures, resultados, live scores, plantel, stats de jugadores y posiciones.

## TL;DR / Decisión

**Proveedor elegido: API-Football (api-sports.io), plan Pro (~USD 19/mes, 7.500 req/día, live in-play incluido).**

Es el único con cobertura completa y barata de **Liga Profesional Argentina + Copa Argentina + Copa Libertadores + Copa Sudamericana**, incluyendo eventos en vivo, formaciones y stats por jugador. SportMonks es más prolijo (licencia comercial más limpia) pero 3–10x más caro para cobertura CONMEBOL equivalente. Football-Data.org queda descartado (no cubre Argentina). Sportradar es enterprise (USD 1k+/mes).

- **Prototipar:** arrancar en **free tier (100 req/día, todos los endpoints, live incluido)**.
- **Prod:** subir a **Pro** antes del launch. Ultra (USD 29/mes, 75k/día) como headroom.
- Agregar **footer de atribución** a API-Sports/API-Football.

## Comparativa

| | API-Football | SportMonks | Football-Data.org | Sportradar |
|---|---|---|---|---|
| Liga Profesional (Primera) | Sí (`league=128`) | Sí (según plan) | No | Enterprise |
| Copa Argentina | Sí (`league=130`) | Parcial | No | Sí |
| Copa Libertadores | Sí (`league=13`) | Growth+ (€99+) | No | Sí |
| Copa Sudamericana | Sí (`league=11`) | Growth+ | No | Sí |
| Boca fixtures | Sí (`team=451`) | Sí | Solo Europa | Sí |
| **Live scores/eventos** | Sí, refresh 15s, **todos los tiers** | Sí, sub-segundo, tiers pagos | Pago, sin eventos jugador | Premium |
| Formaciones | Sí | Sí | No | Sí |
| Plantel + stats jugador | Sí | Sí | No | Sí |
| Free tier | **100 req/día, TODOS los endpoints** | Solo trial 14 días | 10 req/min, sin Argentina | No |
| Pago útil más barato | **Pro ~USD 19/mo → 7.500 req/día** | Growth €99/mo (para CONMEBOL) | — (igual no cubre AR) | Custom $1k+ |
| Auth | header `x-apisports-key` | `?api_token=` | `X-Auth-Token` | OAuth/key |

**Notas clave:**
- API-Football: todos los planes desbloquean todos los endpoints incl. live; los tiers sólo cambian volumen y profundidad histórica. Mismo precio directo o vía RapidAPI.
- SportMonks Starter (€29) sólo 5 ligas → no alcanza para las 4 competencias + margen. CONMEBOL confirmado desde Growth (€99). Bill por request/entidad/hora.

## Endpoints clave (API-Football v3)

Base: `https://v3.football.api-sports.io` · Auth: `x-apisports-key: <KEY>`
Headers de rate-limit a monitorear: `x-ratelimit-requests-remaining` (diario) y `X-RateLimit-Remaining` (por minuto).
IDs: Boca = `team=451`, Primera = `league=128`. **Verificar al registrarse** con `/teams?search=Boca` y `/leagues?country=Argentina`.

| Propósito | Endpoint | Campos clave |
|---|---|---|
| Fixtures por equipo+temporada | `GET /fixtures?team=451&season=2026` | `fixture{id,date,status.short,venue}`, `teams`, `goals`, `score` |
| Fixtures por liga | `GET /fixtures?league=128&season=2026` | ídem lista |
| **Live** | `GET /fixtures?live=all` (o `live=128-13-11-130`) | `status.elapsed` poblado; refresh ~15s |
| Fixture único (bundle) | `GET /fixtures?id={id}` | embebe `events`,`lineups`,`statistics`,`players` |
| Formaciones | `GET /fixtures/lineups?fixture={id}` | `formation`, `startXI[]`, `substitutes[]`, `coach` |
| Eventos (goles/tarjetas/cambios) | `GET /fixtures/events?fixture={id}` | `time.elapsed`, `player`, `assist`, `type`, `detail` |
| Stats por jugador (partido) | `GET /fixtures/players?fixture={id}` | `players[].statistics[]` (min, rating, tiros, pases…) |
| Stats jugador (temporada) | `GET /players?team=451&season=2026` (paginado) | `player{id,name,age,nationality}`, `statistics[]` |
| Plantel | `GET /players/squads?team=451` | `players[]` con `number`, `position` |
| Posiciones | `GET /standings?league=128&season=2026` | `rank`,`points`,`goalsDiff`,`form` |
| Info equipo | `GET /teams?id=451` | `team{name,logo,founded}`, `venue` |

## Términos de uso (gotchas)

**API-Football** ([terms](https://www.api-football.com/terms)):
- **Disclama derechos comerciales**: no otorga licencia para publicar datos de competición; vos sos responsable de autorizaciones de rights holders (AFA/CONMEBOL). En la práctica miles de sitios ad-supported lo usan así — postura normal de la industria, pero **no representar la data como oficialmente licenciada**.
- **Atribución** esperada (link en footer). Logos/fotos son de sus dueños.
- **Caching permitido** (se espera para no reventar quota). Prohibido revender/redistribuir el feed crudo.

**SportMonks**: uso comercial **explícitamente permitido** (licencia más limpia), storage permitido, prohibido revender. Caching sugerido: estático ~24h, dinámico 5–10 min, live TTL muy corto.

## Diseño de sincronización en .NET (dentro de 7.500 req/día)

`BackgroundService` (o Hangfire/Quartz) con **frecuencias por tipo de dato** y polling live **sólo si hay partido en curso**:

- **Estático (cache 12–24h):** plantel, info equipo, metadata liga → 1x/día (pocas req).
- **Semi-estático (cache 30–60 min):** posiciones, próximos fixtures, stats temporada → poll 30–60 min (~24–48 req/día c/u).
- **Live (sólo mientras hay partido):** al startup, chequear fixtures del día; si `status.short` ∈ {1H,HT,2H,ET…}, poll `GET /fixtures?id={id}` (bundle events+lineups+stats) **cada 60s**. Partido 2h ≈ 120 req. 5 partidos simultáneos ≈ 600 req. Cortar al `FT`. Sin polling de noche.
- **Backoff:** leer `x-ratelimit-requests-remaining` en cada respuesta; si baja, ampliar intervalos.

**Presupuesto diario aprox.:** estático+semi ≈ 200–400 req; día de partido ≈ +300–800 req. Cómodo dentro de 7.500.

**Regla de oro:** guardar todo en **Postgres propio** y servir el sitio desde ahí — **nunca** pegarle a la API en el page-load del usuario. Usar `IHttpClientFactory` (typed client) + **Polly** (retry/circuit-breaker en 429/5xx) + `updated_at` por entidad para re-sync condicional.

## Fuentes
- Pricing: https://www.api-football.com/pricing · https://www.sportmonks.com/football-api/plans-pricing/ · https://www.football-data.org/pricing
- Docs v3: https://www.api-football.com/documentation-v3 · https://api-sports.io/documentation/football/v3
- Terms: https://www.api-football.com/terms · https://www.sportmonks.com/terms-of-service/
- Caching best practices SportMonks: https://docs.sportmonks.com/football/welcome/best-practices
