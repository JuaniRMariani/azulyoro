# 11 — Design System "Azul y Oro"

> Identidad **propia** (sin escudo del club, sin "Boca" en logo). Azul y oro son colores, no marca → uso libre. Tailwind v4 (tokens en `@layer base`, ver regla global TW v4). Al construir UI, pasar por `/ui-ux-pro-max` + `/frontend-design`.

## Marca
- **Nombre:** "Azul y Oro". **Logo:** wordmark/monograma **propio** (ej. "AyO" o "A|O" con barra dorada). Nunca el escudo ni tipografía oficial del club.
- **Tono:** apasionado pero editorial/confiable (E-E-A-T). Hincha con criterio, no fanpage ruidosa.
- **Favicon:** monograma propio (no escudo).

## Paleta (OKLCH — light + dark)
Base azul profundo + oro cálido, con neutros para lectura larga de contenido.
```
/* Brand */
--azul-900: oklch(0.28 0.09 250);   /* azul institucional profundo */
--azul-700: oklch(0.42 0.12 248);   /* azul primario */
--azul-500: oklch(0.55 0.13 246);   /* azul interactivo */
--oro-500:  oklch(0.80 0.13 85);    /* oro primario (acento) */
--oro-400:  oklch(0.86 0.11 88);    /* oro claro */
--oro-600:  oklch(0.72 0.13 82);    /* oro hover/borde */

/* Semantic (light) */
--background: oklch(0.99 0.005 250);
--foreground: oklch(0.20 0.02 250);
--primary:   var(--azul-700);
--accent:    var(--oro-500);
--muted:     oklch(0.95 0.01 250);
--border:    oklch(0.90 0.01 250);
--success: oklch(0.65 0.15 150); --warning: oklch(0.80 0.14 80);
--danger:  oklch(0.58 0.19 27);   --info: var(--azul-500);
--live:    oklch(0.62 0.20 27);   /* rojo "EN VIVO" */

/* Dark: fondo azul-carbón, oro se mantiene como acento */
--background(dark): oklch(0.18 0.03 250);
--foreground(dark): oklch(0.95 0.01 250);
```
- **Contraste:** oro sobre azul y texto sobre fondos → cumplir **WCAG AA** (≥4.5:1 texto). El oro puro sobre blanco tiene bajo contraste → usar `--oro-600` para texto/enlaces sobre claro.
- **Dark mode:** de arranque (audiencia mobile, uso nocturno en partidos).

## Tipografía
- **Display/titulares:** una grotesca con carácter (ej. **Space Grotesk** o **Archivo**) — impacto deportivo.
- **Texto/UI:** una sans legible para lectura larga (ej. **Inter** o **Manrope**).
- **Números/stats:** variante tabular (`font-variant-numeric: tabular-nums`) para tablas y marcadores.
- `font-display: swap`, preload de la display para LCP.

## Escala & layout
- Espaciado base 4px; contenedor de lectura ~72ch para artículos.
- Grid responsive mobile-first. Breakpoints Tailwind estándar.
- Radios: `sm` tarjetas, `full` badges. Sombras sutiles; en dark, borde > sombra.

## Componentes clave (mapa a `components/ui/`)
- **LiveScoreBadge** — pill roja `--live` con minuto, animación de pulso sutil.
- **MatchCard** — escudos (de API), marcador tabular, competición, fecha/hora local AR, estado.
- **FixtureList / ResultsList** — agrupado por fecha/competición.
- **StandingsTable** — resaltar fila de Boca con borde oro; tabular-nums.
- **PlayerCard** — foto, número, posición, nacionalidad (bandera).
- **PlayerStatsTable** — stats temporada/partido, tabular.
- **ArticleCard** — imagen 16:9, categoría (badge), **SourceAttribution** ("Fuente: X" + link), fecha, autor.
- **RumorBadge** — badge ámbar "RUMOR / No confirmado".
- **NewsletterForm** — email + checkbox opt-in (destildado) + estado double opt-in.
- **UnofficialDisclaimer** — banda discreta en footer (texto de `02-legal §1`).
- **LocaleSwitcher**, **Breadcrumbs** (con schema), **Skeleton**, **EmptyState**, **StatCard**.

## Patrones UI (reglas globales del usuario)
- IDs de registros: UUID corto (6 chars, mayúsculas, sin guiones) en tablas/admin. **Nunca** UUID completo en modales/cards salvo pedido explícito.
- **Nada** `window.confirm/alert` → usar `ConfirmDialog` + `Toast` (`useConfirm()`/`useToast()`).
- Campos obligatorios con `(*)` (`<Label required>`).
- Consistencia de tamaños con módulos de referencia cuando se pida "como X".

## Accesibilidad & rendimiento
- WCAG AA, foco visible, navegación por teclado, `alt` en imágenes, `lang` por locale.
- Reservar alto de imágenes/embeds (CLS<0.1). `next/image` con `width/height`. Diferir widgets sociales.
- Respetar `prefers-reduced-motion` (animación del LiveBadge).

## Assets / imágenes (legal)
- Fotos: sólo **API-licenciadas / CC no-NC / propias**. Placeholders propios cuando falte foto. Ver `02-legal §5`.
