using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Reports;

/// <summary>
/// MISSING DATA, second question: which pupils has nobody written anything about — in
/// this week, or in the whole term?
/// <para>
/// Exactly one of <c>weekId</c> or <c>termId</c> is expected; the two answer different
/// questions and mixing them silently would give a number nobody can interpret.
/// </para>
/// </summary>
public static class ListStudentsWithoutRecords
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/reports/students-without-records", HandleAsync)
            .RequireAuthorization()
            .WithName("ListStudentsWithoutRecords")
            .WithSummary("Học sinh chưa có bản ghi nào");

    private static async Task<IReadOnlyList<StudentWithoutRecordsResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken,
        int? weekId = null,
        int? termId = null)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        if ((weekId is null) == (termId is null))
        {
            throw new AppValidationException(
                "weekId",
                "Vui lòng chọn một tuần hoặc một học kỳ.");
        }

        var recorded = db.ViolationRecords
            .Where(r => r.ClassId == classId
                && (weekId == null || r.WeekId == weekId)
                && (termId == null || r.Week.TermId == termId))
            .Select(r => r.StudentId);

        return await db.Enrollments
            .AsNoTracking()
            .Where(e => e.ClassId == classId && e.IsActive && !recorded.Contains(e.StudentId))
            .OrderBy(e => e.OrderNo == null)
            .ThenBy(e => e.OrderNo)
            .ThenBy(e => e.Student.FullName)
            .Select(e => new StudentWithoutRecordsResponse(
                e.StudentId,
                e.Student.Code,
                e.Student.FullName,
                e.OrderNo))
            .ToListAsync(cancellationToken);
    }
}
