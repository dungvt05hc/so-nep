using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

/// <summary>
/// Deletes a class created by mistake, softly, and only while nothing hangs off it: no
/// student on the register, no officer appointment, no account, no record.
/// <para>
/// The moment a class has students it becomes the anchor of everything that happens to
/// them for a whole year, and it stays.
/// </para>
/// </summary>
public static class DeleteClass
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapDelete("/{classId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteClass")
            .WithSummary("Xoá lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        NeNepDbContext db,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var entity = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        var blockers = new List<string>();

        if (await db.Enrollments.AnyAsync(e => e.ClassId == classId, cancellationToken))
        {
            blockers.Add("học sinh");
        }

        if (await db.ClassOfficers.AnyAsync(o => o.ClassId == classId, cancellationToken))
        {
            blockers.Add("cán bộ lớp");
        }

        if (await db.UserRoles.AnyAsync(r => r.ClassId == classId, cancellationToken))
        {
            blockers.Add("tài khoản");
        }

        if (await db.ViolationRecords.IgnoreQueryFilters().AnyAsync(v => v.ClassId == classId, cancellationToken))
        {
            blockers.Add("bản ghi vi phạm");
        }

        if (blockers.Count > 0)
        {
            throw new ConflictException(
                $"Lớp đã có {string.Join(", ", blockers)} nên không xoá được.",
                "CLASS_IN_USE");
        }

        entity.DeletedAt = timeProvider.GetUtcNow();

        auditScope.Tag(entity, AuditAction.DELETE, $"Xoá lớp {entity.Code}.");

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
