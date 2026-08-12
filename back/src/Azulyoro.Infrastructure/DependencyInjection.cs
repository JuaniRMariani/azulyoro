using Azulyoro.Infrastructure.ApiFootball;
using Azulyoro.Infrastructure.Content;
using Azulyoro.Infrastructure.Persistence;
using Azulyoro.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Azulyoro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApiFootballClient(configuration);
        services.AddContentServices(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' is not configured. Set it via user-secrets (dev) or EnvironmentFile (prod).");

        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", AppDbContext.Schema))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<LiveSyncService>();
        services.AddHostedService<LiveSyncBackgroundService>();

        return services;
    }
}
