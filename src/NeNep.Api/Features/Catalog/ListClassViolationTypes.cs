using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Catalog;

/// <summary>
/// The catalog AS THIS CLASS USES IT — what the quick-entry screen renders as chips.
/// Codes the homeroom teacher switched off are returned too, marked
/// <c>isEnabled: false</c>, so the catalog screen can show the switch.
/// </summary>
public static class ListClassViolationTypes
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapGet("/{classId:int}/violation-types", HandleAsync)
            .RequireAuthorization()
            .WithName("ListClassViolationTypes")
            .WithSummary("Danh mục mã lỗi áp dụng cho lớp");

    private static async Task<IReadOnlyList<ClassViolationTypeResponse>> HandleAsync(
        int classId,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISchoolSettings settings,
        CancellationToken cancellationToken,
        bool includeDisabled = true)
    {
        await guard.EnsureCanReadAsync(classId, cancellationToken);

        var allowPointOverride = (await settings.GetAsync(cancellationToken)).AllowClassPointOverride;

        var rows = await db.ViolationTypes
            .AsNoTracking()
            .Where(t => t.IsActive && (t.ClassId == null || t.ClassId == classId))
            .OrderBy(t => t.Ordinal)
            .Select(t => new
            {
                Type = t,
                t.Category.Kind,
                CategoryName = t.Category.Name,
                Override = t.Overrides.FirstOrDefault(o => o.ClassId == classId),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new
            {
                r.Type,
                r.Kind,
                r.CategoryName,
                r.Override,
                IsEnabled = r.Override?.IsEnabled ?? true,
            })
            .Where(r => includeDisabled || r.IsEnabled)
            .Select(r => new ClassViolationTypeResponse(
                r.Type.Id,
                r.Type.Code,
                r.Kind,
                r.CategoryName,
                r.Type.Name,
                r.Type.Points,
                ClassCatalog.EffectivePoints(r.Type, r.Override, allowPointOverride),
                r.Type.CountsForScore,
                r.Type.AllowedRoles,
                r.Type.IsAutoComputed,
                r.Type.MaxPerWeek,
                r.Type.RequiresRemediation,
                r.Type.RemediationNote,
                r.Type.RemediationDays,
                r.Type.Ordinal,
                r.IsEnabled,
                r.Override?.PointsOverride,
                r.Override?.Note,
                r.Type.Note))
            .ToList();
    }
}
