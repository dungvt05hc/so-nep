using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Tests.Infrastructure;
using NeNep.Domain.Enums;

namespace NeNep.Api.Tests;

/// <summary>
/// A homeroom teacher handing out and taking back class-officer accounts, and appointing
/// officers for a period.
/// </summary>
[Collection(ApiCollection.Name)]
public class ClassAccountTests
{
    private readonly ApiFactory _factory;

    public ClassAccountTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Granting_an_account_returns_the_temporary_password_once_and_forces_a_change()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/accounts",
            new { studentId = school.Student91Id, role = nameof(Role.LOP_TRUONG) });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.ReadJsonAsync();
        var temporary = body.GetProperty("temporaryPassword").GetString()!;
        var username = body.GetProperty("account").GetProperty("username").GetString()!;

        body.GetProperty("account").GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();

        var client = _factory.CreateClient();
        var login = await client.LoginAsync(username, temporary);

        login.AccessToken.Should().NotBeNullOrEmpty();

        // The audit trail records the grant, and never the password itself.
        await using var db = _factory.CreateDbContext();

        var log = await db.AuditLogs
            .Where(l => l.Action == AuditAction.GRANT_ACCOUNT && l.Entity == "users")
            .OrderByDescending(l => l.Id)
            .FirstAsync();

        log.Summary.Should().Contain(username);
        log.AfterJson!.RootElement.GetProperty("PasswordHash").GetString().Should().Be("***");
    }

    [Fact]
    public async Task A_student_cannot_be_given_two_accounts()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var body = new { studentId = school.Student91Id, role = nameof(Role.PHO_HOC_TAP) };

        (await teacher.PostAsJsonAsync($"/api/classes/{school.Class91Id}/accounts", body))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await teacher.PostAsJsonAsync($"/api/classes/{school.Class91Id}/accounts", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("STUDENT_HAS_ACCOUNT");
    }

    [Fact]
    public async Task An_account_may_only_be_granted_to_a_student_of_the_class()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var response = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/accounts",
            new { studentId = school.Student92Id, role = nameof(Role.LOP_TRUONG) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_officer_appointment_is_dated_and_cannot_overlap_another()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var first = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/officers",
            new
            {
                studentId = school.Student91Id,
                role = nameof(Role.LOP_TRUONG),
                validFrom = "2026-09-07",
            });

        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var officerId = (await first.ReadJsonAsync()).GetProperty("id").GetInt32();

        var overlapping = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/officers",
            new
            {
                studentId = school.Student91Id,
                role = nameof(Role.LOP_TRUONG),
                validFrom = "2026-10-01",
            });

        overlapping.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await overlapping.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("OFFICER_ROLE_TAKEN");

        var ended = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/officers/{officerId}/end",
            new { validTo = "2026-09-30" });

        ended.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ended.ReadJsonAsync()).GetProperty("validTo").GetString().Should().Be("2026-09-30");

        var afterHandover = await teacher.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/officers",
            new
            {
                studentId = school.Student91Id,
                role = nameof(Role.LOP_TRUONG),
                validFrom = "2026-10-01",
            });

        afterHandover.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_class_officer_cannot_administer_anything()
    {
        var school = await TestSchool.SeedAsync(_factory);

        await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb-quyen-{school.Token}");

        var officer = await _factory.SignInAsync($"cb-quyen-{school.Token}", TestSchool.Password);

        // Their own class register is readable.
        (await officer.GetAsync($"/api/classes/{school.Class91Id}/students"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Handing out accounts is not.
        var granting = await officer.PostAsJsonAsync(
            $"/api/classes/{school.Class91Id}/accounts",
            new { studentId = school.Student91Id, role = nameof(Role.PHO_LAO_DONG) });

        granting.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Neither is the school calendar.
        var calendar = await officer.PostAsJsonAsync(
            $"/api/academic-years/{school.YearId}/weeks/generate",
            new { firstSchoolDay = "2026-09-05", weekCount = 35 });

        calendar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
