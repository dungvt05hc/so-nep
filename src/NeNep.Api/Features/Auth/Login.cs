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
/// Signs in with a username and password.
/// <para>
/// After <c>Auth:MaxFailedAttempts</c> consecutive failures the account is locked for
/// <c>Auth:LockMinutes</c>. The class-officer accounts are handed out on paper to
/// twelve-year-olds, so guessing has to be made slow.
/// </para>
/// </summary>
public static class Login
{
    public sealed record Request(string Username, string Password);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Username).NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập.");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Vui lòng nhập mật khẩu.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/login", HandleAsync)
            .AllowAnonymous()
            .WithValidation<Request>()
            .WithName("Login")
            .WithSummary("Đăng nhập");

    private static async Task<AuthTokensResponse> HandleAsync(
        Request request,
        NeNepDbContext db,
        IPasswordService passwords,
        ISessionManager sessions,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        IOptions<AuthOptions> options,
        CancellationToken cancellationToken)
    {
        var auth = options.Value;
        var now = timeProvider.GetUtcNow();
        var username = Usernames.Normalize(request.Username);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        // Unknown username and wrong password answer the same way, so the response cannot
        // be used to find out which accounts exist.
        if (user is null)
        {
            throw new UnauthorizedException(
                "Tên đăng nhập hoặc mật khẩu không đúng.",
                "INVALID_CREDENTIALS");
        }

        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            var minutes = (int)Math.Ceiling((lockedUntil - now).TotalMinutes);

            throw new AppException(
                System.Net.HttpStatusCode.Locked,
                "ACCOUNT_LOCKED",
                $"Tài khoản đang bị tạm khoá. Vui lòng thử lại sau {minutes} phút.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("Tài khoản đã bị khoá.", "ACCOUNT_DISABLED");
        }

        if (!passwords.Verify(user.PasswordHash, request.Password, out var needsRehash))
        {
            user.FailedAttempts++;

            if (user.FailedAttempts >= auth.MaxFailedAttempts)
            {
                user.LockedUntil = now.AddMinutes(auth.LockMinutes);
                user.FailedAttempts = 0;
            }

            auditScope.Tag(user, AuditAction.LOGIN_FAILED, "Đăng nhập sai mật khẩu.");

            await db.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException(
                "Tên đăng nhập hoặc mật khẩu không đúng.",
                "INVALID_CREDENTIALS");
        }

        if (needsRehash)
        {
            user.PasswordHash = passwords.Hash(request.Password);
        }

        user.FailedAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;

        auditScope.Tag(user, AuditAction.LOGIN, $"Đăng nhập thành công: {user.Username}.");

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
