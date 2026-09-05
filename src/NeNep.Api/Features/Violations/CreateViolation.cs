using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Features.Catalog;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The quick-entry screen: one student, one code, done in under 15 seconds on a phone.
/// <para>
/// Everything that could distort a score is decided HERE, on the server, from data the
/// client never sees: which week the date belongs to, how many points the code is worth
/// in this class, whether the caller may record it, and whether it needs flagging.
/// </para>
/// </summary>
public static class CreateViolation
{
    public sealed record Request(
        int StudentId,
        int TypeId,
        DateOnly OccurredDate,
        int? PeriodNo,
        int? Quantity,
        string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.StudentId).GreaterThan(0).WithMessage("Vui lòng chọn học sinh.");
            RuleFor(x => x.TypeId).GreaterThan(0).WithMessage("Vui lòng chọn mã lỗi.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 50)
                .When(x => x.Quantity is not null)
                .WithMessage("Số lần phải từ 1 đến 50.");

            RuleFor(x => x.PeriodNo)
                .GreaterThan(0)
                .When(x => x.PeriodNo is not null)
                .WithMessage("Tiết học phải là số nguyên dương.");

            RuleFor(x => x.Note).MaximumLength(500);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/violations", HandleAsync)
            .RequireAuthorization(Policies.ReportRecords)
            .WithValidation<Request>()
            .WithName("CreateViolation")
            .WithSummary("Ghi nhận lỗi / điểm cộng");

    private static async Task<IResult> HandleAsync(
        int classId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISchoolSettings settings,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var scope = await guard.GetScopeAsync(cancellationToken);

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        // "Today" is a Vietnamese business date; a record entered at 21:00 belongs to the
        // day the school is living, not to whatever UTC says.
        var today = VietnamClock.Today(timeProvider);

        if (request.OccurredDate > today)
        {
            throw new AppValidationException(
                nameof(Request.OccurredDate),
                "Không thể ghi nhận cho ngày trong tương lai.");
        }

        await ViolationRules.EnsureEnrolledAsync(db, classId, request.StudentId, cancellationToken);

        var entry = await ClassCatalog.ResolveAsync(db, settings, classId, request.TypeId, cancellationToken);

        ViolationRules.EnsureMayRecord(scope, classId, entry.Type);

        var callerStudentId = await ViolationRules.GetCallerStudentIdAsync(
            db,
            currentUser.UserId,
            cancellationToken);

        ViolationRules.EnsureNotSelfAwardedBonus(
            scope,
            classId,
            entry.Type,
            entry.EffectivePoints,
            callerStudentId,
            request.StudentId);

        var week = await ViolationRules.ResolveWeekAsync(db, target.YearId, request.OccurredDate, cancellationToken);

        ViolationRules.EnsureWeekIsOpen(week);

        var quantity = request.Quantity ?? 1;

        await ViolationRules.EnsureWithinWeeklyLimitAsync(
            db,
            entry.Type,
            request.StudentId,
            week.Id,
            quantity,
            excludeRecordId: null,
            cancellationToken);

        var now = timeProvider.GetUtcNow();
        var reviewsOwnRecords = ViolationRules.ReviewsOwnRecords(scope, classId);

        var record = new ViolationRecord
        {
            ClassId = classId,
            StudentId = request.StudentId,
            WeekId = week.Id,
            Quantity = quantity,
            OccurredDate = request.OccurredDate,
            PeriodNo = request.PeriodNo,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = reviewsOwnRecords ? ViolationStatus.APPROVED : ViolationStatus.PENDING,
            ReviewedById = reviewsOwnRecords ? currentUser.UserId : null,
            ReviewedAt = reviewsOwnRecords ? now : null,
            IsFlagged = await ViolationRules.IsPeerReportAsync(
                db,
                scope,
                classId,
                request.StudentId,
                request.OccurredDate,
                callerStudentId,
                cancellationToken),
            ReportedById = currentUser.UserId,
            ReportedAt = now,
        };

        ViolationRules.ApplySnapshot(record, entry.Type, entry.EffectivePoints);
        ViolationRules.ApplyRemediation(record, entry.Type);

        db.ViolationRecords.Add(record);

        await db.SaveChangesAsync(cancellationToken);

        var response = await ViolationProjection.LoadAsync(db, record.Id, cancellationToken);

        return Results.Created($"/api/classes/{classId}/violations/{record.Id}", response);
    }
}
