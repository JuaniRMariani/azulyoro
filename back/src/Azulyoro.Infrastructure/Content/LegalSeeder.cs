using System.Text;
using Azulyoro.Domain.Entities;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Infrastructure.Content;

/// <summary>
/// Idempotent seeder for the legal pages (terms/privacy/legal-notice/cookies),
/// both locales. Content is the resolved version of the drafts under
/// docs/borradores-legales (every <c>[[...]]</c> placeholder resolved to the
/// production values). Markdown is converted to simple HTML at seed time.
/// </summary>
public static class LegalSeeder
{
    private static readonly DateOnly EffectiveDate = new(2026, 8, 12);

    public static async Task SeedLegalAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.LegalPages.AnyAsync(ct))
            return;

        var pages = new List<LegalPage>();
        foreach (var (slug, titleEs, mdEs, titleEn, mdEn) in Drafts())
        {
            pages.Add(Build(slug, "es", titleEs, mdEs));
            pages.Add(Build(slug, "en", titleEn, mdEn));
        }

        // Defense-in-depth: never seed an unresolved placeholder.
        foreach (var p in pages)
        {
            if (p.BodyHtml.Contains("[[") || p.Title.Contains("[["))
                throw new InvalidOperationException(
                    $"Unresolved [[placeholder]] in legal page '{p.Slug}' ({p.Locale}).");
        }

        db.LegalPages.AddRange(pages);
        await db.SaveChangesAsync(ct);
    }

    private static LegalPage Build(string slug, string locale, string title, string markdown) =>
        new()
        {
            Slug = slug,
            Locale = locale,
            Title = title,
            BodyHtml = MarkdownToHtml(markdown),
            Version = 1,
            EffectiveDate = EffectiveDate,
        };

    /// <summary>
    /// Minimal, purpose-built markdown → HTML converter covering the subset used
    /// by the legal drafts: ###/#### headings, paragraphs, `-` bullet lists,
    /// `> ` blockquotes, pipe tables, and inline <strong> (**bold**) + <code>.
    /// </summary>
    private static string MarkdownToHtml(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder();

        var listOpen = false;
        var paragraph = new StringBuilder();
        List<string[]>? table = null;

        void FlushParagraph()
        {
            if (paragraph.Length > 0)
            {
                sb.Append("<p>").Append(Inline(paragraph.ToString().Trim())).Append("</p>");
                paragraph.Clear();
            }
        }

        void CloseList()
        {
            if (listOpen) { sb.Append("</ul>"); listOpen = false; }
        }

        void FlushTable()
        {
            if (table is null) return;
            sb.Append("<table>");
            for (var r = 0; r < table.Count; r++)
            {
                if (r == 1) continue; // separator row
                var cell = r == 0 ? "th" : "td";
                sb.Append("<tr>");
                foreach (var c in table[r])
                    sb.Append('<').Append(cell).Append('>')
                      .Append(Inline(c.Trim())).Append("</").Append(cell).Append('>');
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            table = null;
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            var trimmed = line.TrimStart();

            // Table row.
            if (trimmed.StartsWith('|'))
            {
                FlushParagraph();
                CloseList();
                var cells = trimmed.Trim('|').Split('|');
                (table ??= new List<string[]>()).Add(cells);
                continue;
            }
            FlushTable();

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                CloseList();
                continue;
            }

            if (trimmed.StartsWith("#### "))
            {
                FlushParagraph(); CloseList();
                sb.Append("<h3>").Append(Inline(trimmed[5..].Trim())).Append("</h3>");
            }
            else if (trimmed.StartsWith("### "))
            {
                FlushParagraph(); CloseList();
                sb.Append("<h2>").Append(Inline(trimmed[4..].Trim())).Append("</h2>");
            }
            else if (trimmed.StartsWith("## "))
            {
                FlushParagraph(); CloseList();
                sb.Append("<h2>").Append(Inline(trimmed[3..].Trim())).Append("</h2>");
            }
            else if (trimmed.StartsWith("> "))
            {
                FlushParagraph(); CloseList();
                sb.Append("<blockquote>").Append(Inline(trimmed[2..].Trim())).Append("</blockquote>");
            }
            else if (trimmed.StartsWith("- "))
            {
                FlushParagraph();
                if (!listOpen) { sb.Append("<ul>"); listOpen = true; }
                sb.Append("<li>").Append(Inline(trimmed[2..].Trim())).Append("</li>");
            }
            else
            {
                CloseList();
                if (paragraph.Length > 0) paragraph.Append(' ');
                paragraph.Append(trimmed);
            }
        }

        FlushParagraph();
        CloseList();
        FlushTable();

        return sb.ToString();
    }

    /// <summary>Inline formatting: escape HTML, then apply **bold** and `code`.</summary>
    private static string Inline(string text)
    {
        var s = text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        s = ReplacePairs(s, "**", "<strong>", "</strong>");
        s = ReplacePairs(s, "`", "<code>", "</code>");
        return s;
    }

    private static string ReplacePairs(string s, string marker, string open, string close)
    {
        var sb = new StringBuilder(s.Length);
        var idx = 0;
        var opened = false;
        while (true)
        {
            var next = s.IndexOf(marker, idx, StringComparison.Ordinal);
            if (next < 0)
            {
                sb.Append(s, idx, s.Length - idx);
                break;
            }
            sb.Append(s, idx, next - idx);
            sb.Append(opened ? close : open);
            opened = !opened;
            idx = next + marker.Length;
        }
        return sb.ToString();
    }

    // --- Resolved drafts --------------------------------------------------
    // Placeholder resolution map applied:
    //   responsable/entidad          → Xenova
    //   jurisdicción                 → Ciudad Autónoma de Buenos Aires (CABA)
    //   edad mínima                  → 16
    //   email newsletter/proveedor   → Brevo
    //   analytics                    → Plausible (sin cookies)
    //   legal email                  → legal@azulyoro.com.ar
    //   privacy email                → privacidad@azulyoro.com.ar
    //   datos deportivos             → API-Football
    //   FECHA / effective date       → 2026-08-12
    //   anything "TBD / no en v1"    → clause omitted

    private static IEnumerable<(string Slug, string TitleEs, string MdEs, string TitleEn, string MdEn)> Drafts()
    {
        yield return (
            "terminos",
            "Términos y Condiciones",
            TermsEs,
            "Terms & Conditions",
            TermsEn);

        yield return (
            "privacidad",
            "Política de Privacidad",
            PrivacyEs,
            "Privacy Policy",
            PrivacyEn);

        yield return (
            "aviso-legal",
            "Aviso Legal y Política de Retirada de Contenido",
            LegalNoticeEs,
            "Legal Notice & Takedown Policy",
            LegalNoticeEn);

        yield return (
            "cookies",
            "Política de Cookies",
            CookiesEs,
            "Cookie Policy",
            CookiesEn);
    }

    private const string TermsEs = """
### 1. Aceptación
El uso de azulyoro.com.ar ("el Sitio") implica la aceptación plena de estos Términos y Condiciones y de la Política de Privacidad. Si no está de acuerdo, no utilice el Sitio.

### 2. Sitio no oficial (disclaimer)
Azul y Oro (azulyoro.com.ar) es un sitio de aficionados de carácter **NO OFICIAL**. No está afiliado, asociado, autorizado, patrocinado ni avalado, de forma alguna, por el Club Atlético Boca Juniors ni por ninguna de sus filiales o entidades relacionadas. Los nombres, marcas, escudos e imágenes relacionados con el club son propiedad de sus respectivos titulares y se utilizan únicamente con fines informativos y de referencia.

### 3. Objeto del Sitio
El Sitio ofrece información, estadísticas, resultados, noticias y contenidos de opinión referidos al club, un boletín (newsletter) y cuentas de usuario gratuitas con acceso a contenido para socios.

### 4. Cuentas de usuario
- El registro requiere datos veraces y una edad mínima de 16 años. Los menores requieren consentimiento de sus responsables.
- El usuario es responsable de la confidencialidad de sus credenciales y de la actividad en su cuenta.
- Está prohibido: usar el Sitio con fines ilícitos, vulnerar la seguridad, publicar contenido ofensivo/ilegal en comentarios (si los hubiere), automatizar accesos sin autorización.
- El Sitio puede suspender o cancelar cuentas ante incumplimientos.

### 5. Datos de terceros (limitación) — "tal cual"
Los marcadores, resultados, datos en vivo, formaciones, estadísticas, noticias y **rumores de fichajes** provienen de fuentes de terceros (una API deportiva licenciada y medios periodísticos). Se proveen **"tal cual" ("as is")**, sin garantía de exactitud, integridad ni actualidad. El Sitio no será responsable por decisiones tomadas en base a dichos datos. Los rumores son versiones no confirmadas de carácter especulativo, no hechos.

### 6. Propiedad intelectual
Los contenidos originales del Sitio (textos redactados, diseño, logo propio "Azul y Oro") son de su titular. Las marcas y contenidos de terceros pertenecen a sus dueños. Las citas y enlaces a fuentes se realizan conforme a la Ley 11.723.

### 7. Enlaces y publicidad
El Sitio puede contener enlaces a terceros y publicidad. No controla ni responde por contenidos externos. La naturaleza comercial (publicidad/afiliados) se divulga cuando corresponde.

### 8. Limitación de responsabilidad
El Sitio se ofrece "según disponibilidad", sin garantía de continuidad ni ausencia de errores. En la máxima medida permitida por la ley, el titular no responde por daños directos o indirectos derivados del uso del Sitio. El usuario mantiene indemne al titular frente a reclamos derivados de su uso indebido.

### 9. Ley aplicable y jurisdicción
Estos Términos se rigen por las leyes de la República Argentina. Ante cualquier controversia, las partes se someten a los tribunales ordinarios de la Ciudad Autónoma de Buenos Aires (CABA).

### 10. Modificaciones y contacto
El titular puede modificar estos Términos; la versión vigente se publica en el Sitio con su fecha. Contacto: legal@azulyoro.com.ar.
""";

    private const string TermsEn = """
### 1. Acceptance
Using azulyoro.com.ar ("the Site") means you fully accept these Terms & Conditions and the Privacy Policy. If you disagree, do not use the Site.

### 2. Unofficial site (disclaimer)
Azul y Oro (azulyoro.com.ar) is an **UNOFFICIAL** fan site. It is not affiliated with, associated with, authorized, sponsored or endorsed in any way by Club Atlético Boca Juniors or any of its affiliates or related entities. All club-related names, trademarks, crests and images belong to their respective owners and are used solely for informational and referential purposes.

### 3. Purpose
The Site provides information, statistics, results, news and opinion content about the club, a newsletter, and free user accounts with access to members' content.

### 4. User accounts
- Registration requires accurate data and a minimum age of 16 years. Minors require guardian consent.
- Users are responsible for the confidentiality of their credentials and for the activity on their account.
- The following is prohibited: using the Site for unlawful purposes, breaching security, posting offensive/illegal content in comments (if any), or automating access without authorization.
- The Site may suspend or cancel accounts for breaches.

### 5. Third-party data (limitation) — "as is"
Scores, results, live data, line-ups, statistics, news and **transfer rumors** come from third-party sources (a licensed sports API and media outlets). They are provided **"as is"**, without warranty of accuracy, completeness or timeliness. The Site is not liable for decisions made in reliance on such data. Rumors are unconfirmed, speculative content, not fact.

### 6. Intellectual property
The Site's original content (written text, design, the original "Azul y Oro" logo) belongs to its owner. Third-party marks and content belong to their owners. Quotations and source links follow Argentine Law 11.723.

### 7. Links and advertising
The Site may contain third-party links and advertising. It does not control or answer for external content. The commercial nature (ads/affiliates) is disclosed where applicable.

### 8. Limitation of liability
The Site is provided "as available", without warranty of uptime or error-free operation. To the maximum extent permitted by law, the owner is not liable for direct or indirect damages arising from use of the Site. Users hold the owner harmless against claims arising from their misuse.

### 9. Governing law & jurisdiction
These Terms are governed by the laws of the Argentine Republic. For any dispute, the parties submit to the ordinary courts of the Autonomous City of Buenos Aires (CABA).

### 10. Changes & contact
The owner may amend these Terms; the current version is posted on the Site with its date. Contact: legal@azulyoro.com.ar.
""";

    private const string PrivacyEs = """
### 1. Responsable
El responsable del tratamiento es Xenova, con contacto en privacidad@azulyoro.com.ar.

### 2. Qué datos recolectamos y con qué finalidad
| Dato | Finalidad | Base |
| Email (newsletter) | Envío del boletín | Consentimiento (opt-in / double opt-in) |
| Email, contraseña (hash), nombre visible | Cuenta de usuario / socios | Ejecución del servicio solicitado |
| Preferencia de idioma | Personalización | Interés legítimo / servicio |
| IP, logs técnicos, datos de navegación | Seguridad, prevención de abuso | Interés legítimo |
| Analítica (Plausible, sin cookies) | Medición agregada del uso | Interés legítimo |

La provisión de datos es **voluntaria**; sin los datos necesarios no podremos prestar ciertos servicios (cuenta, newsletter).

### 3. Terceros y transferencias internacionales
Compartimos datos con proveedores que nos asisten: proveedor de email (Brevo), analítica (Plausible, sin cookies) y la API deportiva (API-Football), que **no** recibe datos personales de usuarios. Algunos pueden implicar **transferencia internacional**; se aplican garantías adecuadas. Argentina posee adecuación ante la UE.

### 4. Conservación
Conservamos los datos mientras la cuenta/suscripción esté activa y según plazos legales. El usuario puede solicitar la baja/supresión en cualquier momento.

### 5. Derechos (ARCO)
El titular puede ejercer **Acceso, Rectificación, Actualización, Supresión y Oposición** escribiendo a privacidad@azulyoro.com.ar, de forma gratuita. La **AGENCIA DE ACCESO A LA INFORMACIÓN PÚBLICA (AAIP)**, órgano de control de la Ley 25.326, tiene la atribución de atender denuncias y reclamos.

> El titular de los datos personales tiene la facultad de ejercer el derecho de acceso a los mismos en forma gratuita a intervalos no inferiores a seis meses, salvo que se acredite un interés legítimo al efecto conforme lo establecido en el artículo 14, inciso 3 de la Ley N.º 25.326.

### 6. Newsletter (opt-in) y anti-spam
La suscripción es por **opt-in** con confirmación por email (**double opt-in**). Cada envío incluye un enlace de **baja (unsubscribe) de un clic**. Puede solicitar la remoción o bloqueo total/parcial de sus datos en cualquier momento.

### 7. Cookies
Usamos cookies necesarias (funcionamiento/sesión). La analítica se realiza con Plausible, que no utiliza cookies ni datos personales. Ver la **Política de Cookies**.

### 8. Menores
El Sitio no está dirigido a menores de 16 años; el registro requiere esa edad mínima o consentimiento de responsables.

### 9. Seguridad
Aplicamos medidas técnicas y organizativas razonables (hashing de contraseñas, cifrado en tránsito). Ante un incidente relevante actuaremos conforme a la normativa aplicable.

### 10. Cambios
Podemos actualizar esta Política; publicaremos la versión vigente con su fecha.

## Add-ons GDPR (visitantes UE)
- **Bases legales** explícitas (consentimiento / interés legítimo / ejecución de contrato).
- Derechos adicionales: **supresión ("olvido"), portabilidad, oposición, limitación**.
- Compromiso de **notificación de brechas**.
- Contacto de privacidad: privacidad@azulyoro.com.ar.
""";

    private const string PrivacyEn = """
### 1. Data controller
The data controller is Xenova, contact at privacidad@azulyoro.com.ar.

### 2. What data we collect and why
| Data | Purpose | Basis |
| Email (newsletter) | Sending the bulletin | Consent (opt-in / double opt-in) |
| Email, password (hash), display name | User account / members | Performance of the requested service |
| Language preference | Personalization | Legitimate interest / service |
| IP, technical logs, browsing data | Security, abuse prevention | Legitimate interest |
| Analytics (Plausible, cookieless) | Aggregate usage measurement | Legitimate interest |

Providing data is **voluntary**; without the necessary data we cannot provide certain services (account, newsletter).

### 3. Third parties and international transfers
We share data with providers that assist us: email provider (Brevo), analytics (Plausible, cookieless) and the sports API (API-Football), which does **not** receive users' personal data. Some may involve an **international transfer**; adequate safeguards apply. Argentina holds EU adequacy.

### 4. Retention
We retain data while the account/subscription is active and for legal terms. Users may request unsubscription/erasure at any time.

### 5. Rights (ARCO)
Data subjects may exercise **Access, Rectification, Update, Erasure and Objection** by writing to privacidad@azulyoro.com.ar, free of charge. The **AGENCY FOR ACCESS TO PUBLIC INFORMATION (AAIP)**, supervisory body of Law 25.326, is empowered to handle complaints and claims.

### 6. Newsletter (opt-in) and anti-spam
Subscription is by **opt-in** with email confirmation (**double opt-in**). Every email includes a **one-click unsubscribe** link. You may request removal or total/partial blocking of your data at any time.

### 7. Cookies
We use necessary cookies (operation/session). Analytics is done with Plausible, which uses no cookies or personal data. See the **Cookie Policy**.

### 8. Minors
The Site is not directed to persons under 16 years; registration requires that minimum age or guardian consent.

### 9. Security
We apply reasonable technical and organizational measures (password hashing, encryption in transit). In the event of a relevant incident we will act in accordance with applicable regulations.

### 10. Changes
We may update this Policy; we will post the current version with its date.

## GDPR add-ons (EU visitors)
- Explicit **legal bases** (consent / legitimate interest / contract performance).
- Additional rights: **erasure ("right to be forgotten"), portability, objection, restriction**.
- Commitment to **breach notification**.
- Privacy contact: privacidad@azulyoro.com.ar.
""";

    private const string LegalNoticeEs = """
### 1. Titularidad
azulyoro.com.ar es operado por Xenova. Contacto: legal@azulyoro.com.ar.

### 2. Carácter no oficial
Sitio de aficionados **NO OFICIAL**, sin relación con el Club Atlético Boca Juniors ni sus entidades. Las marcas, escudos, nombres e imágenes de terceros pertenecen a sus titulares y se usan con fines informativos y de referencia. Ver Términos §2.

### 3. Propiedad intelectual de terceros y derecho de cita
Los contenidos periodísticos citados o enlazados se utilizan conforme a la **Ley 11.723** (derecho de cita, art. 10; noticias de interés general con mención de fuente, art. 28). No reproducimos cuerpos completos de artículos ajenos; publicamos redacciones propias con atribución y enlace a la fuente original.

### 4. Notificación y retirada (notice-and-takedown)
Si usted es titular de derechos y considera que un contenido publicado infringe sus derechos (copyright, marca, imagen, datos personales), envíe una notificación a legal@azulyoro.com.ar incluyendo:
- Identificación del contenido y su URL en el Sitio.
- Identificación del derecho presuntamente infringido y su titularidad.
- Datos de contacto del reclamante.
- Declaración de buena fe.

Nos comprometemos a **revisar y, de corresponder, deshabilitar o retirar** el contenido con la mayor prontitud razonable tras una notificación válida. El usuario/autor afectado podrá presentar una **contra-notificación**.

> La DMCA es normativa de EE.UU. y no vinculante en Argentina; este procedimiento es una buena práctica y puede ser requerido por hosts/CDN. Los reclamos también pueden canalizarse conforme a la ley argentina aplicable.

### 5. Enlaces externos
No controlamos ni respondemos por el contenido de sitios de terceros enlazados.

### 6. Atribución de datos deportivos
Los datos de partidos, resultados y estadísticas se obtienen de proveedores de terceros licenciados. Atribución: "Datos deportivos por API-Football". No representamos dichos datos como oficiales.

### 7. Ley aplicable
República Argentina. Jurisdicción: tribunales de la Ciudad Autónoma de Buenos Aires (CABA).
""";

    private const string LegalNoticeEn = """
### 1. Ownership
azulyoro.com.ar is operated by Xenova. Contact: legal@azulyoro.com.ar.

### 2. Unofficial nature
An **UNOFFICIAL** fan site, unrelated to Club Atlético Boca Juniors or its entities. Third-party marks, crests, names and images belong to their owners and are used for informational and referential purposes. See Terms §2.

### 3. Third-party intellectual property and the right of quotation
Journalistic content quoted or linked is used in accordance with **Law 11.723** (right of quotation, art. 10; general-interest news with source credit, art. 28). We do not reproduce full bodies of third-party articles; we publish original write-ups with attribution and a link to the original source.

### 4. Notice-and-takedown
If you are a rights holder and believe published content infringes your rights (copyright, trademark, image, personal data), send a notice to legal@azulyoro.com.ar including:
- Identification of the content and its URL on the Site.
- Identification of the allegedly infringed right and its ownership.
- The claimant's contact details.
- A good-faith statement.

We commit to **review and, where appropriate, disable or remove** the content as promptly as reasonably possible upon a valid notice. The affected user/author may submit a **counter-notice**.

> The DMCA is US law and not binding in Argentina; this procedure is best practice and may be required by hosts/CDN. Claims may also be channeled under applicable Argentine law.

### 5. External links
We do not control or answer for the content of linked third-party sites.

### 6. Sports data attribution
Match data, results and statistics are obtained from licensed third-party providers. Attribution: "Sports data by API-Football". We do not represent such data as official.

### 7. Governing law
Argentine Republic. Jurisdiction: courts of the Autonomous City of Buenos Aires (CABA).
""";

    private const string CookiesEs = """
### 1. Qué son
Las cookies son pequeños archivos que el Sitio almacena en su dispositivo para funcionar, recordar preferencias y medir el uso.

### 2. Categorías que usamos
| Categoría | Finalidad | Consentimiento | Ejemplos |
| **Necesarias** | Sesión de usuario, seguridad (CSRF), preferencia de idioma | No requiere (imprescindibles) | cookie de sesión Identity, XSRF-TOKEN, locale |
| **Analíticas** | Medir tráfico y uso de forma agregada | No requiere (Plausible, sin cookies) | Plausible (sin cookies) |

> Usamos Plausible, que mide el tráfico de forma agregada **sin cookies ni datos personales**. Por eso no se disparan cookies analíticas ni se requiere consentimiento para la analítica.

### 3. Gestión del consentimiento
Al ingresar, un **aviso** informa sobre el uso de cookies. Las cookies no esenciales **no se activan** sin su consentimiento. En la versión actual solo usamos cookies necesarias y analítica sin cookies (Plausible), por lo que el aviso es meramente informativo. Puede gestionar las cookies desde la configuración de su navegador.

### 4. Cookies de terceros
Los embeds sociales (X, Instagram) pueden establecer sus propias cookies, regidas por sus políticas.

### 5. Duración
Las cookies de sesión expiran al cerrar el navegador; las persistentes tienen plazos definidos por cada finalidad.

### 6. Cambios
Actualizaremos esta política ante cambios en las cookies utilizadas.
""";

    private const string CookiesEn = """
### 1. What they are
Cookies are small files the Site stores on your device to function, remember preferences and measure usage.

### 2. Categories we use
| Category | Purpose | Consent | Examples |
| **Necessary** | User session, security (CSRF), language preference | Not required (essential) | Identity session cookie, XSRF-TOKEN, locale |
| **Analytics** | Measure traffic and usage in aggregate | Not required (Plausible, cookieless) | Plausible (cookieless) |

> We use Plausible, which measures traffic in aggregate **without cookies or personal data**. Therefore no analytics cookies fire and no consent is required for analytics.

### 3. Consent management
On arrival, a **notice** informs you about the use of cookies. Non-essential cookies do **not** fire without your consent. In the current version we only use necessary cookies and cookieless analytics (Plausible), so the notice is purely informational. You can manage cookies from your browser settings.

### 4. Third-party cookies
Social embeds (X, Instagram) may set their own cookies, governed by their own policies.

### 5. Duration
Session cookies expire when you close your browser; persistent cookies have durations defined by each purpose.

### 6. Changes
We will update this policy when the cookies used change.
""";
}
