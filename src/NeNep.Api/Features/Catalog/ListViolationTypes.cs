using Microsoft.EntityFrameworkCore;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Catalog;

/// <summary>
/// The school-wide catalog, as the administrator maintains it. A class officer entering
/// a record uses <see cref="ListClassViolationTypes"/> instead, because that one already
/// takes the homeroom teacher's adjustments into account.
/// </summary>
public static class ListViolationTypes
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder catalog) =>
        catalog.MapGet("/violation-types", HandleAsync)
            .RequireAuthorization()
            .WithName("ListViolationTypes")
            .WithSummary("Danh mục mã lỗi toàn trường");

    private static async Task<IReadOnlyList<ViolationTypeResponse>> HandleAsync(
        NeNepDbContext db,
        CancellationToken cancellationToken,
        bool includeInactive = false) =>
        await db.ViolationTypes
            .AsNoTracking()
            .Where(t => t.ClassId == null && (includeInactive || t.IsActive))
            .OrderBy(t => t.Ordinal)
            .Select(t => new ViolationTypeResponse(
                t.Id,
                t.Code,
                t.Category.Kind,
                t.Category.Name,
                t.Name,
                t.Points,
                t.CountsForScore,
                t.AllowedRoles,
                t.IsAutoComputed,
                t.MaxPerWeek,
                t.RequiresRemediation,
                t.RemediationNote,
                t.RemediationDays,
                t.Ordinal,
                t.IsActive,
                t.Note))
            .ToListAsync(cancellationToken);
}
