using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

public sealed record ClassResponse(
    int Id,
    int GradeId,
    int GradeLevel,
    int YearId,
    string YearName,
    string Code,
    string Name,
    int? HomeroomTeacherId,
    string? HomeroomTeacherName,
    int StudentCount);

public static class ClassQueries
{
    /// <summary>The single projection every class listing uses, so they cannot drift apart.</summary>
    public static IQueryable<ClassResponse> Project(IQueryable<Domain.Entities.Class> query) =>
        query.Select(c => new ClassResponse(
            c.Id,
            c.GradeId,
            c.Grade.Level,
            c.YearId,
            c.Year.Name,
            c.Code,
            c.Name,
            c.HomeroomTeacherId,
            c.HomeroomTeacher != null ? c.HomeroomTeacher.FullName : null,
            c.Enrollments.Count(e => e.IsActive)));

    /// <summary>
    /// Checks that a teacher may be put in charge of a class: the account has to exist,
    /// be active, and actually hold the GVCN role.
    /// </summary>
    public static async Task EnsureIsHomeroomTeacherAsync(
        NeNepDbContext db,
        int teacherId,
        CancellationToken cancellationToken)
    {
        var teacher = await db.Users
            .Where(u => u.Id == teacherId)
            .Select(u => new { u.Id, u.IsActive })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppValidationException("homeroomTeacherId", "Không tìm thấy tài khoản giáo viên.");

        if (!teacher.IsActive)
        {
            throw new AppValidationException("homeroomTeacherId", "Tài khoản giáo viên đang bị khoá.");
        }

        var isTeacher = await db.UserRoles
            .AnyAsync(r => r.UserId == teacherId && r.Role == Role.GVCN, cancellationToken);

        if (!isTeacher)
        {
            throw new AppValidationException("homeroomTeacherId", "Tài khoản này không có vai trò GVCN.");
        }
    }
}
