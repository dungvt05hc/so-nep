using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>Adds a single student to a class, for the ones who arrive after the import.</summary>
public static class CreateStudent
{
    public sealed record Request(
        string Code,
        string FullName,
        DateOnly? Dob,
        string? Gender,
        int? OrderNo,
        string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Vui lòng nhập mã học sinh.").MaximumLength(30);
            RuleFor(x => x.FullName).NotEmpty().WithMessage("Vui lòng nhập họ và tên.").MaximumLength(100);

            RuleFor(x => x.Gender)
                .Must(g => g is null or "Nam" or "Nữ")
                .WithMessage("Giới tính chỉ nhận giá trị Nam hoặc Nữ.");

            RuleFor(x => x.OrderNo)
                .GreaterThan(0)
                .When(x => x.OrderNo is not null)
                .WithMessage("Số thứ tự phải là số nguyên dương.");
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/students", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("CreateStudent")
            .WithSummary("Thêm học sinh vào lớp");

    private static async Task<IResult> HandleAsync(
        int classId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        var code = request.Code.Trim();

        var existing = await db.Students
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException($"Mã học sinh \"{code}\" đã tồn tại.", "STUDENT_CODE_TAKEN");
        }

        var student = new Student
        {
            Code = code,
            FullName = request.FullName.Trim(),
            Dob = request.Dob,
            Gender = request.Gender,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = timeProvider.GetUtcNow(),
            Enrollments =
            {
                new Enrollment
                {
                    ClassId = target.Id,
                    YearId = target.YearId,
                    OrderNo = request.OrderNo,
                    IsActive = true,
                },
            },
        };

        db.Students.Add(student);

        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/students/{student.Id}", student.Id);
    }
}
