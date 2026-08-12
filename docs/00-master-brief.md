# 00 — Master Brief · azulyoro

> **Entrada del proyecto.** Sitio **NO OFICIAL** de fans de Boca Juniors. Leer esto primero, luego el resto de `docs/` y `../tasks/todo.md`.

## Índice de documentación
| Doc | Contenido |
|---|---|
| `00-master-brief.md` | Este archivo: decisiones, alcance, riesgos, índice |
| `01-fuentes-datos.md` | API-Football (elección, endpoints, sync, términos) |
| `02-legal.md` | Marca, copyright, datos personales, checklist |
| `03-arquitectura.md` | Stack, jobs, scraping, email, SEO, auth, deploy |
| `04-modelo-datos.md` | Esquema Postgres completo |
| `05-arquitectura-informacion.md` | Sitemap, rutas es/en, render, flujos, componentes |
| `06-contrato-api.md` | Endpoints REST back→front |
| `07-setup-scaffold.md` | Comandos y estructura de Fase 0 |
| `08-boca-referencia.md` | Datos reales de Boca (seed de contenido) |
| `09-fuentes-noticias.md` | Fuentes con RSS verificados + whitelist |
| `10-seo-contenido.md` | Keywords es/en, pillars, SEO técnico, E-E-A-T |
| `11-design-system.md` | Paleta azul/oro, tipografía, componentes |
| `borradores-legales/` | T&C, Privacidad, Aviso Legal+Takedown, Cookies (ES+EN) |

## Qué es
Portal de contenido e info del plantel/partidos de Boca Juniors, de **carácter no oficial**. Marca propia **"Azul y Oro"** (colores, no la marca del club). Dominio **azulyoro.com.ar**. Bilingüe **es/en**.

## Decisiones tomadas (con el usuario)
| Tema | Decisión |
|---|---|
| Nombre / carpeta / dominio | `azulyoro` · `B:\Xenova\azulyoro` · `azulyoro.com.ar` |
| Stack | .NET 10 (back, VSA + EF Core) + Next 16 (front, next-intl) + **Postgres** |
| Datos deportivos | **API-Football** (api-sports.io), free tier para dev → Pro (~USD 19/mo) en prod |
| Zona socios | **Login + zona privada gratis** (ASP.NET Identity, cookie), sin pagos en MVP |
| Noticias/rumores | Scraper (RSS-first) → **staging** → **CMS con moderación humana** → publicado |
| Legal | Borradores bilingües de T&C + Privacidad + Aviso Legal (validar con abogado) |
| Idioma | Español (AR) + Inglés (next-intl, rutas localizadas) |
| Hosting | VPS Ubuntu único, Nginx + systemd, **sin Docker**, Cloudflare adelante |
| Subdominios | Front `azulyoro.com.ar` · API `api.azulyoro.com.ar` · envío `mail.azulyoro.com.ar` |

## Alcance MVP (v1)
1. **Partidos**: próximos, resultados, fixture/calendario, **live scores** + eventos + formaciones.
2. **Plantel**: lista de jugadores, fichas, stats por jugador (temporada + partido).
3. **Posiciones** de la liga.
4. **Noticias + rumores de fichajes** (sólo Boca): scraping moderado, reescritura + link-out.
5. **Newsletter** con double opt-in.
6. **Socios**: registro/login + zona privada gratis (contenido exclusivo).
7. **Legales**: T&C, Privacidad, Aviso Legal, disclaimer "no oficial" en footer.
8. **SEO** desde el día 1 (SSG/ISR, hreflang, JSON-LD, sitemap).

## Fuera de MVP (fases siguientes)
- Precios/venta de entradas (partidos locales) — requiere definir fuente/legalidad.
- Membresía **paga** (Mercado Pago).
- Historia extensa / efemérides / palmarés curado a mano.
- App móvil.
- Comentarios/UGC, foro.
- Fantasy / predicciones.

## Riesgos y mitigaciones clave
- **Marca:** logo propio, **sin escudo**, disclaimer no oficial. → `02-legal §1`.
- **Copyright noticias:** reescribir + citar + link-out, nunca pegar cuerpos. → `02-legal §2`.
- **Datos personales:** opt-in/double opt-in, Privacidad completa, ARCO/AAIP. → `02-legal §3`.
- **Fotos:** sólo API-licenciadas / CC no-NC / propias. → `02-legal §5`.
- **Licencia API:** API-Football disclama derechos comerciales → atribución en footer, no representar como oficial. → `01-fuentes-datos §4`.
- **Rate limit API:** guardar todo en Postgres, servir del propio DB, polling tiered. → `01-fuentes-datos §5`.

## Estructura de repos (a crear)
```
B:\Xenova\azulyoro\
├─ docs\            # esta documentación
├─ tasks\           # todo.md, lessons.md
├─ back\            # solución .NET 10 (VSA)  → azulyoro.com.ar API
│   └─ src\Azulyoro.Api\  (+ Application, Domain, Infrastructure según VSA)
└─ front\           # Next.js 16 (App Router, next-intl)
```

## Preguntas abiertas para el usuario
Ver `../tasks/todo.md` §"Decisiones pendientes".
