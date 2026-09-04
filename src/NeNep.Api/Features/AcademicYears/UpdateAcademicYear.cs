using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AcademicYears;

/// <summary>Renames an academic year or moves its start and end dates.</summary>
public static class UpdateAcademicYear
{
    public sealed record Request(string Name, DateOnly StartDate, DateOnly EndDate);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên năm học.").MaximumLength(50);

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{yearId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("UpdateAcademicYear")
            .WithSummary("Sửa năm học");

    private static async Task<AcademicYearResponse> HandleAsync(
        int yearId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var year = await db.AcademicYears
            .Include(y => y.Terms)
            .FirstOrDefaultAsync(y => y.Id == yearId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy năm học.");

        var name = request.Name.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.AcademicYears.Where(y => y.Name == name && y.Id != yearId),
            $"Năm học \"{name}\" đã tồn tại.",
            $"Tên năm học \"{name}\" trùng với một năm học đã xoá.",
            cancellationToken);

        year.Name = name;
        year.StartDate = request.StartDate;
        year.EndDate = request.EndDate;

        await db.SaveChangesAsync(cancellationToken);

        return CreateAcademicYear.ToResponse(year);
    }
}
