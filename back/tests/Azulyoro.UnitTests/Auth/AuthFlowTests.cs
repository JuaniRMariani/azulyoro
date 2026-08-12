using Azulyoro.Infrastructure.Identity;
using Azulyoro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Azulyoro.UnitTests.Auth;

/// <summary>
/// Focused Identity-flow test using an InMemory store (no Postgres, no host):
/// register a user, assign the Member role, generate + consume an email
/// confirmation token, and assert the resulting state.
/// </summary>
public class AuthFlowTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection(); // required by the default token providers

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"auth-{Guid.CreateVersion7()}"));

        services
            .AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Register_confirm_email_and_assign_member_role()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AppRoles.All)
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }

        var users = sp.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            UserName = "member@azulyoro.com.ar",
            Email = "member@azulyoro.com.ar",
            DisplayName = "Test Member",
            LocalePref = "es",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        var created = await users.CreateAsync(user, "Sup3rSecret!");
        Assert.True(created.Succeeded);

        var roleResult = await users.AddToRoleAsync(user, AppRoles.Member);
        Assert.True(roleResult.Succeeded);

        // Not confirmed yet.
        var reloaded = await users.FindByEmailAsync(user.Email!);
        Assert.NotNull(reloaded);
        Assert.False(reloaded!.EmailConfirmed);

        var token = await users.GenerateEmailConfirmationTokenAsync(reloaded);
        var confirm = await users.ConfirmEmailAsync(reloaded, token);
        Assert.True(confirm.Succeeded);

        var confirmed = await users.FindByEmailAsync(user.Email!);
        Assert.NotNull(confirmed);
        Assert.True(confirmed!.EmailConfirmed);
        Assert.Contains(AppRoles.Member, await users.GetRolesAsync(confirmed));
    }

    [Fact]
    public async Task ConfirmEmail_with_bad_token_fails()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            UserName = "bad@azulyoro.com.ar",
            Email = "bad@azulyoro.com.ar",
            CreatedAt = DateTime.UtcNow,
        };
        Assert.True((await users.CreateAsync(user, "Sup3rSecret!")).Succeeded);

        var result = await users.ConfirmEmailAsync(user, "not-a-valid-token");
        Assert.False(result.Succeeded);
    }
}
