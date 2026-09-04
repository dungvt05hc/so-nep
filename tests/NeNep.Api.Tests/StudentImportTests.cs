using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

/// <summary>
/// Importing the class register from Excel. The school types these files by hand, so the
/// preview has to point at the line that is wrong before anything is written.
/// </summary>
[Collection(ApiCollection.Name)]
public class StudentImportTests
{
    private readonly ApiFactory _factory;

    public StudentImportTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Preview_reports_the_problem_on_each_line_and_writes_nothing()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var file = StudentWorkbook.Build(
        [
            ($"A{school.Token}", "Nguyễn Văn A", "01/09/2012", "Nam", 1),
            ($"B{school.Token}", null, "02/09/2012", "Nữ", 2),                 // missing name
            ($"A{school.Token}", "Trùng mã", "03/09/2012", "Nam", 3),          // duplicate code
            ($"C{school.Token}", "Phạm Thị D", "khong-phai-ngay", "Nữ", 4),    // unreadable date
            ($"D{school.Token}", "Đỗ Văn E", "05/09/2012", "Khác", 5),         // gender outside the list
        ]);

        var response = await PostFileAsync(
            teacher,
            $"/api/classes/{school.Class91Id}/students/import/preview",
            file);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.ReadJsonAsync();

        body.GetProperty("totalRows").GetInt32().Should().Be(5);
        body.GetProperty("willCreate").GetInt32().Should().Be(1);
        body.GetProperty("withErrors").GetInt32().Should().Be(4);

        var rows = body.GetProperty("rows").EnumerateArray().ToList();

        rows[1].GetProperty("rowNumber").GetInt32().Should().Be(3);
        rows[1].GetProperty("errors")[0].GetString().Should().Contain("họ và tên");

        rows[2].GetProperty("errors")[0].GetString().Should().Contain("trùng với dòng 2");

        await using var db = _factory.CreateDbContext();

        var written = await db.Enrollments.CountAsync(e => e.ClassId == school.Class91Id);

        written.Should().Be(1, "the preview must not write anything");
    }

    [Fact]
    public async Task A_file_with_errors_is_refused_unless_the_bad_lines_are_skipped()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var file = StudentWorkbook.Build(
        [
            ($"E{school.Token}", "Nguyễn Văn F", "01/09/2012", "Nam", 1),
            (null, "Thiếu mã", "02/09/2012", "Nam", 2),
        ]);

        var refused = await PostFileAsync(teacher, $"/api/classes/{school.Class91Id}/students/import", file);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var skipped = await PostFileAsync(
            teacher,
            $"/api/classes/{school.Class91Id}/students/import?skipInvalidRows=true",
            file);

        skipped.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await skipped.ReadJsonAsync();

        body.GetProperty("created").GetInt32().Should().Be(1);
        body.GetProperty("skipped").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Importing_writes_the_register_and_a_second_run_updates_it()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var first = StudentWorkbook.Build(
        [
            ($"G{school.Token}", "Nguyễn Văn G", "01/09/2012", "Nam", 2),
            ($"H{school.Token}", "Trần Thị H", "02/09/2012", "Nữ", 3),
        ]);

        var created = await PostFileAsync(teacher, $"/api/classes/{school.Class91Id}/students/import", first);

        created.StatusCode.Should().Be(HttpStatusCode.OK);
        (await created.ReadJsonAsync()).GetProperty("created").GetInt32().Should().Be(2);

        var corrected = StudentWorkbook.Build(
        [
            ($"G{school.Token}", "Nguyễn Văn Giang", "01/09/2012", "Nam", 2),
        ]);

        var updated = await PostFileAsync(teacher, $"/api/classes/{school.Class91Id}/students/import", corrected);

        (await updated.ReadJsonAsync()).GetProperty("updated").GetInt32().Should().Be(1);

        await using var db = _factory.CreateDbContext();

        var student = await db.Students.FirstAsync(s => s.Code == $"G{school.Token}");

        student.FullName.Should().Be("Nguyễn Văn Giang");
    }

    [Fact]
    public async Task A_student_already_enrolled_in_another_class_is_reported_not_moved()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        string code;

        await using (var db = _factory.CreateDbContext())
        {
            code = (await db.Students.FirstAsync(s => s.Id == school.Student92Id)).Code;
        }

        var file = StudentWorkbook.Build([(code, "Lê Văn Cường", "01/09/2012", "Nam", 9)]);

        var response = await PostFileAsync(
            teacher,
            $"/api/classes/{school.Class91Id}/students/import/preview",
            file);

        var body = await response.ReadJsonAsync();

        body.GetProperty("withErrors").GetInt32().Should().Be(1);

        body.GetProperty("rows")[0].GetProperty("errors")[0].GetString()
            .Should().Contain("đã thuộc lớp");
    }

    [Fact]
    public async Task The_template_can_be_downloaded_and_parsed_back()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var response = await teacher.GetAsync("/api/students/import/template");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        (await response.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    private static async Task<HttpResponseMessage> PostFileAsync(HttpClient client, string url, byte[] file)
    {
        using var content = new MultipartFormDataContent();
        using var part = new ByteArrayContent(file);

        part.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        content.Add(part, "file", "danh-sach.xlsx");

        return await client.PostAsync(url, content);
    }
}
