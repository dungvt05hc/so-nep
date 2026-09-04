using Microsoft.EntityFrameworkCore;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>
/// The class register: the students enrolled in one class for its academic year.
/// Every caller passes the guard first, so a homeroom teacher cannot read another class
/// by changing the number in the URL.
/// </summary>
public static class ListStudents
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/students", HandleAsync)
            .RequireAuthorization()
            .WithName("ListStudents")
            .WithSummary("Danh sách học sinh của lớp");

    private static async Task<IReadOnlyList<StudentResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        return await db.Enrollments
            .AsNoTracking()
            .Where(e => e.ClassId == classId && (includeInactive || e.IsActive))
            .OrderBy(e => e.OrderNo == null)
            .ThenBy(e => e.OrderNo)
            .ThenBy(e => e.Student.FullName)
            .Select(e => new StudentResponse(
                e.StudentId,
                e.Student.Code,
                e.Student.FullName,
                e.Student.Dob,
                e.Student.Gender,
                e.Student.Note,
                e.Id,
                e.ClassId,
                e.Class.Code,
                e.OrderNo,
                e.IsActive,
                e.LeftAt,
                e.Student.Account != null))
            .ToListAsync(cancellationToken);
    }
}
