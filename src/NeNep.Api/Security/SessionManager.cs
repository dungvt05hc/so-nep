using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Security;

/// <summary>The pair of tokens handed back after a successful sign-in or refresh.</summary>
public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>
/// Creates and revokes rows in <c>sessions</c>. Every path that hands out a token goes
/// through here, so revoking an account can be done in exactly one place and be certain
/// it caught everything.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Opens a session and issues its tokens. The caller is responsible for calling
    /// <c>SaveChangesAsync</c>, so the session and whatever caused it are written in one
    /// transaction and produce one coherent audit trail.
    /// </summary>
    Task<TokenPair> IssueAsync(User user, CancellationToken cancellationToken);

    /// <summary>Revokes one session; unknown or already revoked ids are ignored.</summary>
    Task RevokeAsync(string sessionId, AuditAction action, string? summary, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes every live session of a user. This is what makes account revocation, a
    /// password reset and a password change take effect immediately.
    /// </summary>
    Task RevokeAllAsync(
        int userId,
        AuditAction action,
        string? summary,
        CancellationToken cancellationToken,
        string? exceptSessionId = null);

    /// <summary>Compares a presented refresh secret with the stored hash, in constant time.</summary>
    bool Matches(Session session, string secret);
}

public sealed class SessionManager : ISessionManager
{
    private readonly NeNepDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IAuditScope _auditScope;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public SessionManager(
        NeNepDbContext db,
        ITokenService tokens,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        IOptions<AuthOptions> options)
    {
        _db = db;
        _tokens = tokens;
        _auditScope = auditScope;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<TokenPair> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        // The id is generated here rather than by the database default, because the
        // refresh token has to carry it.
        var sessionId = Guid.NewGuid().ToString();
        var refresh = _tokens.CreateRefreshToken(sessionId);
        var expiresAt = now.AddDays(_options.RefreshTokenDays);

        var session = new Session
        {
            Id = sessionId,
            UserId = user.Id,
            RefreshHash = refresh.Hash,
            ExpiresAt = expiresAt,
            CreatedAt = now,
        };

        _db.Sessions.Add(session);
        _auditScope.Tag(session, AuditAction.LOGIN, $"Mở phiên đăng nhập cho {user.Username}.");

        var roles = await _db.UserRoles
            .Where(r => r.UserId == user.Id)
            .Select(r => r.Role)
            .ToListAsync(cancellationToken);

        var access = _tokens.CreateAccessToken(user, roles, sessionId);

        return new TokenPair(access.Value, access.ExpiresAt, refresh.Value, expiresAt);
    }

    public async Task RevokeAsync(
        string sessionId,
        AuditAction action,
        string? summary,
        CancellationToken cancellationToken)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.RevokedAt == null, cancellationToken);

        if (session is null)
        {
            return;
        }

        Revoke(session, action, summary);
    }

    public async Task RevokeAllAsync(
        int userId,
        AuditAction action,
        string? summary,
        CancellationToken cancellationToken,
        string? exceptSessionId = null)
    {
        // Loaded and updated through the change tracker on purpose: ExecuteUpdate would
        // skip the audit interceptor, and every write has to reach audit_logs.
        var sessions = await _db.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.Id != exceptSessionId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            Revoke(session, action, summary);
        }
    }

    public bool Matches(Session session, string secret) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(session.RefreshHash),
            Encoding.UTF8.GetBytes(_tokens.HashSecret(secret)));

    private void Revoke(Session session, AuditAction action, string? summary)
    {
        session.RevokedAt = _timeProvider.GetUtcNow();

        _auditScope.Tag(session, action, summary);
    }
}
