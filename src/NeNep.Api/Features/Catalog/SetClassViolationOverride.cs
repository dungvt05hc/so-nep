using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Catalog;

/// <summary>
/// DECISION Q7: the homeroom teacher tailors the school catalog for their own class.
/// <para>
/// Switching a code off is always allowed. Changing its point value is allowed ONLY
/// while <see cref="SchoolSetting.AllowClassPointOverride"/> is true — it is false today,
/// and that is what keeps the figures of the 62 classes comparable. Sending a
/// <c>pointsOverride</c> while the flag is off is refused rather than quietly ignored,
/// so a teacher never believes an adjustment took effect when it did not.
/// </para>
/// </summary>
public static class SetClassViolationOverride
{
    public sealed record Request(bool IsEnabled, int? PointsOverride, string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPut("/{classId:int}/violation-types/{typeId:int}", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("SetClassViolationOverride")
            .WithSummary("Bật/tắt mã lỗi cho lớp");

    private static async Task<ClassViolationTypeResponse> HandleAsync(
        int classId,
        int typeId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISchoolSettings settings,
        IAuditScope audit,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var type = await db.ViolationTypes
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == typeId && (t.ClassId == null || t.ClassId == classId), cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy mã lỗi.");

        var current = await settings.GetAsync(cancellationToken);

        if (request.PointsOverride is not null && !current.AllowClassPointOverride)
        {
            throw new ForbiddenException(
                "Nhà trường không cho phép lớp tự sửa mức điểm. GVCN chỉ được bật hoặc tắt mã lỗi.",
                "CLASS_POINT_OVERRIDE_DISABLED");
        }

        var classOverride = await db.ClassViolationOverrides
            .FirstOrDefaultAsync(o => o.ClassId == classId && o.TypeId == typeId, cancellationToken);

        if (classOverride is null)
        {
            classOverride = new ClassViolationOverride
            {
                ClassId = classId,
                TypeId = typeId,
                CreatedById = currentUser.UserId,
            };

            db.ClassViolationOverrides.Add(classOverride);
        }

        classOverride.IsEnabled = request.IsEnabled;
        classOverride.PointsOverride = request.PointsOverride;
        classOverride.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        audit.Tag(
            classOverride,
            AuditAction.OVERRIDE_CATALOG,
            request.IsEnabled
                ? $"Bật mã lỗi {type.Code} cho lớp"
                : $"Tắt mã lỗi {type.Code} cho lớp");

        await db.SaveChangesAsync(cancellationToken);

        return new ClassViolationTypeResponse(
            type.Id,
            type.Code,
            type.Category.Kind,
            type.Category.Name,
            type.Name,
            type.Points,
            ClassCatalog.EffectivePoints(type, classOverride, current.AllowClassPointOverride),
            type.CountsForScore,
            type.AllowedRoles,
            type.IsAutoComputed,
            type.MaxPerWeek,
            type.RequiresRemediation,
            type.RemediationNote,
            type.RemediationDays,
            type.Ordinal,
            classOverride.IsEnabled,
            classOverride.PointsOverride,
            classOverride.Note,
            type.Note);
    }
}
