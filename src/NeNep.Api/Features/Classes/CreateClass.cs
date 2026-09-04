using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

/// <summary>Creates a class inside one academic year.</summary>
public static class CreateClass
{
    public sealed record Request(int GradeId, int YearId, string Code, string Name, int? HomeroomTeacherId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Vui lòng nhập mã lớp.").MaximumLength(20);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên lớp.").MaximumLength(100);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPost("/", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("CreateClass")
            .WithSummary("Tạo lớp");

    private static async Task<IResult> HandleAsync(
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.Grades.AnyAsync(g => g.Id == request.GradeId, cancellationToken))
        {
            throw new AppValidationException(nameof(Request.GradeId), "Không tìm thấy khối.");
        }

        if (!await db.AcademicYears.AnyAsync(y => y.Id == request.YearId, cancellationToken))
        {
            throw new AppValidationException(nameof(Request.YearId), "Không tìm thấy năm học.");
        }

        var code = request.Code.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.Classes.Where(c => c.YearId == request.YearId && c.Code == code),
            $"Lớp \"{code}\" đã tồn tại trong năm học này.",
            $"Mã lớp \"{code}\" trùng với một lớp đã xoá của năm học này.",
            cancellationToken);

        if (request.HomeroomTeacherId is { } teacherId)
        {
            await ClassQueries.EnsureIsHomeroomTeacherAsync(db, teacherId, cancellationToken);
        }

        var entity = new Class
        {
            GradeId = request.GradeId,
            YearId = request.YearId,
            Code = code,
            Name = request.Name.Trim(),
            HomeroomTeacherId = request.HomeroomTeacherId,
        };

        db.Classes.Add(entity);

        await db.SaveChangesAsync(cancellationToken);

        var created = await ClassQueries
            .Project(db.Classes.AsNoTracking().Where(c => c.Id == entity.Id))
            .FirstAsync(cancellationToken);

        return Results.Created($"/api/classes/{entity.Id}", created);
    }
}
