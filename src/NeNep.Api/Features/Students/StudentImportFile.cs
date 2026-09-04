using System.Globalization;
using ClosedXML.Excel;
using NeNep.Api.Common;

namespace NeNep.Api.Features.Students;

/// <summary>One parsed line of the spreadsheet, before the database has been consulted.</summary>
public sealed record ImportRow(
    int RowNumber,
    string? Code,
    string? FullName,
    DateOnly? Dob,
    string? Gender,
    int? OrderNo,
    string? Note,
    List<string> Errors);

/// <summary>
/// Reads and writes the student list spreadsheet.
/// <para>
/// Reading never touches the database: it turns the file into rows and reports what is
/// wrong with each one by line number, because a secretary fixing a 45-line file needs to
/// be told "dòng 12" and not "có lỗi".
/// </para>
/// </summary>
public static class StudentImportFile
{
    /// <summary>Largest file accepted, in bytes. 62 classes of 45 students fit easily.</summary>
    public const long MaxFileBytes = 5 * 1024 * 1024;

    /// <summary>Largest number of data rows accepted in one file.</summary>
    public const int MaxRows = 5_000;

    private const int FirstDataRow = 2;

    // The header the school sees. Vietnamese because it is shown to the user.
    private static readonly string[] Headers =
    [
        "Mã học sinh",
        "Họ và tên",
        "Ngày sinh (dd/mm/yyyy)",
        "Giới tính (Nam/Nữ)",
        "Số thứ tự",
        "Ghi chú",
    ];

    // Accepted values of the gender column. These are DATA, stored as written.
    private static readonly string[] Genders = ["Nam", "Nữ"];

    private static readonly string[] DateFormats = ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd"];

    /// <summary>Builds the empty template offered for download, with one example row.</summary>
    public static byte[] BuildTemplate()
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.AddWorksheet("Danh sách học sinh");

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);

            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
        }

        sheet.Cell(FirstDataRow, 1).Value = "HS0001";
        sheet.Cell(FirstDataRow, 2).Value = "Nguyễn Văn An";
        sheet.Cell(FirstDataRow, 3).Value = "01/09/2012";
        sheet.Cell(FirstDataRow, 4).Value = "Nam";
        sheet.Cell(FirstDataRow, 5).Value = 1;
        sheet.Cell(FirstDataRow, 6).Value = string.Empty;

        sheet.Column(3).Style.NumberFormat.Format = "@";
        sheet.Columns(1, Headers.Length).AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    /// <summary>Parses the uploaded file into rows. Throws only when the file itself is unusable.</summary>
    public static List<ImportRow> Read(Stream stream)
    {
        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception exception) when (exception is not AppException)
        {
            throw new AppValidationException(
                "file",
                "Không đọc được tệp. Vui lòng dùng đúng tệp mẫu định dạng .xlsx.");
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new AppValidationException("file", "Tệp không có sheet nào.");

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

            if (lastRow - FirstDataRow + 1 > MaxRows)
            {
                throw new AppValidationException(
                    "file",
                    $"Tệp có quá {MaxRows} dòng dữ liệu. Vui lòng tách nhỏ tệp.");
            }

            var rows = new List<ImportRow>();

            for (var rowNumber = FirstDataRow; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);

                if (row.IsEmpty())
                {
                    continue;
                }

                rows.Add(ReadRow(row, rowNumber));
            }

            return rows;
        }
    }

    private static ImportRow ReadRow(IXLRow row, int rowNumber)
    {
        var errors = new List<string>();

        var code = Text(row.Cell(1));
        var fullName = Text(row.Cell(2));
        var gender = Text(row.Cell(4));
        var note = Text(row.Cell(6));

        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add("Thiếu mã học sinh.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add("Thiếu họ và tên.");
        }

        var dob = ReadDate(row.Cell(3), errors);
        var orderNo = ReadOrderNo(row.Cell(5), errors);

        if (gender is not null && !Genders.Contains(gender, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Giới tính chỉ nhận giá trị Nam hoặc Nữ.");
        }

        return new ImportRow(rowNumber, code, fullName, dob, gender, orderNo, note, errors);
    }

    private static DateOnly? ReadDate(IXLCell cell, List<string> errors)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.DateTime)
        {
            return DateOnly.FromDateTime(cell.GetDateTime());
        }

        var text = Text(cell);

        if (text is not null && DateOnly.TryParseExact(
                text,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        errors.Add("Ngày sinh không đúng định dạng dd/mm/yyyy.");

        return null;
    }

    private static int? ReadOrderNo(IXLCell cell, List<string> errors)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        var text = Text(cell);

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        errors.Add("Số thứ tự phải là số nguyên dương.");

        return null;
    }

    private static string? Text(IXLCell cell)
    {
        var value = cell.GetString().Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }
}
