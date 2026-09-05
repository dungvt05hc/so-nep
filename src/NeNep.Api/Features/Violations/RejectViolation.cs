using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The homeroom teacher turning down a record.
/// <para>
/// The reason is MANDATORY. The class officer who entered it sees the rejection in their
/// own list, and a rejection without a reason teaches them nothing and looks arbitrary.
/// </para>
/// </summary>
public static class RejectViolation
{
    public sealed record Request(string Reason);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty()
                .WithMessage("Vui lòng nhập lý do từ chối.")
                .MaximumLength(500);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/violations/{id:int}/reject", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("RejectViolation")
            .WithSummary("Từ chối bản ghi");

    private static async Task<ViolationResponse> HandleAsync(
        int classId,
        int id,
        Request request,
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

        ViolationRules.EnsureWeekIsOpen(record.Week);

        if (record.Status != ViolationStatus.PENDING)
        {
            throw new ConflictException(
                $"Bản ghi không còn ở trạng thái chờ duyệt (hiện tại: {record.Status}).",
                "VIOLATION_NOT_PENDING");
        }

        record.Status = ViolationStatus.REJECTED;
        record.RejectReason = request.Reason.Trim();
        record.ReviewedById = currentUser.UserId;
        record.ReviewedAt = timeProvider.GetUtcNow();

        // A rejected record never counted, so nothing is owed for it either.
        record.RemediationStatus = RemediationStatus.NOT_REQUIRED;
        record.RemediationDeadline = null;

        audit.Tag(record, AuditAction.REJECT, $"Từ chối {record.TypeCodeSnapshot}: {record.RejectReason}");

        await db.SaveChangesAsync(cancellationToken);

        return await ViolationProjection.LoadAsync(db, id, cancellationToken);
    }
}
