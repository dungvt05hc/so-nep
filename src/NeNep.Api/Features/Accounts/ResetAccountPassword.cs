using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;
using NeNep.Infrastructure.Security;

namespace NeNep.Api.Features.Accounts;

/// <summary>
/// Gives a class officer a new temporary password, for the common case of a forgotten one.
/// <para>
/// Every session of that account is revoked at the same time, so whoever was using the old
/// password is signed out immediately rather than at the end of the access token's life.
/// </para>
/// </summary>
public static class ResetAccountPassword
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/accounts/{userId:int}/reset-password", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("ResetAccountPassword")
            .WithSummary("Đặt lại mật khẩu tài khoản cán bộ lớp");

    private static async Task<IssuedAccountResponse> HandleAsync(
        int classId,
        int userId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISessionManager sessions,
        IPasswordService passwords,
        IAuditScope auditScope,
        IOptions<AuthOptions> options,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var user = await AccountQueries.LoadOfficerAccountAsync(db, classId, userId, cancellationToken);

        var temporaryPassword = TemporaryPassword.Generate(options.Value.TemporaryPasswordLength);

        user.PasswordHash = passwords.Hash(temporaryPassword);
        user.MustChangePassword = true;
        user.FailedAttempts = 0;
        user.LockedUntil = null;

        auditScope.Tag(user, AuditAction.RESET_PASSWORD, $"Đặt lại mật khẩu tài khoản {user.Username}.");

        await sessions.RevokeAllAsync(
            user.Id,
            AuditAction.RESET_PASSWORD,
            "Đặt lại mật khẩu — thu hồi phiên đăng nhập.",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        var account = await AccountQueries.ProjectOfficers(db, classId, user.Id)
            .AsNoTracking()
            .FirstAsync(cancellationToken);

        return new IssuedAccountResponse(account, temporaryPassword);
    }
}
