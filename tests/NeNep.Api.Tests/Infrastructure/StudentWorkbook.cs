using ClosedXML.Excel;

namespace NeNep.Api.Tests.Infrastructure;

/// <summary>Builds an import spreadsheet in memory, in the layout of the downloaded template.</summary>
public static class StudentWorkbook
{
    public static byte[] Build(IEnumerable<(string? Code, string? FullName, string? Dob, string? Gender, int? OrderNo)> rows)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.AddWorksheet("Danh sách học sinh");

        sheet.Cell(1, 1).Value = "Mã học sinh";
        sheet.Cell(1, 2).Value = "Họ và tên";
        sheet.Cell(1, 3).Value = "Ngày sinh (dd/mm/yyyy)";
        sheet.Cell(1, 4).Value = "Giới tính (Nam/Nữ)";
        sheet.Cell(1, 5).Value = "Số thứ tự";
        sheet.Cell(1, 6).Value = "Ghi chú";

        var rowNumber = 2;

        foreach (var row in rows)
        {
            sheet.Cell(rowNumber, 1).Value = row.Code ?? string.Empty;
            sheet.Cell(rowNumber, 2).Value = row.FullName ?? string.Empty;
            sheet.Cell(rowNumber, 3).SetValue(row.Dob ?? string.Empty);
            sheet.Cell(rowNumber, 4).Value = row.Gender ?? string.Empty;

            if (row.OrderNo is { } orderNo)
            {
                sheet.Cell(rowNumber, 5).Value = orderNo;
            }

            rowNumber++;
        }

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}
