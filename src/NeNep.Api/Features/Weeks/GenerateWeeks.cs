using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Calendar;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Weeks;

/// <summary>
/// Lays out the academic weeks of a year from the first day of school: Monday to Sunday
/// blocks, numbered the way the school numbers them.
/// <para>
/// The call ADDS missing weeks and moves weeks that are still open. It never deletes a
/// week and never touches one that has been locked or published, so running it twice, or
/// running it again after the plan changes, is safe.
/// </para>
/// </summary>
public static class GenerateWeeks
{
    /// <summary>
    /// Upper bound for a single request. A sanity limit on the input, not a rule of the
    /// school regulation: how many weeks a year has comes from the yearly plan.
    /// </summary>
    private const int MaxWeeks = 60;

    public sealed record Request(DateOnly FirstSchoolDay, int WeekCount, int FirstWeekNo = 1);

    public sealed record Response(
        int Created,
        int Moved,
        int SkippedLocked,
        IReadOnlyCollection<int> SkippedWeekNos);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.WeekCount)
                .InclusiveBetween(1, MaxWeeks)
                .WithMessage($"Số tuần phải nằm trong khoảng 1 đến {MaxWeeks}.");

            RuleFor(x => x.FirstWeekNo)
                .GreaterThan(0)
                .WithMessage("Số hiệu tuần đầu tiên phải lớn hơn 0.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder yearGroup) =>
        yearGroup.MapPost("/{yearId:int}/weeks/generate", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("GenerateWeeks")
            .WithSummary("Sinh tuần học từ ngày khai giảng");

    private static async Task<Response> HandleAsync(
        int yearId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.AcademicYears.AnyAsync(y => y.Id == yearId, cancellationToken))
        {
            throw new NotFoundException("Không tìm thấy năm học.");
        }

        var terms = await db.Terms
            .Where(t => t.YearId == yearId)
            .OrderBy(t => t.Ordinal)
            .ToListAsync(cancellationToken);

        if (terms.Count == 0)
        {
            throw new ConflictException(
                "Năm học chưa có học kỳ nào. Hãy tạo học kỳ trước khi sinh tuần.",
                "YEAR_HAS_NO_TERMS");
        }

        var plan = AcademicCalendar.PlanWeeks(request.FirstSchoolDay, request.WeekCount, request.FirstWeekNo);

        var existing = await db.AcademicWeeks
            .Where(w => w.YearId == yearId)
            .ToDictionaryAsync(w => w.WeekNo, cancellationToken);

        var created = 0;
        var moved = 0;
        var skipped = new List<int>();

        foreach (var planned in plan)
        {
            var term = ResolveTerm(terms, planned)
                ?? throw new AppValidationException(
                    nameof(Request.FirstSchoolDay),
                    $"Tuần {planned.WeekNo} ({planned.StartDate:dd/MM/yyyy}) không nằm trong học kỳ nào. "
                    + "Hãy kiểm tra lại ngày khai giảng, số tuần và khoảng thời gian các học kỳ.");

            if (!existing.TryGetValue(planned.WeekNo, out var week))
            {
                db.AcademicWeeks.Add(new AcademicWeek
                {
                    YearId = yearId,
                    TermId = term.Id,
                    WeekNo = planned.WeekNo,
                    StartDate = planned.StartDate,
                    EndDate = planned.EndDate,
                    IsCounted = true,
                    Status = WeekStatus.OPEN,
                });

                created++;
                continue;
            }

            if (week.Status is WeekStatus.LOCKED or WeekStatus.PUBLISHED)
            {
                // A locked week carries scores that parents may already have seen.
                skipped.Add(week.WeekNo);
                continue;
            }

            if (week.StartDate == planned.StartDate && week.EndDate == planned.EndDate && week.TermId == term.Id)
            {
                continue;
            }

            week.StartDate = planned.StartDate;
            week.EndDate = planned.EndDate;
            week.TermId = term.Id;

            moved++;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new Response(created, moved, skipped.Count, skipped);
    }

    /// <summary>
    /// Which term a week belongs to is derived from the term dates, so the split between
    /// HK1 and HK2 is never a number written into the code.
    /// </summary>
    private static Term? ResolveTerm(IReadOnlyList<Term> terms, PlannedWeek week) =>
        terms.FirstOrDefault(t => t.StartDate <= week.StartDate && week.StartDate <= t.EndDate)
        ?? terms.FirstOrDefault(t => AcademicCalendar.Overlaps(t.StartDate, t.EndDate, week.StartDate, week.EndDate));
}
