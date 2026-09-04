using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Calendar;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.SchoolBreaks;

/// <summary>
/// Pushes the academic calendar back because of a school break.
/// <para>
/// Every week that starts on or after the break moves forward by the same number of
/// weeks and KEEPS ITS WEEK NUMBER, because the school refers to "tuần 12" all year and
/// the term score divides by the number of weeks locked, not by dates.
/// </para>
/// <para>
/// A locked or published week is never touched. If one of them sits on or after the
/// break, the request is refused rather than silently skipping it, since skipping would
/// leave two weeks claiming the same dates.
/// </para>
/// <para>
/// How many weeks a break costs is a judgement the school makes: two weeks of Tết is
/// obvious, three days lost to a storm is not. The suggestion counts whole
/// Monday-to-Sunday blocks inside the break, and the administrator can override it.
/// Send <c>preview: true</c> to see the effect without writing anything.
/// </para>
/// </summary>
public static class ApplySchoolBreak
{
    public sealed record Request(int? ShiftWeeks, bool Preview);

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/{breakId:int}/apply", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("ApplySchoolBreak")
            .WithSummary("Áp dụng kỳ nghỉ và dời lịch tuần học");

    private static async Task<ApplySchoolBreakResponse> HandleAsync(
        int breakId,
        Request request,
        NeNepDbContext db,
        ICurrentUser currentUser,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var schoolBreak = await db.SchoolBreaks.FirstOrDefaultAsync(b => b.Id == breakId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy kỳ nghỉ.");

        if (schoolBreak.AppliedAt is not null)
        {
            throw new ConflictException("Kỳ nghỉ này đã được áp dụng.", "BREAK_ALREADY_APPLIED");
        }

        if (!schoolBreak.IsConfirmed)
        {
            throw new ConflictException(
                "Kỳ nghỉ chưa được xác nhận chính thức nên chưa thể dời lịch.",
                "BREAK_NOT_CONFIRMED");
        }

        var suggested = AcademicCalendar.SuggestShiftWeeks(schoolBreak.StartDate, schoolBreak.EndDate);
        var shift = request.ShiftWeeks ?? suggested;

        if (shift < 1)
        {
            throw new AppValidationException(
                nameof(Request.ShiftWeeks),
                "Kỳ nghỉ không chiếm trọn tuần học nào. Nếu vẫn cần dời lịch, hãy nhập số tuần muốn dời.");
        }

        var weeks = await db.AcademicWeeks
            .Where(w => w.YearId == schoolBreak.YearId && w.StartDate >= schoolBreak.StartDate)
            .OrderBy(w => w.WeekNo)
            .ToListAsync(cancellationToken);

        var locked = weeks
            .Where(w => w.Status is WeekStatus.LOCKED or WeekStatus.PUBLISHED)
            .Select(w => w.WeekNo)
            .ToList();

        if (locked.Count > 0)
        {
            throw new ConflictException(
                $"Không thể dời lịch: các tuần đã chốt nằm sau ngày bắt đầu kỳ nghỉ ({string.Join(", ", locked)}).",
                "BREAK_HITS_LOCKED_WEEKS");
        }

        var days = shift * AcademicCalendar.DaysPerWeek;

        var moved = weeks
            .Select(w => new ShiftedWeekResponse(
                w.Id,
                w.WeekNo,
                w.StartDate,
                w.StartDate.AddDays(days),
                w.EndDate.AddDays(days)))
            .ToList();

        if (request.Preview)
        {
            return new ApplySchoolBreakResponse(schoolBreak.Id, false, shift, suggested, moved);
        }

        foreach (var week in weeks)
        {
            // Kept only the first time a week moves, so it still points at the date the
            // yearly plan originally announced.
            week.OriginalStartDate ??= week.StartDate;
            week.StartDate = week.StartDate.AddDays(days);
            week.EndDate = week.EndDate.AddDays(days);

            auditScope.Tag(
                week,
                AuditAction.UPDATE,
                $"Dời {shift} tuần do kỳ nghỉ \"{schoolBreak.Name}\"; giữ nguyên tuần {week.WeekNo}.");
        }

        schoolBreak.ShiftedWeeks = shift;
        schoolBreak.AppliedAt = timeProvider.GetUtcNow();
        schoolBreak.AppliedById = currentUser.UserId;

        auditScope.Tag(
            schoolBreak,
            AuditAction.UPDATE,
            $"Áp dụng kỳ nghỉ \"{schoolBreak.Name}\", dời {moved.Count} tuần học.");

        await db.SaveChangesAsync(cancellationToken);

        return new ApplySchoolBreakResponse(schoolBreak.Id, true, shift, suggested, moved);
    }
}
