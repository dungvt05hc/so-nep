using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Accounts;

public sealed record AccountResponse(
    int Id,
    string Username,
    string FullName,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? LockedUntil,
    DateTimeOffset? LastLoginAt,
    int? StudentId,
    string? StudentName,
    Role Role,
    int ClassId);

/// <summary>
/// A newly created or reset account. The temporary password is returned exactly once, at
/// this moment; it is stored only as a hash, so it cannot be shown again.
/// </summary>
public sealed record IssuedAccountResponse(AccountResponse Account, string TemporaryPassword);

public static class AccountQueries
{
    /// <summary>
    /// Loads a class-officer account belonging to this class.
    /// <para>
    /// Only officer accounts are reachable here. A homeroom teacher administers the
    /// children of their class, never another teacher's account, so a GVCN account is
    /// reported as not found rather than merely refused.
    /// </para>
    /// </summary>
    public static async Task<User> LoadOfficerAccountAsync(
        NeNepDbContext db,
        int classId,
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        var isOfficerOfClass = await db.UserRoles.AnyAsync(
            r => r.UserId == userId
                && r.ClassId == classId
                && (r.Role == Role.LOP_TRUONG || r.Role == Role.PHO_HOC_TAP || r.Role == Role.PHO_LAO_DONG),
            cancellationToken);

        if (!isOfficerOfClass)
        {
            throw new NotFoundException("Tài khoản này không phải cán bộ của lớp.");
        }

        return user;
    }

    /// <summary>
    /// Projects the officer accounts of one class, or one of them.
    /// <para>
    /// Narrowing to a single account happens HERE and not on the result: a filter applied
    /// after the projection has nothing left to translate into SQL.
    /// </para>
    /// </summary>
    public static IQueryable<AccountResponse> ProjectOfficers(NeNepDbContext db, int classId, int? userId = null) =>
        db.UserRoles
            .Where(r => r.ClassId == classId
                && (userId == null || r.UserId == userId)
                && (r.Role == Role.LOP_TRUONG || r.Role == Role.PHO_HOC_TAP || r.Role == Role.PHO_LAO_DONG))
            .Select(r => new AccountResponse(
                r.User.Id,
                r.User.Username,
                r.User.FullName,
                r.User.IsActive,
                r.User.MustChangePassword,
                r.User.LockedUntil,
                r.User.LastLoginAt,
                r.User.StudentId,
                r.User.Student != null ? r.User.Student.FullName : null,
                r.Role,
                classId));
}
