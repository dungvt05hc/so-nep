using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>
/// Edits one student and their place in the class register.
/// <para>
/// The class is taken from the URL and checked against the guard, and the student must
/// actually be enrolled in that class — a teacher cannot reach a student of another class
/// by guessing their id.
/// </para>
/// </summary>
public static class UpdateStudent
{
    public sealed record Request(string FullName, DateOnly? Dob, string? Gender, int? OrderNo, string? Note);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
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
        classGroup.MapPut("/{classId:int}/students/{studentId:int}", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .WithValidation<Request>()
            .WithName("UpdateStudent")
            .WithSummary("Sửa thông tin học sinh");

    private static async Task<StudentResponse> HandleAsync(
        int classId,
        int studentId,
        Request request,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var enrollment = await db.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
            .FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId, cancellationToken)
            ?? throw new NotFoundException("Học sinh không thuộc lớp này.");

        var student = enrollment.Student;

        student.FullName = request.FullName.Trim();
        student.Dob = request.Dob;
        student.Gender = request.Gender;
        student.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        enrollment.OrderNo = request.OrderNo;

        await db.SaveChangesAsync(cancellationToken);

        return new StudentResponse(
            student.Id,
            student.Code,
            student.FullName,
            student.Dob,
            student.Gender,
            student.Note,
            enrollment.Id,
            enrollment.ClassId,
            enrollment.Class.Code,
            enrollment.OrderNo,
            enrollment.IsActive,
            enrollment.LeftAt,
            await db.Users.AnyAsync(u => u.StudentId == student.Id, cancellationToken));
    }
}
