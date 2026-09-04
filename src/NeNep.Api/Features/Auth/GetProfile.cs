using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Auth;

/// <summary>Returns the signed-in account: roles, classes, and whether a password change is due.</summary>
public static class GetProfile
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/me", HandleAsync)
            .RequireAuthorization()
            .AllowMustChangePassword()
            .WithName("GetProfile")
            .WithSummary("Thông tin tài khoản đang đăng nhập");

    private static Task<ProfileResponse> HandleAsync(
        NeNepDbContext db,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        Profiles.LoadAsync(db, currentUser.UserId, cancellationToken);
}
