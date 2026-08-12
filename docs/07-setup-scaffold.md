# 07 — Setup & scaffold (Fase 0)

> Comandos concretos y estructura para levantar el proyecto. Windows dev (`B:\Xenova\azulyoro`), deploy Ubuntu.

## Prerrequisitos dev
- **.NET 10 SDK**, **Node 22+ / pnpm** (patrón Xenova usa pnpm en front), **PostgreSQL 16** local, **EF Core tools** (`dotnet tool install --global dotnet-ef`).
- Cuenta **API-Football** (free tier) → API key.
- Cuenta **Brevo** (free) → API key + verificación de dominio para DKIM (más adelante).

## Estructura de carpetas
```
B:\Xenova\azulyoro\
├─ docs\
├─ tasks\
├─ back\
│  ├─ Azulyoro.sln
│  └─ src\
│     ├─ Azulyoro.Api\            # host ASP.NET, endpoints (VSA slices en Features/)
│     │  └─ Features\
│     │     ├─ Matches\  Players\  Standings\  Articles\
│     │     ├─ Newsletter\  Auth\  Members\  Admin\  Legal\
│     ├─ Azulyoro.Domain\         # entidades, enums, value objects
│     ├─ Azulyoro.Infrastructure\ # EF DbContext, migraciones, ApiFootball client, scraping, email
│     └─ Azulyoro.Application\     # (si hace falta) contratos/behaviors compartidos
│  └─ tests\
│     ├─ Azulyoro.UnitTests\
│     └─ Azulyoro.IntegrationTests\
└─ front\
   ├─ app\[locale]\...            # rutas localizadas (ver docs/05)
   ├─ components\ui\              # componentes reutilizables
   ├─ messages\{es,en}.json       # textos next-intl
   ├─ lib\  (api client, auth, i18n)
   └─ next.config.ts  (output: 'standalone')
```

## Comandos scaffold — back
```bash
cd B:/Xenova/azulyoro/back
dotnet new sln -n Azulyoro
dotnet new webapi -n Azulyoro.Api -o src/Azulyoro.Api --use-minimal-apis
dotnet new classlib -n Azulyoro.Domain -o src/Azulyoro.Domain
dotnet new classlib -n Azulyoro.Infrastructure -o src/Azulyoro.Infrastructure
dotnet sln add src/**/**.csproj
# Paquetes clave
dotnet add src/Azulyoro.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Azulyoro.Api package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/Azulyoro.Api package Hangfire.AspNetCore
dotnet add src/Azulyoro.Api package Hangfire.PostgreSql
dotnet add src/Azulyoro.Infrastructure package AngleSharp
dotnet add src/Azulyoro.Infrastructure package Polly            # o Microsoft.Extensions.Http.Resilience
dotnet add src/Azulyoro.Infrastructure package HtmlSanitizer
dotnet add src/Azulyoro.Api package ErrorOr
# EF
dotnet ef migrations add Initial -p src/Azulyoro.Infrastructure -s src/Azulyoro.Api
dotnet ef database update -s src/Azulyoro.Api
```

## Comandos scaffold — front
```bash
cd B:/Xenova/azulyoro/front
pnpm create next-app@latest . --ts --app --tailwind --eslint --src-dir=false --import-alias "@/*"
pnpm add next-intl
# estructura [locale] + middleware next-intl + Tailwind v4 tokens azul/oro
```

## Config / secrets (dev)
`back/src/Azulyoro.Api/appsettings.Development.json` (NO commitear secrets reales; usar user-secrets):
```
dotnet user-secrets init -p src/Azulyoro.Api
dotnet user-secrets set "ApiFootball:Key" "xxx"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=azulyoro;Username=postgres;Password=xxx"
dotnet user-secrets set "Brevo:ApiKey" "xxx"
dotnet user-secrets set "Frontend:RevalidateSecret" "xxx"
```
Front `.env.local`: `NEXT_PUBLIC_API_URL=https://api.azulyoro.com.ar` (dev: `http://localhost:5000`), `REVALIDATE_SECRET=...`.

## .gitignore (unir plantillas .NET + Next)
- .NET: `bin/`, `obj/`, `*.user`, `appsettings.*.Local.json`.
- Next: `node_modules/`, `.next/`, `out/`, `.env*` (excepto `.env.example`).
- **Gotcha conocido** (ver lessons global): `[Rr]elease/` de la plantilla .NET puede tragarse carpetas feature llamadas `Release/` — no aplica salvo que exista; verificar con `git check-ignore -v` si algo no se trackea.

## Orden de arranque (Fase 0 → 1)
1. Postgres local + DB `azulyoro`.
2. Scaffold back + Domain + Infra + DbContext + migración `Initial` (entidades de `docs/04`).
3. Scaffold front + next-intl + Tailwind tokens + shell/layout + LocaleSwitcher.
4. Cliente API-Football + primer sync manual (teams/squad) para validar IDs.
5. Endpoints `/api/squad` + `/api/matches/next` → primera página real en front.

## Verificación Fase 0 (DoD)
- [ ] `dotnet build` verde, `dotnet ef database update` crea schema.
- [ ] Front `pnpm build` verde, rutas `/es` y `/en` responden.
- [ ] Un `GET /api/squad` devuelve plantel real sincronizado desde API-Football.
- [ ] Disclaimer no oficial visible en footer.
