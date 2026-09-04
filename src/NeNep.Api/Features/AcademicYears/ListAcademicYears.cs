using Microsoft.EntityFrameworkCore;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AcademicYears;

/// <summary>
/// Lists the academic years with their terms. Readable by anyone signed in: the school
/// calendar is not class data, and every screen needs to know which year is current.
/// </summary>
public static class ListAcademicYears
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapGet("/", HandleAsync)
            .RequireAuthorization()
            .WithName("ListAcademicYears")
            .WithSummary("Danh sách năm học");

    private static async Task<IReadOnlyList<AcademicYearResponse>> HandleAsync(
        NeNepDbContext db,
        CancellationToken cancellationToken) =>
        await db.AcademicYears
            .AsNoTracking()
            .OrderByDescending(y => y.StartDate)
            .Select(y => new AcademicYearResponse(
                y.Id,
                y.Name,
                y.StartDate,
                y.EndDate,
                y.IsCurrent,
                y.Terms
                    .OrderBy(t => t.Ordinal)
                    .Select(t => new TermResponse(t.Id, t.YearId, t.Code, t.Name, t.StartDate, t.EndDate, t.Ordinal))
                    .ToList()))
            .ToListAsync(cancellationToken);
}
