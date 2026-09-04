using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Auth;

/// <summary>
/// Exchanges a refresh token for a new pair of tokens.
/// <para>
/// The old session is revoked and a new one opened on every refresh. If a refresh token
/// is presented whose secret does not match the stored hash, the token has been copied,
/// so EVERY session of that account is revoked at once.
/// </para>
/// </summary>
public static class RefreshSession
{
    public sealed record Request(string RefreshToken);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Thiếu refresh token.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/refresh", HandleAsync)
            .AllowAnonymous()
            .WithValidation<Request>()
            .WithName("RefreshSession")
            .WithSummary("Làm mới phiên đăng nhập");

    private static async Task<AuthTokensResponse> HandleAsync(
        Request request,
        NeNepDbContext db,
        ITokenService tokens,
        ISessionManager sessions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var invalid = new UnauthorizedException(
            "Phiên đăng nhập đã hết hiệu lực. Vui lòng đăng nhập lại.",
            "INVALID_REFRESH_TOKEN");

        if (!tokens.TryRead(request.RefreshToken, out var sessionId, out var secret))
        {
            throw invalid;
        }

        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw invalid;
        }

        if (!sessions.Matches(session, secret))
        {
            await sessions.RevokeAllAsync(
                session.UserId,
                AuditAction.REVOKE_ACCOUNT,
                "Refresh token không khớp — thu hồi toàn bộ phiên đăng nhập.",
                cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            throw invalid;
        }

        var now = timeProvider.GetUtcNow();

        if (session.RevokedAt is not null || session.ExpiresAt <= now)
        {
            throw invalid;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw invalid;
        }

        await sessions.RevokeAsync(
            session.Id,
            AuditAction.LOGIN,
            "Xoay vòng phiên đăng nhập.",
            cancellationToken);

        var issued = await sessions.IssueAsync(user, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var profile = await Profiles.LoadAsync(db, user.Id, cancellationToken);

        return new AuthTokensResponse(
            issued.AccessToken,
            issued.AccessTokenExpiresAt,
            issued.RefreshToken,
            issued.RefreshTokenExpiresAt,
            profile);
    }
}
