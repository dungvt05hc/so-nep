using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;
using NeNep.Infrastructure.Security;

namespace NeNep.Api.Features.Auth;

/// <summary>
/// Changes the password of the signed-in account. This is the one endpoint that stays
/// open while <c>must_change_password</c> is set, because it is how an account created
/// by a teacher gets out of that state.
/// <para>
/// Every other session is revoked, and a fresh pair of tokens is returned so the caller
/// is not left holding an access token that still says a password change is due.
/// </para>
/// </summary>
public static class ChangePassword
{
    public sealed record Request(string CurrentPassword, string NewPassword);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator(IOptions<AuthOptions> options)
        {
            var minimum = options.Value.MinPasswordLength;

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới.")
                .MinimumLength(minimum).WithMessage($"Mật khẩu mới phải có ít nhất {minimum} ký tự.")
                .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/change-password", HandleAsync)
            .RequireAuthorization()
            .AllowMustChangePassword()
            .WithValidation<Request>()
            .WithName("ChangePassword")
            .WithSummary("Đổi mật khẩu");

    private static async Task<AuthTokensResponse> HandleAsync(
        Request request,
        NeNepDbContext db,
        ICurrentUser currentUser,
        IPasswordService passwords,
        ISessionManager sessions,
        IAuditScope auditScope,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        if (!passwords.Verify(user.PasswordHash, request.CurrentPassword, out _))
        {
            throw new AppValidationException(
                nameof(Request.CurrentPassword),
                "Mật khẩu hiện tại không đúng.");
        }

        user.PasswordHash = passwords.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.FailedAttempts = 0;
        user.LockedUntil = null;

        auditScope.Tag(user, AuditAction.UPDATE, "Người dùng tự đổi mật khẩu.");

        // Anyone holding an older token for this account loses it, including the caller's
        // own token, which is replaced below.
        await sessions.RevokeAllAsync(
            user.Id,
            AuditAction.UPDATE,
            "Đổi mật khẩu — thu hồi các phiên đăng nhập cũ.",
            cancellationToken);

        var tokens = await sessions.IssueAsync(user, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var profile = await Profiles.LoadAsync(db, user.Id, cancellationToken);

        return new AuthTokensResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            profile);
    }
}
