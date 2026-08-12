using Azulyoro.Api.Configuration;
using Azulyoro.Api.Features.Admin;
using Azulyoro.Api.Features.Articles;
using Azulyoro.Api.Features.Auth;
using Azulyoro.Api.Features.Competitions;
using Azulyoro.Api.Features.Legal;
using Azulyoro.Api.Features.Matches;
using Azulyoro.Api.Features.Members;
using Azulyoro.Api.Features.Newsletter;
using Azulyoro.Api.Features.Players;
using Azulyoro.Api.Features.Standings;
using Azulyoro.Infrastructure;
using Azulyoro.Infrastructure.Content;
using Azulyoro.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAppIdentity(builder.Configuration);
builder.Services.AddApiHardening(builder.Configuration);
builder.Services.AddAppHangfire(builder.Configuration);

var app = builder.Build();

// The deploy pipeline runs migrations in a short-lived, isolated systemd
// unit before switching the live release. Normal web workers never migrate
// the database during request-serving startup.
if (builder.Configuration.GetValue<bool>("Database:MigrateOnly"))
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    return;
}

app.UseApiHardening();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Seed representative sports data so the API is verifiable without the
    // external API-Football key.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DevDataSeeder.SeedAsync(db, CancellationToken.None);
    await ContentSeeder.SeedSourcesAsync(db, CancellationToken.None);
    await LegalSeeder.SeedLegalAsync(db, CancellationToken.None);
    await IdentitySetup.SeedRolesAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

app.MapMatchesEndpoints();
app.MapPlayersEndpoints();
app.MapStandingsEndpoints();
app.MapCompetitionsEndpoints();
app.MapArticlesEndpoints();
app.MapContentAdminEndpoints();
app.MapAuthEndpoints();
app.MapNewsletterEndpoints();
app.MapLegalEndpoints();
app.MapMembersEndpoints();

app.UseAppHangfire();

// Dev-only endpoints: exercise the sensitive rate-limit policy and let us
// enqueue a sync job through Hangfire to confirm the pipeline executes.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/dev/rate-test", () => Results.Ok())
        .RequireRateLimiting(ApiHardening.SensitivePolicy);

    app.MapPost("/api/dev/run-sync", (Hangfire.IBackgroundJobClient jobs) =>
    {
        jobs.Enqueue<Azulyoro.Api.Features.Admin.SyncJobs>(
            j => j.SyncStaticAsync(CancellationToken.None));
        return Results.Accepted();
    });
}

app.Run();
