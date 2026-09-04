using NeNep.Api.Common;
using NeNep.Api.Security;

namespace NeNep.Api.Features.Students;

public static class StudentImportEndpoints
{
    private const string XlsxContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>Serves the blank spreadsheet the school fills in.</summary>
    public static RouteHandlerBuilder MapTemplate(RouteGroupBuilder studentGroup) =>
        studentGroup.MapGet("/import/template", () =>
                Results.File(
                    StudentImportFile.BuildTemplate(),
                    XlsxContentType,
                    "mau-danh-sach-hoc-sinh.xlsx"))
            .RequireAuthorization(Policies.ManageClass)
            .WithName("GetStudentImportTemplate")
            .WithSummary("Tải tệp mẫu danh sách học sinh");

    /// <summary>Rejects an upload that is missing, empty, too large, or not a workbook.</summary>
    public static void EnsureAcceptableFile(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppValidationException("file", "Vui lòng chọn tệp danh sách học sinh.");
        }

        if (file.Length > StudentImportFile.MaxFileBytes)
        {
            var megabytes = StudentImportFile.MaxFileBytes / (1024 * 1024);

            throw new AppValidationException("file", $"Tệp vượt quá {megabytes} MB.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("file", "Chỉ chấp nhận tệp Excel định dạng .xlsx.");
        }
    }
}
