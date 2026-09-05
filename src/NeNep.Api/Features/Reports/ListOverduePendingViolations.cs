using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Calendar;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Reports;

/// <summary>
/// MISSING DATA, third question: what is still waiting for review after its week's
/// review window closed?
/// <para>
/// This is the report the plan asks for so the pressure lands in the right place. A
/// pending record past its deadline is discarded by the weekly lock, and a pupil must
/// never lose a commendation — or keep a violation — because nobody looked at it.
/// </para>
/// </summary>
public static class ListOverduePendingViolations
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/reports/overdue-pending", HandleAsync)
            .RequireAuthorization()
            .WithName("ListOverduePendingViolations")
            .WithSummary("Bản ghi chờ duyệt quá hạn");

    private static async Task<IReadOnlyList<OverduePendingResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISchoolSettings settings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        var current = await settings.GetAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var pending = await db.ViolationRecords
            .AsNoTracking()
            .Where(r => r.ClassId == classId && r.Status == ViolationStatus.PENDING)
            .Select(r => new
            {
                r.Id,
                r.WeekId,
                r.Week.WeekNo,
                r.Week.StartDate,
                r.Week.EndDate,
                r.StudentId,
                StudentCode = r.Student.Code,
                StudentName = r.Student.FullName,
                r.TypeCodeSnapshot,
                r.TypeNameSnapshot,
                r.OccurredDate,
                r.ReportedById,
                ReportedByName = r.ReportedBy.FullName,
                r.ReportedAt,
            })
            .ToListAsync(cancellationToken);

        return pending
            .Select(r => new
            {
                Row = r,
                Deadline = WeekReviewWindow.ReviewDeadline(
                    r.StartDate,
                    r.EndDate,
                    current.LockDayOfWeek,
                    current.LockHour,
                    current.GraceHours),
            })
            .Where(x => now > x.Deadline)
            .OrderBy(x => x.Deadline)
            .ThenBy(x => x.Row.Id)
            .Select(x => new OverduePendingResponse(
                x.Row.Id,
                x.Row.WeekId,
                x.Row.WeekNo,
                x.Row.StudentId,
                x.Row.StudentCode,
                x.Row.StudentName,
                x.Row.TypeCodeSnapshot,
                x.Row.TypeNameSnapshot,
                x.Row.OccurredDate,
                x.Row.ReportedById,
                x.Row.ReportedByName,
                x.Row.ReportedAt,
                x.Deadline,
                (int)(now - x.Deadline).TotalHours))
            .ToList();
    }
}
