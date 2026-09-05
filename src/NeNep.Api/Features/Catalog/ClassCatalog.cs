using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Catalog;

/// <summary>One catalog code as it applies to one class, adjustments already applied.</summary>
/// <param name="EffectivePoints">
/// The value a new record snapshots. It is the school value unless
/// <see cref="SchoolSetting.AllowClassPointOverride"/> is on AND the homeroom teacher
/// set a different one.
/// </param>
public sealed record ClassCatalogEntry(
    ViolationType Type,
    ClassViolationOverride? Override,
    bool IsEnabled,
    int EffectivePoints);

/// <summary>
/// DECISION Q7: there is ONE school-wide catalog. A homeroom teacher may switch a code
/// off for their class, and may only change its point value when the school has opened
/// that door — otherwise the numbers of the 62 classes stop being comparable.
/// <para>
/// Both the catalog screen and the record-entry endpoint resolve a code through here, so
/// what the teacher sees and what the score uses can never drift apart.
/// </para>
/// </summary>
public static class ClassCatalog
{
    /// <summary>Points of a code for one class. The single answer to "how much is this worth here".</summary>
    public static int EffectivePoints(
        ViolationType type,
        ClassViolationOverride? classOverride,
        bool allowPointOverride) =>
        allowPointOverride && classOverride?.PointsOverride is { } points ? points : type.Points;

    /// <summary>
    /// Loads one code for one class, refusing what the class may not use. Returns a
    /// TRACKED type entity, because the caller goes on to snapshot from it.
    /// </summary>
    public static async Task<ClassCatalogEntry> ResolveAsync(
        NeNepDbContext db,
        ISchoolSettings settings,
        int classId,
        int typeId,
        CancellationToken cancellationToken)
    {
        var type = await db.ViolationTypes
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == typeId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy mã lỗi.");

        if (type.ClassId is not null && type.ClassId != classId)
        {
            throw new NotFoundException("Không tìm thấy mã lỗi.");
        }

        if (!type.IsActive)
        {
            throw new ConflictException(
                $"Mã lỗi {type.Code} đã ngừng sử dụng.",
                "VIOLATION_TYPE_INACTIVE");
        }

        var classOverride = await db.ClassViolationOverrides
            .FirstOrDefaultAsync(o => o.ClassId == classId && o.TypeId == typeId, cancellationToken);

        var isEnabled = classOverride?.IsEnabled ?? true;

        if (!isEnabled)
        {
            throw new ConflictException(
                $"Lớp không sử dụng mã lỗi {type.Code}.",
                "VIOLATION_TYPE_DISABLED_FOR_CLASS");
        }

        var allowPointOverride = (await settings.GetAsync(cancellationToken)).AllowClassPointOverride;

        return new ClassCatalogEntry(
            type,
            classOverride,
            isEnabled,
            EffectivePoints(type, classOverride, allowPointOverride));
    }
}
