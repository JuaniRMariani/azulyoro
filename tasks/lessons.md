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
- `Newtonsoft.Json` 11.0.1 (transitiva de Hangfire) tenía NU1903 → override explícito a 13.0.3 en Api. Resuelto.

## Correcciones del usuario
- (vacío — agregar a medida que surjan)
