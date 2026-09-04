using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.SchoolBreaks;

/// <summary>
/// Records a school break: Tết, a public holiday, or an unplanned closure for a storm or
/// an epidemic. Creating it changes no dates — that happens when it is applied, so the
/// administrator can enter a provisional break and confirm it later.
/// </summary>
public static class CreateSchoolBreak
{
    public sealed record Request(
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsConfirmed,
        string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên kỳ nghỉ.").MaximumLength(100);

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("Ngày kết thúc không được trước ngày bắt đầu.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder yearGroup) =>
        yearGroup.MapPost("/{yearId:int}/breaks", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("CreateSchoolBreak")
            .WithSummary("Tạo kỳ nghỉ");

    private static async Task<IResult> HandleAsync(
        int yearId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");

        if (request.StartDate < year.StartDate || request.EndDate > year.EndDate)
        {
            throw new AppValidationException(
                nameof(Request.StartDate),
                "Kỳ nghỉ phải nằm trong khoảng thời gian của năm học.");
        }

        var schoolBreak = new SchoolBreak
        {
            YearId = yearId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsConfirmed = request.IsConfirmed,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
        };

        db.SchoolBreaks.Add(schoolBreak);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/breaks/{schoolBreak.Id}", ToResponse(schoolBreak));
    }

    internal static SchoolBreakResponse ToResponse(SchoolBreak b) =>
        new(b.Id, b.YearId, b.Name, b.StartDate, b.EndDate, b.IsConfirmed, b.ShiftedWeeks, b.AppliedAt, b.AppliedById, b.Note);
}
