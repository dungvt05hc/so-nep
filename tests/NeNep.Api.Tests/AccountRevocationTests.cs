using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

/// <summary>
/// The second acceptance test of Phase 1: withdrawing an account has to take effect at
/// once, not when the fifteen-minute access token happens to run out.
/// </summary>
[Collection(ApiCollection.Name)]
public class AccountRevocationTests
{
    private readonly ApiFactory _factory;

    public AccountRevocationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Revoking_an_account_stops_the_access_token_it_is_already_holding()
    {
        var school = await TestSchool.SeedAsync(_factory);

        var officerId = await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb91-{school.Token}");

        var officer = await _factory.SignInAsync($"cb91-{school.Token}", TestSchool.Password);

        (await officer.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var revoked = await teacher.DeleteAsync($"/api/classes/{school.Class91Id}/accounts/{officerId}");

        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Same token, same client, no waiting.
        var afterRevoke = await officer.GetAsync("/api/auth/me");

        afterRevoke.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Locking_an_account_stops_the_access_token_it_is_already_holding()
    {
        var school = await TestSchool.SeedAsync(_factory);

        var officerId = await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb91b-{school.Token}");

        var officer = await _factory.SignInAsync($"cb91b-{school.Token}", TestSchool.Password);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var locked = await teacher.PostAsync(
            $"/api/classes/{school.Class91Id}/accounts/{officerId}/lock",
            content: null);

        locked.StatusCode.Should().Be(HttpStatusCode.OK);

        (await officer.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_revoked_account_can_no_longer_refresh_its_session()
    {
        var school = await TestSchool.SeedAsync(_factory);

        var officerId = await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb91c-{school.Token}");

        var anonymous = _factory.CreateClient();
        var login = await anonymous.LoginAsync($"cb91c-{school.Token}", TestSchool.Password);

        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        await teacher.DeleteAsync($"/api/classes/{school.Class91Id}/accounts/{officerId}");

        var refresh = await anonymous.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = login.RefreshToken });

        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resetting_a_password_signs_the_student_out_everywhere()
    {
        var school = await TestSchool.SeedAsync(_factory);

        var officerId = await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb91d-{school.Token}");

        var officer = await _factory.SignInAsync($"cb91d-{school.Token}", TestSchool.Password);
        var teacher = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        var reset = await teacher.PostAsync(
            $"/api/classes/{school.Class91Id}/accounts/{officerId}/reset-password",
            content: null);

        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await reset.ReadJsonAsync();
        var temporary = payload.GetProperty("temporaryPassword").GetString()!;

        (await officer.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The new password works, and lands the student on the change-password screen.
        var anonymous = _factory.CreateClient();
        var login = await anonymous.LoginAsync($"cb91d-{school.Token}", temporary);

        anonymous.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var blocked = await anonymous.GetAsync("/api/classes");

        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var error = await blocked.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("MUST_CHANGE_PASSWORD");
    }
}
