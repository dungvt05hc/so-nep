using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Features.Catalog;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Domain.Time;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Violations;

/// <summary>
/// The homeroom teacher correcting a record — most often fixing the code or the day
/// before approving it.
/// <para>
/// Changing the code RE-TAKES the snapshot, because the record now stands for a different
/// offence and must carry that one's code, name and points. Changing the date can move
/// the record into another week, so both the week it leaves and the week it enters have
/// to still be open.
/// </para>
/// </summary>
public static class UpdateViolation
{
    public sealed record Request(
        int? TypeId,
        DateOnly? OccurredDate,
        int? PeriodNo,
        int? Quantity,
        string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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
        classGroup.MapPut("/{classId:int}/violations/{id:int}", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("UpdateViolation")
            .WithSummary("Sửa bản ghi");

    private static async Task<ViolationResponse> HandleAsync(
        int classId,
        int id,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        ISchoolSettings settings,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var scope = await guard.GetScopeAsync(cancellationToken);

        var record = await db.ViolationRecords
            .Include(r => r.Week)
            .FirstOrDefaultAsync(r => r.Id == id && r.ClassId == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy bản ghi.");

        ViolationRules.EnsureWeekIsOpen(record.Week);

        var occurredDate = request.OccurredDate ?? record.OccurredDate;

        if (occurredDate > VietnamClock.Today(timeProvider))
        {
            throw new AppValidationException(
                nameof(Request.OccurredDate),
                "Không thể ghi nhận cho ngày trong tương lai.");
        }

        var typeId = request.TypeId ?? record.TypeId;
        var quantity = request.Quantity ?? record.Quantity;

        var entry = await ClassCatalog.ResolveAsync(db, settings, classId, typeId, cancellationToken);

        ViolationRules.EnsureMayRecord(scope, classId, entry.Type);

        var week = record.Week;

        if (occurredDate != record.OccurredDate)
        {
            var target = await db.Classes
                .Where(c => c.Id == classId)
                .Select(c => c.YearId)
                .FirstAsync(cancellationToken);

            week = await ViolationRules.ResolveWeekAsync(db, target, occurredDate, cancellationToken);

            ViolationRules.EnsureWeekIsOpen(week);
        }

        await ViolationRules.EnsureWithinWeeklyLimitAsync(
            db,
            entry.Type,
            record.StudentId,
            week.Id,
            quantity,
            excludeRecordId: record.Id,
            cancellationToken);

        record.WeekId = week.Id;
        record.OccurredDate = occurredDate;
        record.PeriodNo = request.PeriodNo ?? record.PeriodNo;
        record.Quantity = quantity;
        record.Note = string.IsNullOrWhiteSpace(request.Note) ? record.Note : request.Note.Trim();

        if (typeId != record.TypeId)
        {
            ViolationRules.ApplySnapshot(record, entry.Type, entry.EffectivePoints);
            ViolationRules.ApplyRemediation(record, entry.Type);
        }
        else if (record.RemediationStatus == RemediationStatus.PENDING)
        {
            // The deadline is counted from the date of the violation, so moving the date
            // moves the deadline with it.
            record.RemediationDeadline = occurredDate.AddDays(entry.Type.RemediationDays ?? 0);
        }

        await db.SaveChangesAsync(cancellationToken);

        return await ViolationProjection.LoadAsync(db, id, cancellationToken);
    }
}
