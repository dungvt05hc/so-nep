using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// Removes a record from the class register.
/// <para>
/// PRINCIPLE 6: this is a SOFT delete. The row stays, stops counting, and remains
/// readable in the audit trail — when a parent asks in March why a record from October
/// disappeared, the answer has to exist.
/// </para>
/// </summary>
public static class DeleteViolation
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapDelete("/{classId:int}/violations/{id:int}", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("DeleteViolation")
            .WithSummary("Xoá bản ghi");

    private static async Task<IResult> HandleAsync(
        int classId,
        int id,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var record = await db.ViolationRecords
            .Include(r => r.Week)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClassId == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bản ghi.");

        ViolationRules.EnsureWeekIsOpen(record.Week);

        record.DeletedAt = timeProvider.GetUtcNow();
        record.DeletedBy = currentUser.UserId;

        // AuditSaveChangesInterceptor recognises the stamped DeletedAt as a business
        // DELETE, so no audit call is needed here.
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
