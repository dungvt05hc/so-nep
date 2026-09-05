using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Reports;

/// <summary>
/// MISSING DATA, first question: which days of the week did this class write nothing on?
/// <para>
/// Every status counts here, PENDING included: the question is whether anyone entered
/// anything, not whether it was approved.
/// </para>
/// </summary>
public static class ListDaysWithoutRecords
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/reports/days-without-records", HandleAsync)
            .RequireAuthorization()
            .WithName("ListDaysWithoutRecords")
            .WithSummary("Ngày lớp chưa nhập dữ liệu");

    private static async Task<MissingDaysResponse> HandleAsync(
        int classId,
        int weekId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        var week = await db.AcademicWeeks
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == weekId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tuần học.");

        var recordedDates = await db.ViolationRecords
            .AsNoTracking()
            .Where(r => r.ClassId == classId && r.WeekId == weekId)
            .Select(r => r.OccurredDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        // A confirmed break is a day the school was closed, so it is not missing data.
        var breaks = await db.SchoolBreaks
            .AsNoTracking()
            .Where(b => b.YearId == week.YearId
                && b.IsConfirmed
                && b.StartDate <= week.EndDate
                && b.EndDate >= week.StartDate)
            .Select(b => new { b.StartDate, b.EndDate })
            .ToListAsync(cancellationToken);

        var recorded = recordedDates.ToHashSet();
        var missing = new List<DayWithoutRecordsResponse>();
        var breakDates = new List<DateOnly>();

        for (var date = week.StartDate; date <= week.EndDate; date = date.AddDays(1))
        {
            if (breaks.Any(b => b.StartDate <= date && b.EndDate >= date))
            {
                breakDates.Add(date);

                continue;
            }

            if (recorded.Contains(date))
            {
                continue;
            }

            var dayOfWeek = date.DayOfWeek;

            missing.Add(new DayWithoutRecordsResponse(
                date,
                dayOfWeek,
                dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday));
        }

        return new MissingDaysResponse(
            week.Id,
            week.WeekNo,
            week.StartDate,
            week.EndDate,
            missing,
            breakDates);
    }
}
