using Azulyoro.Api.Features.Admin;
using Hangfire;
using Hangfire.PostgreSql;

namespace Azulyoro.Api.Configuration;

public static class HangfireSetup
{
    public static IServiceCollection AddAppHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is required for Hangfire.");

        services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(
                options => options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { SchemaName = "hangfire" }));

        services.AddHangfireServer();
        services.AddScoped<SyncJobs>();

        return services;
    }

    public static WebApplication UseAppHangfire(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthFilter(app.Environment)],
        });

        // Recurring ingestion schedule (static daily, semi every 45 min).
        RecurringJob.AddOrUpdate<SyncJobs>(
            SyncJobs.StaticJobId, job => job.SyncStaticAsync(CancellationToken.None), Cron.Daily);
        RecurringJob.AddOrUpdate<SyncJobs>(
            SyncJobs.SemiJobId, job => job.SyncSemiAsync(CancellationToken.None), "*/45 * * * *");

        return app;
    }
}
