using Microsoft.EntityFrameworkCore;
using NeNep.Api.Features.AcademicYears;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Weeks;

/// <summary>Lists the academic weeks of a year, optionally narrowed to one term.</summary>
public static class ListWeeks
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder yearGroup) =>
        yearGroup.MapGet("/{yearId:int}/weeks", HandleAsync)
            .RequireAuthorization()
            .WithName("ListWeeks")
            .WithSummary("Danh sách tuần học");

    private static async Task<IReadOnlyList<WeekResponse>> HandleAsync(
        int yearId,
        NeNepDbContext db,
        CancellationToken cancellationToken,
        int? termId = null) =>
        await db.AcademicWeeks
            .AsNoTracking()
            .Where(w => w.YearId == yearId && (termId == null || w.TermId == termId))
            .OrderBy(w => w.WeekNo)
            .Select(w => new WeekResponse(
                w.Id,
                w.YearId,
                w.TermId,
                w.Term.Code,
                w.WeekNo,
                w.StartDate,
                w.EndDate,
                w.Label,
                w.IsCounted,
                w.NotCountedReason,
                w.OriginalStartDate,
                w.Status,
                w.LockedAt))
            .ToListAsync(cancellationToken);
}
