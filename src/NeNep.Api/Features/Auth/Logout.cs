using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Auth;

/// <summary>Signs out by revoking the session behind the current access token.</summary>
public static class Logout
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/logout", HandleAsync)
            .RequireAuthorization()
            .AllowMustChangePassword()
            .WithName("Logout")
            .WithSummary("Đăng xuất");

    private static async Task<IResult> HandleAsync(
        NeNepDbContext db,
        ICurrentUser currentUser,
        ISessionManager sessions,
        CancellationToken cancellationToken)
    {
        await sessions.RevokeAsync(
            currentUser.SessionId,
            AuditAction.UPDATE,
            "Người dùng đăng xuất.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
