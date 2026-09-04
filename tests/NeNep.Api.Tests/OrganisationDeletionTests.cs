using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

/// <summary>
/// Deleting a year, a term, a class or a grade created by mistake. Nothing is ever
/// removed from the database, and nothing that is already in use can go.
/// </summary>
[Collection(ApiCollection.Name)]
public class OrganisationDeletionTests
{
    private readonly ApiFactory _factory;

    public OrganisationDeletionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task An_empty_year_is_soft_deleted_and_disappears_from_the_listing()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var name = $"nam-nham-{school.Token}";

        var created = await admin.PostAsJsonAsync(
            "/api/academic-years",
            new { name, startDate = "2028-09-01", endDate = "2029-05-31", isCurrent = false });

        var yearId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        var deleted = await admin.DeleteAsync($"/api/academic-years/{yearId}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listing = await (await admin.GetAsync("/api/academic-years")).ReadJsonAsync();

        listing.EnumerateArray().Select(y => y.GetProperty("id").GetInt32())
            .Should().NotContain(yearId);

        await using var db = _factory.CreateDbContext();

        var row = await db.AcademicYears.IgnoreQueryFilters().FirstAsync(y => y.Id == yearId);

        row.DeletedAt.Should().NotBeNull("the row stays, only marked as deleted");
    }

    [Fact]
    public async Task A_year_that_already_has_terms_and_weeks_cannot_be_deleted()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var response = await admin.DeleteAsync($"/api/academic-years/{school.YearId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("YEAR_IN_USE");
        error.GetProperty("message").GetString().Should().Contain("học kỳ");
    }

    [Fact]
    public async Task The_current_year_is_protected()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        await admin.PostAsync($"/api/academic-years/{school.YearId}/set-current", content: null);

        var response = await admin.DeleteAsync($"/api/academic-years/{school.YearId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("YEAR_IS_CURRENT");
    }

    [Fact]
    public async Task A_term_can_go_until_its_weeks_are_generated()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        // A term of a fresh year, with no week behind it yet.
        var year = await admin.PostAsJsonAsync(
            "/api/academic-years",
            new
            {
                name = $"nam-moi-{school.Token}",
                startDate = "2028-09-01",
                endDate = "2029-05-31",
                isCurrent = false,
            });

        var yearId = (await year.ReadJsonAsync()).GetProperty("id").GetInt32();

        var created = await admin.PostAsJsonAsync(
            $"/api/academic-years/{yearId}/terms",
            new
            {
                code = "HK1",
                name = "Học kỳ 1",
                startDate = "2028-09-01",
                endDate = "2028-12-31",
                ordinal = 1,
            });

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var termId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        (await admin.DeleteAsync($"/api/terms/{termId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The term of a year whose weeks are laid out stays.
        await admin.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 4 });

        var inUse = await admin.DeleteAsync($"/api/terms/{school.Hk1Id}");

        inUse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await inUse.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("TERM_IN_USE");
    }

    [Fact]
    public async Task A_class_with_students_is_kept_and_an_empty_one_can_go()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var inUse = await admin.DeleteAsync($"/api/classes/{school.Class91Id}");

        inUse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await inUse.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("CLASS_IN_USE");
        error.GetProperty("message").GetString().Should().Contain("học sinh");

        var created = await admin.PostAsJsonAsync(
            "/api/classes",
            new
            {
                gradeId = school.GradeId,
                yearId = school.YearId,
                code = $"9/9-{school.Token}",
                name = "Lớp tạo nhầm",
            });

        var classId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        (await admin.DeleteAsync($"/api/classes/{classId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.GetAsync($"/api/classes/{classId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reusing_the_code_of_a_deleted_class_is_reported_clearly()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var code = $"9/8-{school.Token}";

        var created = await admin.PostAsJsonAsync(
            "/api/classes",
            new { gradeId = school.GradeId, yearId = school.YearId, code, name = "Lớp tạo nhầm" });

        var classId = (await created.ReadJsonAsync()).GetProperty("id").GetInt32();

        await admin.DeleteAsync($"/api/classes/{classId}");

        var again = await admin.PostAsJsonAsync(
            "/api/classes",
            new { gradeId = school.GradeId, yearId = school.YearId, code, name = "Lớp mới" });

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await again.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("CODE_TAKEN_BY_DELETED");
        error.GetProperty("message").GetString().Should().Contain("đã xoá");
    }

    [Fact]
    public async Task A_grade_that_still_has_classes_cannot_be_deleted()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var admin = await _factory.SignInAsync(school.AdminUsername, TestSchool.Password);

        var response = await admin.DeleteAsync($"/api/grades/{school.GradeId}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("GRADE_IN_USE");
    }

    [Fact]
    public async Task Only_the_administrator_may_delete()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        (await teacher.DeleteAsync($"/api/classes/{school.Class91Id}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await teacher.DeleteAsync($"/api/academic-years/{school.YearId}"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
