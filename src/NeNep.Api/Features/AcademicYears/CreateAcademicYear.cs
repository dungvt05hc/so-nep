using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AcademicYears;

/// <summary>Creates an academic year, optionally with its terms in the same call.</summary>
public static class CreateAcademicYear
{
    public sealed record TermInput(string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Ordinal);

    public sealed record Request(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsCurrent,
        IReadOnlyList<TermInput>? Terms);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Vui lòng nhập tên năm học.")
                .MaximumLength(50);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");

            RuleForEach(x => x.Terms).ChildRules(term =>
            {
                term.RuleFor(t => t.Code)
                    .NotEmpty().WithMessage("Vui lòng nhập mã học kỳ.")
                    .MaximumLength(10);

                term.RuleFor(t => t.Name).NotEmpty().WithMessage("Vui lòng nhập tên học kỳ.");

                term.RuleFor(t => t.EndDate)
                    .GreaterThan(t => t.StartDate)
                    .WithMessage("Ngày kết thúc học kỳ phải sau ngày bắt đầu.");
            });
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("CreateAcademicYear")
            .WithSummary("Tạo năm học");

    private static async Task<IResult> HandleAsync(
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.AcademicYears.Where(y => y.Name == name),
            $"Năm học \"{name}\" đã tồn tại.",
            $"Tên năm học \"{name}\" trùng với một năm học đã xoá.",
            cancellationToken);

        var year = new AcademicYear
        {
            Name = name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
        };

        foreach (var term in request.Terms ?? [])
        {
            if (term.StartDate < request.StartDate || term.EndDate > request.EndDate)
            {
                throw new AppValidationException(
                    nameof(Request.Terms),
                    $"Học kỳ {term.Code} nằm ngoài khoảng thời gian của năm học.");
            }

            year.Terms.Add(new Term
            {
                Code = term.Code.Trim(),
                Name = term.Name.Trim(),
                StartDate = term.StartDate,
                EndDate = term.EndDate,
                Ordinal = term.Ordinal,
            });
        }

        if (request.IsCurrent)
        {
            await ClearCurrentFlag(db, cancellationToken);
        }

        db.AcademicYears.Add(year);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/academic-years/{year.Id}", ToResponse(year));
    }

    /// <summary>Only one year may be the current one, so the flag is cleared everywhere else.</summary>
    internal static async Task ClearCurrentFlag(NeNepDbContext db, CancellationToken cancellationToken)
    {
        var current = await db.AcademicYears
            .Where(y => y.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var year in current)
        {
            year.IsCurrent = false;
        }
    }

    internal static AcademicYearResponse ToResponse(AcademicYear year) =>
        new(
            year.Id,
            year.Name,
            year.StartDate,
            year.EndDate,
            year.IsCurrent,
            [.. year.Terms
                .OrderBy(t => t.Ordinal)
                .Select(t => new TermResponse(t.Id, t.YearId, t.Code, t.Name, t.StartDate, t.EndDate, t.Ordinal))]);
}
