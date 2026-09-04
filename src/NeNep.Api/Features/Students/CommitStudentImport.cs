using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>
/// Writes an approved student list into a class.
/// <para>
/// The file is parsed and planned again here rather than trusting a preview the client
/// kept: the data may have moved in between. If any line is still wrong the whole file is
/// refused, unless the caller explicitly asks to skip the bad lines.
/// </para>
/// </summary>
public static class CommitStudentImport
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/students/import", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .DisableAntiforgery()
            .WithName("CommitStudentImport")
            .WithSummary("Nhập danh sách học sinh từ Excel");

    private static async Task<ImportResultResponse> HandleAsync(
        int classId,
        IFormFile file,
        NeNepDbContext db,
        IClassAccessGuard guard,
        TimeProvider timeProvider,
        CancellationToken cancellationToken,
        bool skipInvalidRows = false)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        StudentImportEndpoints.EnsureAcceptableFile(file);

        await using var stream = file.OpenReadStream();

        var rows = StudentImportFile.Read(stream);
        var planned = await StudentImportPlanner.PlanAsync(db, target, rows, cancellationToken);

        var invalid = planned.Count(p => p.Action == ImportAction.ERROR);

        if (invalid > 0 && !skipInvalidRows)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["file"] =
                [
                    $"Tệp còn {invalid} dòng bị lỗi. Vui lòng sửa tệp rồi nhập lại, "
                    + "hoặc chọn bỏ qua các dòng lỗi.",
                ],
            });
        }

        var now = timeProvider.GetUtcNow();
        var created = 0;
        var updated = 0;
        var enrolled = 0;

        // One transaction: a half-imported class register is worse than none.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var plan in planned.Where(p => p.Action != ImportAction.ERROR))
        {
            var row = plan.Row;

            switch (plan.Action)
            {
                case ImportAction.CREATE:
                    db.Students.Add(new Student
                    {
                        Code = row.Code!,
                        FullName = row.FullName!,
                        Dob = row.Dob,
                        Gender = row.Gender,
                        Note = row.Note,
                        CreatedAt = now,
                        Enrollments =
                        {
                            new Enrollment
                            {
                                ClassId = target.Id,
                                YearId = target.YearId,
                                OrderNo = row.OrderNo,
                                IsActive = true,
                            },
                        },
                    });

                    created++;
                    break;

                case ImportAction.UPDATE:
                {
                    var student = await db.Students.FirstAsync(s => s.Id == plan.StudentId, cancellationToken);
                    var enrollment = await db.Enrollments.FirstAsync(e => e.Id == plan.EnrollmentId, cancellationToken);

                    Apply(student, row);

                    enrollment.OrderNo = row.OrderNo ?? enrollment.OrderNo;
                    enrollment.IsActive = true;
                    enrollment.LeftAt = null;

                    updated++;
                    break;
                }

                case ImportAction.ENROLL:
                {
                    var student = await db.Students.FirstAsync(s => s.Id == plan.StudentId, cancellationToken);

                    Apply(student, row);

                    db.Enrollments.Add(new Enrollment
                    {
                        StudentId = student.Id,
                        ClassId = target.Id,
                        YearId = target.YearId,
                        OrderNo = row.OrderNo,
                        IsActive = true,
                    });

                    enrolled++;
                    break;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ImportResultResponse(
            target.Id,
            target.Code,
            created,
            updated,
            enrolled,
            invalid,
            [.. planned.Select(p => p.ToResponse())]);
    }

    private static void Apply(Student student, ImportRow row)
    {
        student.FullName = row.FullName!;
        student.Dob = row.Dob ?? student.Dob;
        student.Gender = row.Gender ?? student.Gender;
        student.Note = row.Note ?? student.Note;
    }
}
