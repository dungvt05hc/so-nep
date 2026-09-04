using Microsoft.EntityFrameworkCore;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Security;

/// <summary>
/// Checks, on every request, that the session behind an access token is still alive.
/// <para>
/// This is what makes revocation immediate. A JWT stays valid until it expires, so
/// without this check a teacher whose account was revoked would keep working for up to
/// fifteen minutes. One primary-key lookup per request is a fair price at 255 accounts.
/// </para>
/// </summary>
public interface ISessionValidator
{
    Task<bool> IsAliveAsync(string sessionId, int userId, CancellationToken cancellationToken);
}

public sealed class SessionValidator : ISessionValidator
{
    private readonly NeNepDbContext _db;
    private readonly TimeProvider _timeProvider;

    public SessionValidator(NeNepDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsAliveAsync(string sessionId, int userId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        // The global soft-delete filter on users means a deleted account cannot match.
        return await _db.Sessions
            .Where(s => s.Id == sessionId
                && s.UserId == userId
                && s.RevokedAt == null
                && s.ExpiresAt > now)
            .AnyAsync(s => _db.Users.Any(u => u.Id == s.UserId && u.IsActive), cancellationToken);
    }
}
