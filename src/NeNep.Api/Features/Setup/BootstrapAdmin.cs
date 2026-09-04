using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;
using NeNep.Infrastructure.Security;

namespace NeNep.Api.Features.Setup;

/// <summary>
/// Creates the very first administrator, because there is no sign-up anywhere in this
/// system: every account is handed out by somebody already inside it.
/// <para>
/// It runs only when the database has no accounts at all and the deployment supplies
/// <c>Bootstrap:AdminUsername</c> and <c>Bootstrap:AdminPassword</c>. The account is
/// created with <c>must_change_password</c> set, so the password from the deployment
/// configuration stops working after the first sign-in.
/// </para>
/// </summary>
public static class BootstrapAdmin
{
    public static async Task EnsureBootstrapAdminAsync(this WebApplication app)
    {
        var username = app.Configuration["Bootstrap:AdminUsername"];
        var password = app.Configuration["Bootstrap:AdminPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<NeNepDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BootstrapAdmin));

        if (await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            return;
        }

        db.Users.Add(new User
        {
            Username = Usernames.Normalize(username),
            PasswordHash = passwords.Hash(password),
            FullName = app.Configuration["Bootstrap:AdminFullName"] ?? "Quản trị hệ thống",
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = timeProvider.GetUtcNow(),
            Roles = { new UserRole { Role = Role.ADMIN } },
        });

        await db.SaveChangesAsync();

        logger.LogInformation("Created the bootstrap administrator account {Username}.", username);
    }
}
