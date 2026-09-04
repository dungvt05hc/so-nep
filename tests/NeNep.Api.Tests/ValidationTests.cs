using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

/// <summary>
/// The request validators run before any handler, and report per field so the form can
/// show the message next to the box that is wrong.
/// </summary>
[Collection(ApiCollection.Name)]
public class ValidationTests
{
    private readonly ApiFactory _factory;

    public ValidationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_invalid_body_is_reported_field_by_field_in_Vietnamese()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var response = await admin.PostAsJsonAsync(
            "/api/academic-years",
            new { name = "", startDate = "2027-05-31", endDate = "2026-09-01", isCurrent = false });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");

        var errors = error.GetProperty("errors");

        errors.GetProperty("Name")[0].GetString().Should().Be("Vui lòng nhập tên năm học.");
        errors.GetProperty("EndDate")[0].GetString().Should().Be("Ngày kết thúc phải sau ngày bắt đầu.");
    }

    [Fact]
    public async Task Generating_weeks_without_terms_explains_what_is_missing()
    {
        var admin = await _factory.SignInAsync(
            await CreateAdminAsync(),
            TestSchool.Password);

        var created = await admin.PostAsJsonAsync(
            "/api/academic-years",
            new
            {
                name = $"nam-khong-hoc-ky-{Guid.NewGuid():N}",
                startDate = "2026-09-01",
                endDate = "2027-05-31",
                isCurrent = false,
            });

        var yearId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        var response = await admin.PostAsJsonAsync(
            $"/api/academic-years/{yearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("YEAR_HAS_NO_TERMS");
    }

    private async Task<string> CreateAdminAsync()
    {
        var username = $"admin-{Guid.NewGuid():N}"[..20];

        await TestUsers.AddSchoolWideAsync(_factory, username, NeNep.Domain.Enums.Role.ADMIN);

        return username;
    }
}
