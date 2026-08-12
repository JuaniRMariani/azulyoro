# 02 — Legal (sitio NO oficial · azulyoro.com.ar)

> **No es asesoramiento legal.** Es planificación de compliance para redactar disclaimers y páginas legales. Hacer revisar por abogado local (PI + datos personales) antes del launch. Ley de datos AR está en reforma (2026).

## Resumen ejecutivo (los 3 riesgos vivos)
1. **Reproducir el escudo/logo del club o implicar respaldo/afiliación.**
2. **Republicar cuerpos de artículos** en vez de reescribir con link-out.
3. **Fotos de agencias/prensa/oficiales** (Getty, Télam, AFP, fotos oficiales del club).

El nombre **"azulyoro"** (colores, no la marca) es la mejor decisión de mitigación tomada.

## 1. Marca / Brand
- Protegidos: "Boca Juniors", "Xeneize", "La Bombonera", el **escudo**, colores-como-logo oficial. En AR el titular puede exigir judicialmente el cese **sin probar daño ni mala fe**.
- **Uso referencial defendible:** usar las palabras "Boca Juniors" descriptivamente en el texto de artículos ✅. Zona peligrosa: reproducir el escudo como logo/favicon, implicar sponsorship, usar el nombre como identidad de marca propia.
- **Hacer:** logo **propio original**; **NO** escudo en ningún lado (incl. favicon). Colores azul y oro **no son marca** → estilar el sitio azul/oro está bien.
- **Nombre de marca:** "Azul y Oro" — nunca "Boca [X]", "Boca Oficial", "Club Boca".
- **No comprar** "Boca Juniors" como keyword de ads (fallo AR lo consideró ilegal).
- **Disclaimer en footer de cada página + About:**
  > *"Sitio no oficial. No afiliado, patrocinado ni avalado por el Club Atlético Boca Juniors ni sus entidades relacionadas. Todas las marcas pertenecen a sus respectivos titulares."*

## 2. Noticias / Copyright (Ley 11.723)
- **Art. 28:** las noticias de interés general pueden usarse/retransmitirse; si se publica la **versión original** hay que citar la fuente. Notas sin firma con exclusividad = propiedad del medio.
- **Art. 29:** notas firmadas = propiedad del autor.
- **Art. 10:** derecho de cita (hasta ~1.000 palabras, sólo lo indispensable, obra nueva genuinamente creativa).

| Modo | Veredicto | Por qué |
|---|---|---|
| (a) Copiar cuerpos de artículos | **Alto riesgo — evitar** | Reproduce expresión protegida. "Hechos libres" ≠ "texto libre". |
| (b) Titular + resumen corto + link-out | **Defendible con cuidado** | Titular/hechos + resumen propio + link + crédito. Citas mínimas. Modelo agregador estándar. |
| (c) Reescritura original citando fuentes | **Lo más seguro** | Reportás los hechos (libres) con tus palabras, acreditando. Hacia esto empuja el CMS moderado. |

**CMS moderado — reglas:**
- Guardar `source_name` + `source_url`; render "Fuente: [medio]" con link-out. **Nunca** auto-publicar cuerpos scrapeados.
- Regla editorial: **reescribir, no pegar**. Máximo 1–2 frases entre comillas con atribución.
- **Rumores de fichajes:** siempre atribuir ("según [periodista/medio]") y etiquetar *versión no confirmada* (higiene de copyright + difamación).
- Respetar `robots.txt` / ToS de la fuente (scraping que viola ToS suma riesgo contractual).

## 3. Datos personales
- **Régimen:** Ley 25.326 + Decreto 1558/2001, enforcement **AAIP**. AR mantiene **adecuación UE**. Reforma en curso (alinear con GDPR, eliminar registro de bases) **aún no sancionada** → rige la ley actual.
- **Consentimiento marketing/newsletter:** ley apunta a **opt-in previo, expreso, informado**; el decreto históricamente permitía opt-out. **Best practice: opt-in** (checkbox destildado). **Double opt-in** (email de confirmación) = gold standard.
- **Anti-spam (Disp. 4/2009):** todo mail marketing debe indicar que puede pedir **remoción/bloqueo** y **cómo**; **link de baja** funcional (one-click); publicidad pura puede requerir rótulo **"publicidad"**.
- **Derechos ARCO** (Acceso, Rectificación, Cancelación, Oposición): la política debe decir cómo ejercerlos (email de contacto), gratis, + que pueden reclamar a la **AAIP**.
- **Política de Privacidad debe informar (Art. 6):** identidad/contacto del **responsable**; qué datos (email, cuenta, cookies, IP, analytics) y **finalidades**; base legal/consentimiento; **terceros** (proveedor de email, ad networks, analytics, API deportiva) y **transferencias internacionales**; **retención**; **ARCO + AAIP**; **cookies**; **menores** (age gate recomendado).
- **Registro de bases (RNBD):** técnicamente exigible bajo ley actual (se espera eliminación por reforma). Confirmar con abogado.
- **Cookies:** banner con propósito/categorías (necesarias/analytics/publicidad) y consentimiento para no-esenciales.
- **GDPR-friendly (visitantes UE):** agregar base legal, derecho a supresión/portabilidad/oposición, contacto de privacidad, compromiso de notificación de brechas, consentimiento opt-in antes de cookies no-esenciales.

## 4. Páginas legales requeridas
**A. Términos y Condiciones:** descripción + aceptación; reglas de cuenta/edad; conducta prohibida/moderación UGC (comentarios); PI (contenido propio vs marcas de terceros); **disclaimer de datos de terceros "as is"** (scores/resultados/live/noticias/rumores sin garantía de exactitud ni responsabilidad); caveat de rumores; ads/newsletter (naturaleza comercial); limitación de responsabilidad + indemnidad; links a terceros; **ley y jurisdicción argentina**; cambios; contacto.

**B. Cláusula "sitio no oficial"** (en T&C + footer + About) — ver texto en §1.

**C. Política de Privacidad** — todo lo de §3.

**D. Aviso Legal / Takedown:** email de contacto + proceso para titulares que reporten infracción; compromiso de **remover** ante notificación válida (notice-and-takedown) + contra-notificación. (DMCA es de EE.UU., no vinculante en AR, pero es best practice y a menudo requerido por hosts/CDN; incluir vía de reclamo bajo ley AR.)

**E. Política de Cookies** (puede fusionarse con Privacidad).

## 5. Imágenes / Media
- **Fuentes seguras:** (1) media **licenciada por la API deportiva** (verificar que la licencia permita display editorial); (2) **Wikimedia/Creative Commons** — chequear licencia exacta (CC-BY requiere atribución; **evitar NC** en sitio monetizado); (3) fotos **propias**.
- **Evitar** fotos de agencias/medios (AP, AFP, Reuters, Getty, Télam, fotos oficiales del club) — licencia por uso, enforcement agresivo, top de cartas de intimación.
- Tratar toda foto como **copyright por default**; publicar sólo con licencia/CC.
- **Embeds vs hotlinking:** embeber posts sociales con **widget oficial** (menor riesgo) — no strip de atribución, remover ante pedido. **Hotlinking** de imágenes crudas = peor (sin atribución, robo de ancho de banda, posible infracción). Preferir **link-outs + embeds oficiales**.

## Checklist pre-launch
**Marca:** logo propio · sin escudo (incl favicon) · marca "Azul y Oro" · disclaimer en footer + About · no comprar keyword "Boca".
**Noticias:** CMS fuerza reescribir · "Fuente: [medio]" + link-out · rumores etiquetados · respetar robots.txt.
**Datos:** Privacidad con Art. 6 · opt-in (idealmente double) · baja one-click en cada mail · sección ARCO + AAIP · banner cookies · age gate · confirmar RNBD con abogado · add-ons GDPR.
**Páginas:** T&C (incl "as is" + no oficial) · Privacidad (+cookies) · Aviso Legal con takedown.
**Media:** sólo API/CC-no-NC/propias · sin fotos de agencia/oficiales · social vía embed oficial · atribución guardada + takedown-ready.

## Fuentes
- Ley 11.723 (actualizada): https://www.argentina.gob.ar/normativa/nacional/ley-11723-42755/actualizacion
- Ley 25.326 / AAIP: https://www.argentina.gob.ar/aaip/datospersonales
- Reforma datos 2026: https://www.diariojudicial.com/news-103126-proteccion-de-datos-personales-sigue-siendo-suficiente-la-ley-25326-en-2026
- Opt-in vs opt-out: https://lermanszlak.com/privacy-and-email-marketing-practical-recommendations-for-collecting-personal-data-and-using-it-legally-is-opt-out-still-enough/
- Marcas AR: https://jbbabogados.com.ar/uso-indebido-de-marcas-registradas-en-argentina-que-hacer-si-usan-una-marca-igual-o-parecida-a-la-tuya/
