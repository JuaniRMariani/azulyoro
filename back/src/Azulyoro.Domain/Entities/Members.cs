using Azulyoro.Domain.Common;
using Azulyoro.Domain.Enums;

namespace Azulyoro.Domain.Entities;

/// <summary>Newsletter subscriber with double opt-in bookkeeping.</summary>
public class NewsletterSubscriber : Entity
{
    public string Email { get; set; } = string.Empty;
    public NewsletterStatus Status { get; set; } = NewsletterStatus.Pending;
    public string Locale { get; set; } = "es";

    /// <summary>Hash of the single-use signed confirm/unsubscribe token.</summary>
    public string? ConfirmTokenHash { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedIp { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
}

/// <summary>Versioned legal page body, per slug + locale.</summary>
public class LegalPage : Entity
{
    public string Slug { get; set; } = string.Empty; // terms|privacy|legal-notice|cookies
    public string Locale { get; set; } = "es";
    public string Title { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateOnly EffectiveDate { get; set; }
}
