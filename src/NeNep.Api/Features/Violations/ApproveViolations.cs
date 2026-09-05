using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The homeroom teacher approving what the class officers entered — one record, or the
/// whole selection from the review queue.
/// <para>
/// PRINCIPLE 5: this is the moment a record starts counting. Nothing else in the system
/// turns a PENDING record into points.
/// </para>
/// </summary>
public static class ApproveViolations
{
    public sealed record BulkRequest(IReadOnlyList<int> Ids);

    public sealed class BulkValidator : AbstractValidator<BulkRequest>
    {
        public BulkValidator()
        {
            RuleFor(x => x.Ids)
                .NotEmpty()
                .WithMessage("Vui lòng chọn ít nhất một bản ghi.");

            RuleFor(x => x.Ids)
                .Must(ids => ids.Count <= 200)
                .WithMessage("Mỗi lần chỉ duyệt tối đa 200 bản ghi.");
        }
    }

    public static RouteHandlerBuilder MapSingle(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/violations/{id:int}/approve", ApproveOneAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithName("ApproveViolation")
            .WithSummary("Duyệt bản ghi");

    public static RouteHandlerBuilder MapBulk(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/violations/approve", ApproveManyAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<BulkRequest>()
            .WithName("ApproveViolations")
            .WithSummary("Duyệt hàng loạt");

    private static async Task<ViolationResponse> ApproveOneAsync(
        int classId,
        int id,
        NeNepDbContext db,
        IClassAccessGuard guard,
        IAuditScope audit,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var record = await db.ViolationRecords
            .Include(r => r.Week)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClassId == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bản ghi.");

        Approve(record, currentUser.UserId, timeProvider.GetUtcNow(), audit);

        await db.SaveChangesAsync(cancellationToken);

        return await ViolationProjection.LoadAsync(db, id, cancellationToken);
    }

    private static async Task<BulkResult> ApproveManyAsync(
        int classId,
        BulkRequest request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        IAuditScope audit,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var ids = request.Ids.Distinct().ToList();

        var records = await db.ViolationRecords
            .Include(r => r.Week)
            .Where(r => r.ClassId == classId && ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var approved = new List<int>();
        var skipped = new List<BulkSkip>();

        foreach (var record in records)
        {
            try
            {
                Approve(record, currentUser.UserId, now, audit);
                approved.Add(record.Id);
            }
            catch (AppException ex)
            {
                // One unapprovable record must not sink the other 40 in the selection;
                // the queue screen shows the reasons next to the rows that stayed.
                skipped.Add(new BulkSkip(record.Id, ex.Code, ex.Message));
            }
        }

        foreach (var missing in ids.Where(id => records.All(r => r.Id != id)))
        {
            skipped.Add(new BulkSkip(missing, "NOT_FOUND", "Không tìm thấy bản ghi."));
        }

        await db.SaveChangesAsync(cancellationToken);

        return new BulkResult(approved, skipped);
    }

    /// <summary>
    /// A record already reviewed keeps its outcome: re-approving a rejected record would
    /// erase the reason the parent was told.
    /// </summary>
    private static void Approve(ViolationRecord record, int reviewerId, DateTimeOffset now, IAuditScope audit)
    {
        ViolationRules.EnsureWeekIsOpen(record.Week);

        if (record.Status != ViolationStatus.PENDING)
        {
            throw new ConflictException(
                $"Bản ghi không còn ở trạng thái chờ duyệt (hiện tại: {record.Status}).",
                "VIOLATION_NOT_PENDING");
        }

        record.Status = ViolationStatus.APPROVED;
        record.RejectReason = null;
        record.ReviewedById = reviewerId;
        record.ReviewedAt = now;

        audit.Tag(record, AuditAction.APPROVE, $"Duyệt {record.TypeCodeSnapshot}");
    }

    /// <param name="Approved">Ids that moved to APPROVED.</param>
    /// <param name="Skipped">Rows left untouched, with the reason for each.</param>
    public sealed record BulkResult(IReadOnlyList<int> Approved, IReadOnlyList<BulkSkip> Skipped);

    /// <param name="Message">Shown to the user, so it is in Vietnamese.</param>
    public sealed record BulkSkip(int Id, string Code, string Message);
}
