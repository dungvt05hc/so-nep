using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Accounts;

/// <summary>
/// Withdraws a class-officer account for good, at the end of a term of office or when the
/// student leaves.
/// <para>
/// The row is soft deleted, never removed: the records the student entered still point at
/// it, and the audit trail has to stay readable. Every session is revoked in the same
/// transaction, which is what makes the withdrawal immediate — the next request with an
/// access token that is still within its fifteen minutes is refused.
/// </para>
/// </summary>
public static class RevokeAccount
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapDelete("/{classId:int}/accounts/{userId:int}", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("RevokeAccount")
            .WithSummary("Thu hồi tài khoản cán bộ lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        int userId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISessionManager sessions,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var user = await AccountQueries.LoadOfficerAccountAsync(db, classId, userId, cancellationToken);

        user.IsActive = false;
        user.DeletedAt = timeProvider.GetUtcNow();

        auditScope.Tag(user, AuditAction.REVOKE_ACCOUNT, $"Thu hồi tài khoản {user.Username}.");

        await sessions.RevokeAllAsync(
            user.Id,
            AuditAction.REVOKE_ACCOUNT,
            "Thu hồi tài khoản — thu hồi phiên đăng nhập.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
