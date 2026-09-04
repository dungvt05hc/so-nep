using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Features.AcademicYears;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Weeks;

/// <summary>
/// Edits the label of a week and whether it counts towards the term score. An exam week,
/// or a week with only two teaching days, does not count.
/// <para>
/// A locked or published week is refused: its scores are already out, and flipping
/// <c>is_counted</c> afterwards would silently change the term score of every student in
/// every class. Re-opening a week is a Phase 3 operation with a recalculation behind it.
/// </para>
/// </summary>
public static class UpdateWeek
{
    public sealed record Request(string? Label, bool IsCounted, string? NotCountedReason);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Label).MaximumLength(100);

            RuleFor(x => x.NotCountedReason)
                .NotEmpty()
                .When(x => !x.IsCounted)
                .WithMessage("Vui lòng nhập lý do tuần không được tính điểm.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{weekId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("UpdateWeek")
            .WithSummary("Sửa tuần học");

    private static async Task<WeekResponse> HandleAsync(
        int weekId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var week = await db.AcademicWeeks
            .Include(w => w.Term)
            .FirstOrDefaultAsync(w => w.Id == weekId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tuần học.");

        if (week.Status is WeekStatus.LOCKED or WeekStatus.PUBLISHED)
        {
            throw new ConflictException("Tuần đã chốt nên không thể sửa.", "WEEK_LOCKED");
        }

        week.Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();
        week.IsCounted = request.IsCounted;
        week.NotCountedReason = request.IsCounted ? null : request.NotCountedReason?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return new WeekResponse(
            week.Id,
            week.YearId,
            week.TermId,
            week.Term.Code,
            week.WeekNo,
            week.StartDate,
            week.EndDate,
            week.Label,
            week.IsCounted,
            week.NotCountedReason,
            week.OriginalStartDate,
            week.Status,
            week.LockedAt);
    }
}
