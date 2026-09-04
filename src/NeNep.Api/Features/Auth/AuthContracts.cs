using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Auth;

/// <summary>One role held by the account, with the class it applies to when it has one.</summary>
public sealed record RoleAssignmentResponse(Role Role, int? ClassId, string? ClassCode);

/// <summary>Everything the front end needs to draw the right screens for this account.</summary>
public sealed record ProfileResponse(
    int Id,
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    bool MustChangePassword,
    int? StudentId,
    IReadOnlyCollection<RoleAssignmentResponse> Roles,
    IReadOnlyCollection<int> HomeroomClassIds);

public sealed record AuthTokensResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    ProfileResponse User);

public static class Profiles
{
    /// <summary>Reads the profile of one account, roles and homeroom classes included.</summary>
    public static async Task<ProfileResponse> LoadAsync(
        NeNepDbContext db,
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.Phone,
                u.MustChangePassword,
                u.StudentId,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Common.NotFoundException("Không tìm thấy tài khoản.");

        var roles = await db.UserRoles
            .Where(r => r.UserId == userId)
            .Select(r => new RoleAssignmentResponse(
                r.Role,
                r.ClassId,
                db.Classes.Where(c => c.Id == r.ClassId).Select(c => c.Code).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var homeroom = await db.Classes
            .Where(c => c.HomeroomTeacherId == userId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        return new ProfileResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Phone,
            user.MustChangePassword,
            user.StudentId,
            roles,
            homeroom);
    }
}
