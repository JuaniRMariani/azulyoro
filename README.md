# azulyoro

Sitio **NO OFICIAL** de fans de Club Atlético Boca Juniors. Marca propia **"Azul y Oro"** · dominio **azulyoro.com.ar** · bilingüe es/en.

> ⚠️ Sitio no afiliado, patrocinado ni avalado por el Club Atlético Boca Juniors. Las marcas pertenecen a sus titulares.

## Stack
- **Back:** .NET 10 (Vertical Slice Architecture, EF Core) — `api.azulyoro.com.ar`
- **Front:** Next.js 16 (App Router, next-intl es/en, Tailwind v4) — `azulyoro.com.ar`
- **DB:** PostgreSQL · **Datos:** API-Football · **Email:** Brevo · **Jobs:** Hangfire + BackgroundService
- **Deploy:** VPS Ubuntu único, Nginx + systemd (sin Docker), Cloudflare

## Estructura
```
docs/    → documentación (empezar por docs/00-master-brief.md)
tasks/   → todo.md (plan por fases) + lessons.md
back/    → solución .NET (por crear en Fase 0)
front/   → app Next.js (por crear en Fase 0)
```

## Estado
Fase de planificación completa. Documentación e investigación cargadas en `docs/`. Próximo: Fase 0 (scaffold). Ver `tasks/todo.md`.

## Documentación
Ver [docs/00-master-brief.md](docs/00-master-brief.md) para el índice completo.
