using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Features.AcademicYears;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Terms;

/// <summary>
/// Edits a term. The dates cannot be moved once a week of that term has been locked,
/// because the locked scores were computed against the term as it stood.
/// </summary>
public static class UpdateTerm
{
    public sealed record Request(string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Ordinal);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Vui lòng nhập mã học kỳ.").MaximumLength(10);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên học kỳ.");
            RuleFor(x => x.Ordinal).GreaterThan(0).WithMessage("Thứ tự học kỳ phải lớn hơn 0.");

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{termId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("UpdateTerm")
            .WithSummary("Sửa học kỳ");

    private static async Task<TermResponse> HandleAsync(
        int termId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var term = await db.Terms.FirstOrDefaultAsync(t => t.Id == termId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy học kỳ.");

        var datesChanged = term.StartDate != request.StartDate || term.EndDate != request.EndDate;

        if (datesChanged)
        {
            var locked = await db.AcademicWeeks.AnyAsync(
                w => w.TermId == termId && (w.Status == WeekStatus.LOCKED || w.Status == WeekStatus.PUBLISHED),
                cancellationToken);

            if (locked)
            {
                throw new ConflictException(
                    "Học kỳ đã có tuần được chốt nên không thể đổi ngày.",
                    "TERM_HAS_LOCKED_WEEKS");
            }
        }

        var code = request.Code.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.Terms.Where(t => t.YearId == term.YearId && t.Code == code && t.Id != termId),
            $"Học kỳ \"{code}\" đã tồn tại trong năm học này.",
            $"Mã học kỳ \"{code}\" trùng với một học kỳ đã xoá của năm học này.",
            cancellationToken);

        term.Code = code;
        term.Name = request.Name.Trim();
        term.StartDate = request.StartDate;
        term.EndDate = request.EndDate;
        term.Ordinal = request.Ordinal;

        await db.SaveChangesAsync(cancellationToken);

        return new TermResponse(term.Id, term.YearId, term.Code, term.Name, term.StartDate, term.EndDate, term.Ordinal);
    }
}
