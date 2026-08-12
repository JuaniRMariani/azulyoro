# Lessons · azulyoro

> Patrones aprendidos y correcciones del usuario. Revisar al inicio de cada sesión.

## Reglas fijas del proyecto
- **Sitio NO oficial**: nunca usar el escudo del club, nunca implicar afiliación. Marca propia "Azul y Oro".
- **Noticias**: reescribir + citar fuente + link-out. **Nunca** pegar cuerpos scrapeados. Rumores etiquetados "no confirmado".
- **Fotos**: sólo API-licenciadas / CC no-NC / propias. Nunca agencias/oficiales.
- **Datos deportivos**: guardar en Postgres propio, servir desde ahí. Nunca pegarle a la API en page-load.
- **Git**: no tocar identidad/auth (regla global). Commits `type(module): mensaje` en inglés.

## Notas técnicas / known-issues
- **Solución .NET 10 = `.slnx`** (formato XML nuevo), no `.sln`. Los comandos deben apuntar a `Azulyoro.slnx`.
- **Microsoft.OpenApi NU1903 (known-issue, aceptado):** `Microsoft.AspNetCore.OpenApi` 10.0.5 depende de `Microsoft.OpenApi` 2.x. La 2.0.0 y 2.1.0 arrastran el advisory GHSA-v5pm-xwqc-g5wc (DoS en el reader/parser). La 3.x lo parchea pero **rompe** el source-generator de XML comments (`IOpenApiMediaType.Example` pasa a read-only) y es un cambio de major incompatible con AspNetCore.OpenApi 10 → riesgo de `MissingMethodException` en runtime. Como solo **generamos** OpenAPI (no parseamos documentos no confiables), la explotabilidad es nula. Pin en 2.1.0 (build verde). **Trigger de bump:** cuando ASP.NET Core publique un `Microsoft.AspNetCore.OpenApi` compilado contra OpenApi 3.x, subir ambos juntos.
- `Newtonsoft.Json` 11.0.1 (transitiva de Hangfire) tenía NU1903 → override explícito a 13.0.4 en Api (EF Design pide ≥13.0.4).
- **`UseSnakeCaseNamingConvention()` también renombra la tabla de historial** (`MigrationId`→`migration_id`). Si ya aplicaste una migración SIN la convención, el `database update` rompe con `42703: no existe la columna migration_id`. Fix (pre-deploy): `DROP SCHEMA app CASCADE`, borrar carpeta Migrations y regenerar una sola `Initial` CON la convención activa. Activar la convención ANTES de la primera migración.

## Notas front (Next 16 + next-intl v4)
- **`middleware.ts` está DEPRECADO en Next 16** → renombrar a `proxy.ts` (mismo contenido, default export + `config`). El build tira warning si sigue como middleware.
- **LocaleSwitcher con pathnames dinámicos:** `router.replace(pathname, {locale})` no tipa rutas `[slug]`. Usar el patrón oficial `router.replace({pathname, params}, {locale})` con `useParams()` + `// @ts-expect-error`.
- **Pathnames localizados**: definidos en `i18n/routing.ts` `pathnames{}`. La KEY es el pathname interno (= carpeta en `app/[locale]/`). Ej: carpeta `app/[locale]/partidos/` sirve `/es/partidos` y `/en/matches`.
- **Root layout passthrough**: `app/layout.tsx` devuelve `children`; el `<html lang>` real vive en `app/[locale]/layout.tsx`.

## Correcciones del usuario
- **Paleta (2026-08-12):** usar azul MÁS OSCURO (navy tipo camiseta temporada pasada) + oro más anaranjado pero amarillo, oro-forward. Tokens en `globals.css` (azul hue ~257, oro hue ~78-80). Monograma `icon.svg`/`logo.svg` navy `#152a63` + oro `#f4b322`.
- **Escudo del club (2026-08-12) — OVERRIDE de la regla "sin escudo":** el usuario decidió usar el escudo real del club (sin estrellas), asumiendo la responsabilidad legal, con disclaimer "no oficial" siempre visible. **Matiz legal aclarado al usuario:** "asociación civil" ≠ marca de dominio público; el escudo es marca registrada (INPI) y el disclaimer reduce confusión pero NO es licencia. **Límite mantenido:** NO descargo/redistribuyo el asset con copyright; el usuario coloca el archivo manualmente en `public/brand/logo.svg` (monograma propio como default). El `BrandMark` renderiza ese archivo. Disclaimer no oficial se mantiene en el Footer.
