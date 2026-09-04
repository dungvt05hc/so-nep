using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

/// <summary>
/// The acceptance test of Phase 1: the homeroom teacher of 9/1 must not be able to read
/// or change anything belonging to 9/2, through ANY endpoint that takes a class id.
/// <para>
/// The list is written out one endpoint at a time on purpose. A new class-scoped endpoint
/// added later without a guard should show up here as a missing row, not as a hole
/// nobody notices.
/// </para>
/// </summary>
[Collection(ApiCollection.Name)]
public class ClassIsolationTests
{
    private readonly ApiFactory _factory;

    public ClassIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string, string?> ClassScopedEndpoints() => new()
    {
        { "GET", "/api/classes/{classId}", null },
        { "GET", "/api/classes/{classId}/students", null },
        { "POST", "/api/classes/{classId}/students", """{"code":"HSX1","fullName":"Nguyễn Văn X"}""" },
        { "PUT", "/api/classes/{classId}/students/{studentId}", """{"fullName":"Nguyễn Văn X"}""" },
        { "POST", "/api/classes/{classId}/students/{studentId}/leave", """{"leaveNote":"Chuyển trường"}""" },
        { "GET", "/api/classes/{classId}/officers", null },
        { "POST", "/api/classes/{classId}/officers", """{"studentId":{studentId},"role":"LOP_TRUONG","validFrom":"2026-09-07"}""" },
        { "POST", "/api/classes/{classId}/officers/1/end", """{}""" },
        { "GET", "/api/classes/{classId}/accounts", null },
        { "POST", "/api/classes/{classId}/accounts", """{"studentId":{studentId},"role":"LOP_TRUONG"}""" },
        { "POST", "/api/classes/{classId}/accounts/{officerUserId}/reset-password", null },
        { "POST", "/api/classes/{classId}/accounts/{officerUserId}/lock", null },
        { "POST", "/api/classes/{classId}/accounts/{officerUserId}/unlock", null },
        { "DELETE", "/api/classes/{classId}/accounts/{officerUserId}", null },
    };

    [Theory]
    [MemberData(nameof(ClassScopedEndpoints))]
    public async Task Homeroom_teacher_of_9_1_is_refused_on_every_endpoint_of_9_2(
        string method,
        string template,
        string? body)
    {
        var school = await TestSchool.SeedAsync(_factory);

        var officerUserId = await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class92Id,
            school.Student92Id,
            $"cb92-{school.Token}");

        var client = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var url = template
            .Replace("{classId}", school.Class92Id.ToString())
            .Replace("{studentId}", school.Student92Id.ToString())
            .Replace("{officerUserId}", officerUserId.ToString());

        var request = new HttpRequestMessage(new HttpMethod(method), url);

        if (body is not null)
        {
            var payload = body.Replace("{studentId}", school.Student92Id.ToString());

            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "the guard must refuse {0} {1} before touching any row of class 9/2",
            method,
            url);

        var error = await response.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("CLASS_ACCESS_DENIED");
    }

    [Fact]
    public async Task Import_endpoints_of_another_class_are_refused_before_the_file_is_read()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        foreach (var url in new[]
                 {
                     $"/api/classes/{school.Class92Id}/students/import/preview",
                     $"/api/classes/{school.Class92Id}/students/import",
                 })
        {
            using var content = new MultipartFormDataContent();
            using var file = new ByteArrayContent(StudentWorkbook.Build([("HS001", "Nguyễn Văn A", "01/09/2012", "Nam", 1)]));

            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            content.Add(file, "file", "danh-sach.xlsx");

            var response = await client.PostAsync(url, content);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "{0} must be guarded", url);
        }
    }

    [Fact]
    public async Task Class_listing_shows_only_the_classes_the_teacher_is_in_charge_of()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var response = await client.GetAsync($"/api/classes?yearId={school.YearId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var classes = await response.ReadJsonAsync();
        var ids = classes.EnumerateArray().Select(c => c.GetProperty("id").GetInt32()).ToList();

        ids.Should().Contain(school.Class91Id);
        ids.Should().NotContain(school.Class92Id);
    }

    [Fact]
    public async Task The_school_board_reads_every_class_but_may_not_change_one()
    {
        var school = await TestSchool.SeedAsync(_factory);

        var boardUsername = $"bgh-{school.Token}";

        await TestUsers.AddSchoolWideAsync(_factory, boardUsername, NeNep.Domain.Enums.Role.BGH);

        var client = await _factory.SignInAsync(boardUsername, TestSchool.Password);

        var read = await client.GetAsync($"/api/classes/{school.Class92Id}");

        read.StatusCode.Should().Be(HttpStatusCode.OK);

        var write = await client.PostAsJsonAsync(
            $"/api/classes/{school.Class92Id}/students",
            new { code = $"HSBGH{school.Token}", fullName = "Không được phép" });

        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
