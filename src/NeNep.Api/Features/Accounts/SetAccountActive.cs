using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Accounts;

/// <summary>
/// Locks or unlocks a class-officer account.
/// <para>
/// Locking revokes the open sessions as well as the ability to sign in again, so the
/// student loses access on their next request instead of when their access token runs
/// out. Unlocking also clears the failed-attempt counter and any temporary lock.
/// </para>
/// </summary>
public static class SetAccountActive
{
    public static void Map(RouteGroupBuilder classGroup)
    {
        classGroup.MapPost("/{classId:int}/accounts/{userId:int}/lock", LockAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("LockAccount")
            .WithSummary("Khoá tài khoản cán bộ lớp");

        classGroup.MapPost("/{classId:int}/accounts/{userId:int}/unlock", UnlockAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("UnlockAccount")
            .WithSummary("Mở khoá tài khoản cán bộ lớp");
    }

    private static async Task<AccountResponse> LockAsync(
        int classId,
        int userId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISessionManager sessions,
        IAuditScope auditScope,
        CancellationToken cancellationToken)
    {
        var user = await LoadAsync(classId, userId, db, guard, cancellationToken);

        user.IsActive = false;

        auditScope.Tag(user, AuditAction.REVOKE_ACCOUNT, $"Khoá tài khoản {user.Username}.");

        await sessions.RevokeAllAsync(
            user.Id,
            AuditAction.REVOKE_ACCOUNT,
            "Khoá tài khoản — thu hồi phiên đăng nhập.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return await ReadAsync(db, classId, user.Id, cancellationToken);
    }

    private static async Task<AccountResponse> UnlockAsync(
        int classId,
        int userId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        IAuditScope auditScope,
        CancellationToken cancellationToken)
    {
        var user = await LoadAsync(classId, userId, db, guard, cancellationToken);

        user.IsActive = true;
        user.FailedAttempts = 0;
        user.LockedUntil = null;

        auditScope.Tag(user, AuditAction.GRANT_ACCOUNT, $"Mở khoá tài khoản {user.Username}.");

        await db.SaveChangesAsync(cancellationToken);

        return await ReadAsync(db, classId, user.Id, cancellationToken);
    }

    private static async Task<Domain.Entities.User> LoadAsync(
        int classId,
        int userId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        return await AccountQueries.LoadOfficerAccountAsync(db, classId, userId, cancellationToken);
    }

    private static Task<AccountResponse> ReadAsync(
        NeNepDbContext db,
        int classId,
        int userId,
        CancellationToken cancellationToken) =>
        AccountQueries.ProjectOfficers(db, classId, userId)
            .AsNoTracking()
            .FirstAsync(cancellationToken);
}
