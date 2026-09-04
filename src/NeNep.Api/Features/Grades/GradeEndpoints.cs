using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Auditing;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Grades;

public sealed record GradeResponse(int Id, int Level, string Name, int ClassCount);

/// <summary>
/// The four grade levels of a lower-secondary school. Small enough that listing and
/// creating live in one file.
/// </summary>
public static class GradeEndpoints
{
    public sealed record CreateRequest(int Level, string Name);

    public sealed class CreateValidator : AbstractValidator<CreateRequest>
    {
        public CreateValidator()
        {
            // 6 to 9 is what a lower-secondary school in Vietnam has; it is a fact about
            // the school, not a threshold of the conduct regulation.
            RuleFor(x => x.Level)
                .InclusiveBetween(6, 9)
                .WithMessage("Khối phải nằm trong khoảng 6 đến 9.");

            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên khối.").MaximumLength(50);
        }
    }

    public static IEndpointRouteBuilder MapGradeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/grades").WithTags("Grades");

        group.MapGet("/", ListAsync)
            .RequireAuthorization()
            .WithName("ListGrades")
            .WithSummary("Danh sách khối");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<CreateRequest>()
            .WithName("CreateGrade")
            .WithSummary("Tạo khối");

        group.MapDelete("/{gradeId:int}", DeleteAsync)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteGrade")
            .WithSummary("Xoá khối");

        return app;
    }

    private static async Task<IReadOnlyList<GradeResponse>> ListAsync(
        NeNepDbContext db,
        CancellationToken cancellationToken) =>
        await db.Grades
            .AsNoTracking()
            .OrderBy(g => g.Level)
            .Select(g => new GradeResponse(g.Id, g.Level, g.Name, g.Classes.Count))
            .ToListAsync(cancellationToken);

    private static async Task<IResult> CreateAsync(
        CreateRequest request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        await SoftDelete.EnsureFreeAsync(
            db.Grades.Where(g => g.Level == request.Level),
            $"Khối {request.Level} đã tồn tại.",
            $"Khối {request.Level} trùng với một khối đã xoá. Vui lòng liên hệ quản trị để khôi phục.",
            cancellationToken);

        var grade = new Grade { Level = request.Level, Name = request.Name.Trim() };

        db.Grades.Add(grade);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/grades/{grade.Id}", new GradeResponse(grade.Id, grade.Level, grade.Name, 0));
    }

    /// <summary>Soft deletes a grade, only while no class belongs to it.</summary>
    private static async Task<IResult> DeleteAsync(
        int gradeId,
        NeNepDbContext db,
        IAuditScope auditScope,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var grade = await db.Grades.FirstOrDefaultAsync(g => g.Id == gradeId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy khối.");

        // Deleted classes count too: they still point at this grade.
        if (await db.Classes.IgnoreQueryFilters().AnyAsync(c => c.GradeId == gradeId, cancellationToken))
        {
            throw new ConflictException("Khối đã có lớp nên không xoá được.", "GRADE_IN_USE");
        }

        grade.DeletedAt = timeProvider.GetUtcNow();

        auditScope.Tag(grade, AuditAction.DELETE, $"Xoá khối {grade.Name}.");

        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
