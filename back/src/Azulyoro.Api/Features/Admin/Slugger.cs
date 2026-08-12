using System.Globalization;
using System.Text;

namespace Azulyoro.Api.Features.Admin;

/// <summary>Minimal, dependency-free slug generator (ASCII-fold + kebab-case).</summary>
public static class Slugger
{
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (char.IsWhiteSpace(c) || c is '-' or '_' or '/')
                sb.Append('-');
            // else: drop punctuation
        }

        var slug = sb.ToString().Normalize(NormalizationForm.FormC);

        // Collapse repeated dashes and trim.
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        slug = slug.Trim('-');

        return slug.Length > 200 ? slug[..200].Trim('-') : slug;
    }
}
