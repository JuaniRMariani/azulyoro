using Azulyoro.Api.Configuration;
using Azulyoro.Domain.Entities;
using Azulyoro.Domain.Enums;
using Azulyoro.Infrastructure.Email;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Azulyoro.Api.Features.Newsletter;

public static class NewsletterEndpoints
{
    /// <summary>Confirm tokens expire 48h after the subscribe request.</summary>
    private static readonly TimeSpan TokenTtl = TimeSpan.FromHours(48);

    public static IEndpointRouteBuilder MapNewsletterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/newsletter");

        group.MapPost("/subscribe", Subscribe)
            .RequireRateLimiting(ApiHardening.SensitivePolicy);
        group.MapGet("/confirm", Confirm);
        group.MapGet("/unsubscribe", Unsubscribe);

        return app;
    }

    private static string FrontendBase(IConfiguration config) =>
        (config["Frontend:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');

    private static string NormalizeLocale(string? locale) =>
        locale?.Trim().ToLowerInvariant() is "en" ? "en" : "es";

    // --- Subscribe --------------------------------------------------------

    public record SubscribeRequest(string Email, string? Locale);

    private static async Task<IResult> Subscribe(
        SubscribeRequest req,
        AppDbContext db,
        IEmailSender email,
        IConfiguration config,
        CancellationToken ct)
    {
        var normalized = req.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
        {
            // Always 200 to avoid enumeration; nothing to do.
            return Results.Ok(new { message = "subscription_received" });
        }

        var locale = NormalizeLocale(req.Locale);
        var subscriber = await db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == normalized, ct);

        // Never re-email an already-confirmed subscriber.
        if (subscriber is { Status: NewsletterStatus.Confirmed })
        {
            return Results.Ok(new { message = "subscription_received" });
        }

        var token = TokenHasher.NewToken();
        var tokenHash = TokenHasher.Hash(token);

        if (subscriber is null)
        {
            subscriber = new NewsletterSubscriber
            {
                Email = normalized,
                Locale = locale,
                Status = NewsletterStatus.Pending,
                ConfirmTokenHash = tokenHash,
            };
            db.NewsletterSubscribers.Add(subscriber);
        }
        else
        {
            // Pending or previously Unsubscribed: re-issue a fresh token and
            // reset created-at so the 48h TTL applies from now.
            subscriber.Status = NewsletterStatus.Pending;
            subscriber.Locale = locale;
            subscriber.ConfirmTokenHash = tokenHash;
            subscriber.CreatedAt = DateTime.UtcNow;
            subscriber.ConfirmedAt = null;
            subscriber.UnsubscribedAt = null;
        }

        await db.SaveChangesAsync(ct);

        var link = $"{FrontendBase(config)}/{locale}/newsletter/confirmar" +
            $"?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(normalized)}";
        var unsubscribeLink = $"{FrontendBase(config)}/{locale}/newsletter/baja" +
            $"?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(normalized)}";

        // List-Unsubscribe header concept: the welcome/confirm email should
        // carry `List-Unsubscribe: <{unsubscribeLink}>` and
        // `List-Unsubscribe-Post: List-Unsubscribe=One-Click` so inbox
        // providers expose one-click unsubscribe.
        await email.SendAsync(
            normalized,
            "Confirma tu suscripcion - Azul y Oro",
            $"<p>Confirma tu suscripcion al newsletter:</p><p><a href=\"{link}\">Confirmar</a></p>" +
            $"<p style=\"font-size:12px;color:#888\">Para darte de baja: <a href=\"{unsubscribeLink}\">cancelar</a></p>",
            ct);

        return Results.Ok(new { message = "subscription_received" });
    }

    // --- Confirm ----------------------------------------------------------

    private static async Task<IResult> Confirm(
        string token,
        string email,
        HttpContext http,
        AppDbContext db,
        CancellationToken ct)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(normalized))
        {
            return Results.BadRequest(new { message = "invalid_token" });
        }

        var subscriber = await db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == normalized, ct);

        if (subscriber is null)
        {
            return Results.BadRequest(new { message = "invalid_token" });
        }

        // Idempotent: already confirmed => success without re-validating token.
        if (subscriber.Status == NewsletterStatus.Confirmed)
        {
            return Results.Ok(new { message = "already_confirmed" });
        }

        if (!TokenHasher.Verify(token, subscriber.ConfirmTokenHash))
        {
            return Results.BadRequest(new { message = "invalid_token" });
        }

        if (DateTime.UtcNow - subscriber.CreatedAt > TokenTtl)
        {
            return Results.BadRequest(new { message = "token_expired" });
        }

        subscriber.Status = NewsletterStatus.Confirmed;
        subscriber.ConfirmedAt = DateTime.UtcNow;
        subscriber.ConfirmedIp = http.Connection.RemoteIpAddress?.ToString();
        // Keep the hash so the emailed one-click unsubscribe link (same token)
        // stays valid after confirmation. The token is single-use for confirm
        // (Status is now Confirmed so this branch won't run again).
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { message = "confirmed" });
    }

    // --- Unsubscribe ------------------------------------------------------

    private static async Task<IResult> Unsubscribe(
        string token,
        string email,
        AppDbContext db,
        CancellationToken ct)
    {
        var normalized = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(normalized))
        {
            return Results.BadRequest(new { message = "invalid_token" });
        }

        var subscriber = await db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email == normalized, ct);

        // Idempotent one-click: treat unknown/already-unsubscribed as success.
        if (subscriber is null || subscriber.Status == NewsletterStatus.Unsubscribed)
        {
            return Results.Ok(new { message = "unsubscribed" });
        }

        if (!TokenHasher.Verify(token, subscriber.ConfirmTokenHash))
        {
            return Results.BadRequest(new { message = "invalid_token" });
        }

        subscriber.Status = NewsletterStatus.Unsubscribed;
        subscriber.UnsubscribedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { message = "unsubscribed" });
    }
}
