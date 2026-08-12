# Política de Cookies — Azul y Oro

> **BORRADOR — validar con abogado.** Ajustar la tabla a las cookies reales una vez definidos analytics/ads.

## Versión ES

### 1. Qué son
Las cookies son pequeños archivos que el Sitio almacena en su dispositivo para funcionar, recordar preferencias y medir el uso.

### 2. Categorías que usamos
| Categoría | Finalidad | Consentimiento | Ejemplos |
|---|---|---|---|
| **Necesarias** | Sesión de usuario, seguridad (CSRF), preferencia de idioma | No requiere (imprescindibles) | cookie de sesión Identity, `XSRF-TOKEN`, `locale` |
| **Analíticas** | Medir tráfico y uso de forma agregada | **Requiere consentimiento** | `[[Plausible (sin cookies) / GA4]]` |
| **Publicidad** | Anuncios y medición (si se activan) | **Requiere consentimiento** | `[[a definir — no en v1]]` |

> Si se usa **Plausible**, es sin cookies ni datos personales → reduce la necesidad de banner para analítica. Si se usa **GA4** o ads, el banner de consentimiento es obligatorio para no-esenciales.

### 3. Gestión del consentimiento
Al ingresar, un **banner** permite aceptar/rechazar las cookies no esenciales. Puede cambiar su elección en cualquier momento desde `[[el enlace "Preferencias de cookies" en el footer]]` o la configuración de su navegador. Las cookies no esenciales **no se activan** hasta obtener su consentimiento (enfoque GDPR-friendly).

### 4. Cookies de terceros
Los proveedores de analítica/publicidad y los embeds sociales (X, Instagram) pueden establecer sus propias cookies, regidas por sus políticas.

### 5. Duración
Las cookies de sesión expiran al cerrar el navegador; las persistentes tienen plazos definidos por cada finalidad.

### 6. Cambios
Actualizaremos esta política ante cambios en las cookies utilizadas.

---

## English version (summary)
Cookies are small files the Site stores to function, remember preferences, and measure usage.
**Necessary** (session, CSRF `XSRF-TOKEN`, `locale`) — no consent needed. **Analytics** (`[[Plausible cookieless / GA4]]`) — consent required. **Advertising** (`[[TBD, not in v1]]`) — consent required. A **consent banner** lets you accept/reject non-essential cookies; non-essential cookies do **not** fire before consent (GDPR-friendly). Third-party analytics/ads and social embeds may set their own cookies. Manage anytime via the footer "Cookie preferences" link or your browser.
