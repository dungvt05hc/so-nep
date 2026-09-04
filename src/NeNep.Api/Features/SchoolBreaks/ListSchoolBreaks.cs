using Microsoft.EntityFrameworkCore;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.SchoolBreaks;

/// <summary>Lists the school breaks of a year, applied or still provisional.</summary>
public static class ListSchoolBreaks
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder yearGroup) =>
        yearGroup.MapGet("/{yearId:int}/breaks", HandleAsync)
            .RequireAuthorization()
            .WithName("ListSchoolBreaks")
            .WithSummary("Danh sách kỳ nghỉ");

    private static async Task<IReadOnlyList<SchoolBreakResponse>> HandleAsync(
        int yearId,
        NeNepDbContext db,
        CancellationToken cancellationToken) =>
        await db.SchoolBreaks
            .AsNoTracking()
            .Where(b => b.YearId == yearId)
            .OrderBy(b => b.StartDate)
            .Select(b => new SchoolBreakResponse(
                b.Id,
                b.YearId,
                b.Name,
                b.StartDate,
                b.EndDate,
                b.IsConfirmed,
                b.ShiftedWeeks,
                b.AppliedAt,
                b.AppliedById,
                b.Note))
            .ToListAsync(cancellationToken);
}
