using Microsoft.EntityFrameworkCore;
using NeNep.Domain.Entities;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>A row of the file after the database has been consulted.</summary>
public sealed record PlannedImportRow(
    ImportRow Row,
    ImportAction Action,
    int? StudentId,
    int? EnrollmentId,
    List<string> Errors)
{
    public ImportRowResponse ToResponse() =>
        new(Row.RowNumber, Row.Code, Row.FullName, Row.Dob, Row.Gender, Row.OrderNo, Action, Errors);
}

/// <summary>
/// Decides what the import would do with each row, without writing anything. Preview and
/// commit run exactly the same plan, so what the user approved is what gets written.
/// </summary>
public static class StudentImportPlanner
{
    public static async Task<List<PlannedImportRow>> PlanAsync(
        NeNepDbContext db,
        Class targetClass,
        IReadOnlyList<ImportRow> rows,
        CancellationToken cancellationToken)
    {
        var codes = rows
            .Where(r => r.Code is not null)
            .Select(r => r.Code!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Deleted students are included on purpose: their code still occupies the unique
        // index, so importing it again would fail at the database with an opaque error.
        var students = await db.Students
            .IgnoreQueryFilters()
            .Where(s => codes.Contains(s.Code))
            .Select(s => new { s.Id, s.Code, s.DeletedAt })
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(s => s.Id).ToList();

        var enrollments = await db.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.YearId == targetClass.YearId)
            .Select(e => new { e.Id, e.StudentId, e.ClassId, ClassCode = e.Class.Code })
            .ToListAsync(cancellationToken);

        var byCode = students.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);
        var byStudent = enrollments.ToDictionary(e => e.StudentId);

        var seenCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenOrderNos = new Dictionary<int, int>();

        var planned = new List<PlannedImportRow>(rows.Count);

        foreach (var row in rows)
        {
            var errors = new List<string>(row.Errors);

            if (row.Code is { } code)
            {
                if (seenCodes.TryGetValue(code, out var firstRow))
                {
                    errors.Add($"Mã học sinh bị trùng với dòng {firstRow} trong tệp.");
                }
                else
                {
                    seenCodes[code] = row.RowNumber;
                }
            }

            if (row.OrderNo is { } orderNo)
            {
                if (seenOrderNos.TryGetValue(orderNo, out var firstRow))
                {
                    errors.Add($"Số thứ tự bị trùng với dòng {firstRow} trong tệp.");
                }
                else
                {
                    seenOrderNos[orderNo] = row.RowNumber;
                }
            }

            var action = ImportAction.CREATE;
            int? studentId = null;
            int? enrollmentId = null;

            if (row.Code is { } lookup && byCode.TryGetValue(lookup, out var student))
            {
                studentId = student.Id;

                if (student.DeletedAt is not null)
                {
                    errors.Add("Mã học sinh này thuộc về một hồ sơ đã xoá. Vui lòng liên hệ quản trị.");
                }
                else if (byStudent.TryGetValue(student.Id, out var enrollment))
                {
                    if (enrollment.ClassId == targetClass.Id)
                    {
                        action = ImportAction.UPDATE;
                        enrollmentId = enrollment.Id;
                    }
                    else
                    {
                        errors.Add($"Học sinh đã thuộc lớp {enrollment.ClassCode} trong năm học này.");
                    }
                }
                else
                {
                    action = ImportAction.ENROLL;
                }
            }

            planned.Add(new PlannedImportRow(
                row,
                errors.Count > 0 ? ImportAction.ERROR : action,
                studentId,
                enrollmentId,
                errors));
        }

        return planned;
    }
}
