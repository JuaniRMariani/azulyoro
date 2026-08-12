# 04 — Modelo de datos (propuesta v1)

> Postgres. Guardamos **todo** lo de la API en tablas propias y servimos desde ahí (nunca pegarle a la API en page-load). IDs externos de API-Football se guardan como `*_ext_id` para re-sync idempotente. Timestamps `created_at`/`updated_at` en todas.

## Convenciones
- PK: `id` (uuid v7 o bigint identity — decidir; propongo **uuid v7** por consistencia con otros proyectos).
- `*_ext_id` (int) = id de API-Football, con **unique index** para upsert.
- Traducciones de contenido editorial (noticias, páginas): tabla hija `*_translations` con `locale` (`es`/`en`). Datos deportivos NO se traducen (nombres propios).
- Enums como texto (Postgres) o smallint mapeado en EF.

## Módulo Deportivo (sync desde API-Football)

### `seasons`
`id`, `year` (int, unique), `is_current` (bool).

### `competitions`  (liga/copa)
`id`, `ext_id` (unique), `name`, `type` (`league`|`cup`), `country`, `logo_url`.
Seed: Primera=128, Copa Argentina=130, Libertadores=13, Sudamericana=11.

### `teams`
`id`, `ext_id` (unique), `name`, `short_name`, `logo_url`, `founded`, `venue_name`, `venue_city`, `is_tracked` (bool — Boca=true). Boca `ext_id=451`.

### `players`
`id`, `ext_id` (unique), `team_id` (FK, plantel actual), `name`, `firstname`, `lastname`, `birth_date`, `nationality`, `height`, `weight`, `position` (`GK|DEF|MID|FWD`), `number`, `photo_url`, `is_active`.

### `fixtures`  (partido)
`id`, `ext_id` (unique), `competition_id` (FK), `season_id` (FK), `round`, `date_utc`, `status` (`NS|1H|HT|2H|ET|P|FT|PST|CANC|…`), `elapsed` (int null), `venue_name`,
`home_team_id` (FK), `away_team_id` (FK), `home_goals` (int null), `away_goals` (int null),
`ht_home`,`ht_away`,`ft_home`,`ft_away` (marcadores por período), `is_boca` (bool, computed/flag), `last_synced_at`.
Index: `(date_utc)`, `(status)`, `(is_boca, date_utc)`.

### `fixture_events`  (goles/tarjetas/cambios)
`id`, `fixture_id` (FK), `ext_seq` (orden), `minute` (int), `extra_minute` (int null), `team_id` (FK), `player_id` (FK null), `assist_player_id` (FK null), `type` (`Goal|Card|subst|Var`), `detail`, `comments`.
Unique: `(fixture_id, ext_seq)`.

### `fixture_lineups`
`id`, `fixture_id` (FK), `team_id` (FK), `formation`, `coach_name`.
### `fixture_lineup_players`
`id`, `lineup_id` (FK), `player_id` (FK), `is_starter` (bool), `grid` (pos), `number`.

### `fixture_player_stats`  (stats por jugador por partido)
`id`, `fixture_id` (FK), `player_id` (FK), `team_id` (FK), `minutes`, `rating` (numeric), `goals`, `assists`, `shots_total`, `shots_on`, `passes`, `passes_accuracy`, `tackles`, `yellow`, `red`. (subset — ampliable).

### `player_season_stats`
`id`, `player_id` (FK), `competition_id` (FK), `season_id` (FK), `appearances`, `minutes`, `goals`, `assists`, `yellow`, `red`, `rating`.
Unique: `(player_id, competition_id, season_id)`.

### `standings`
`id`, `competition_id` (FK), `season_id` (FK), `team_id` (FK), `rank`, `points`, `goals_diff`, `played`, `win`, `draw`, `lose`, `goals_for`, `goals_against`, `form` (str), `group_name` (null).
Unique: `(competition_id, season_id, team_id, group_name)`.

## Módulo Contenido (noticias/CMS)

### `staging_articles`  (scraping, pre-moderación)
`id`, `source_name`, `source_url`, `url_hash` (unique), `title_hash` (simhash), `title`, `excerpt`, `clean_content`, `image_url`, `published_at_source`, `scraped_at`, `status` (`Pending|Approved|Rejected`), `category` (`news|rumor`), `reviewed_by` (FK user null), `reviewed_at`.

### `articles`  (publicado)
`id`, `slug` (unique), `category` (`news|rumor|editorial`), `status` (`Draft|Published|Archived`), `cover_image_url`, `source_name`, `source_url`, `author_user_id` (FK), `is_members_only` (bool), `published_at`, `staging_id` (FK null, trazabilidad).
### `article_translations`
`id`, `article_id` (FK), `locale` (`es|en`), `title`, `summary`, `body_html`, `meta_title`, `meta_description`. Unique `(article_id, locale)`.
### `tags`, `article_tags`  (M:N) — opcional v1.

## Módulo Usuarios / Socios

### `users`  (ASP.NET Identity extiende)
`id`, `email` (unique), `email_confirmed`, `display_name`, `password_hash`, `role` (`Member|Editor|Admin`), `locale_pref`, `created_at`, `last_login_at`, `is_active`.
> Identity provee la base; agregamos `display_name`, `role`, `locale_pref`.

### `newsletter_subscribers`  (independiente de users)
`id`, `email` (unique), `status` (`pending|confirmed|unsubscribed`), `locale`, `confirm_token_hash`, `confirmed_at`, `confirmed_ip`, `unsubscribed_at`, `created_at`.
### `newsletter_campaigns` (fase 2) — `id`,`subject`,`body`,`sent_at`,`recipients_count`.

## Módulo Legal / Config

### `legal_pages`
`id`, `slug` (`terms|privacy|legal-notice|cookies`), `locale`, `title`, `body_html`, `version`, `effective_date`, `updated_at`. Unique `(slug, locale)`.

## Infra
- **Hangfire** crea su propio schema `hangfire.*` en el mismo Postgres.
- Tabla `sync_state` opcional: `id`, `resource` (`fixtures|standings|squad|…`), `last_run_at`, `last_ok_at`, `last_error`, para orquestar re-sync condicional.

## Notas de sync (ver `01-fuentes-datos §5`)
- Estático (plantel, teams) → 1x/día.
- Semi (standings, próximos fixtures, season stats) → 30–60 min.
- Live (fixture en curso) → 30–60s, bundle events+lineups+stats, corte en `FT`.
- Upsert por `ext_id`; `last_synced_at` para no reprocesar.
