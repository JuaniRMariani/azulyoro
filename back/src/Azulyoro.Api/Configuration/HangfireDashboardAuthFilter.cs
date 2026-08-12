using Hangfire.Dashboard;

namespace Azulyoro.Api.Configuration;

/// <summary>
/// Guards the Hangfire dashboard. It is NEVER public: in Development it is
/// reachable from localhost only; in Production it requires an authenticated
/// user in the "Admin" role (wired once ASP.NET Identity lands in Phase 4).
/// </summary>
public class HangfireDashboardAuthFilter(IWebHostEnvironment environment)
    : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (environment.IsDevelopment())
        {
            return httpContext.Connection.RemoteIpAddress is { } ip &&
                   System.Net.IPAddress.IsLoopback(ip);
        }

        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
