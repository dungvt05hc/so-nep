using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Features.AcademicYears;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Terms;

/// <summary>Adds a term to an academic year.</summary>
public static class CreateTerm
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

    public static RouteHandlerBuilder Map(RouteGroupBuilder yearGroup) =>
        yearGroup.MapPost("/{yearId:int}/terms", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("CreateTerm")
            .WithSummary("Tạo học kỳ");

    private static async Task<IResult> HandleAsync(
        int yearId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");

        var code = request.Code.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.Terms.Where(t => t.YearId == yearId && t.Code == code),
            $"Học kỳ \"{code}\" đã tồn tại trong năm học này.",
            $"Mã học kỳ \"{code}\" trùng với một học kỳ đã xoá của năm học này.",
            cancellationToken);

        if (request.StartDate < year.StartDate || request.EndDate > year.EndDate)
        {
            throw new AppValidationException(
                nameof(Request.StartDate),
                "Học kỳ phải nằm trong khoảng thời gian của năm học.");
        }

        var overlapping = await db.Terms
            .Where(t => t.YearId == yearId
                && t.StartDate <= request.EndDate
                && request.StartDate <= t.EndDate)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (overlapping is not null)
        {
            throw new ConflictException($"Khoảng thời gian bị trùng với học kỳ {overlapping}.");
        }

        var term = new Term
        {
            YearId = yearId,
            Code = code,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Ordinal = request.Ordinal,
        };

        db.Terms.Add(term);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/terms/{term.Id}",
            new TermResponse(term.Id, term.YearId, term.Code, term.Name, term.StartDate, term.EndDate, term.Ordinal));
    }
}
