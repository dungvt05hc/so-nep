using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using NeNep.Api.Tests.Infrastructure;

namespace NeNep.Api.Tests;

[Collection(ApiCollection.Name)]
public class AuthenticationTests
{
    private readonly ApiFactory _factory;

    public AuthenticationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_temporarily()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = _factory.CreateClient();

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var wrong = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = school.Teacher91Username, password = "sai-mat-khau" });

            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // The right password is now refused too, because the account is locked.
        var locked = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = school.Teacher91Username, password = TestSchool.Password });

        locked.StatusCode.Should().Be(HttpStatusCode.Locked);

        var error = await locked.ReadJsonAsync();

        error.GetProperty("code").GetString().Should().Be("ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task An_unknown_username_is_answered_exactly_like_a_wrong_password()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = _factory.CreateClient();

        var unknown = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = $"khong-ton-tai-{school.Token}", password = TestSchool.Password });

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = school.Teacher91Username, password = "sai-mat-khau" });

        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await unknown.ReadJsonAsync()).GetProperty("code").GetString()
            .Should().Be((await wrongPassword.ReadJsonAsync()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_new_account_has_to_change_its_password_before_doing_anything_else()
    {
        var school = await TestSchool.SeedAsync(_factory);

        await TestSchool.AddOfficerAccountAsync(
            _factory,
            school.Class91Id,
            school.Student91Id,
            $"cb-moi-{school.Token}",
            mustChangePassword: true);

        var client = _factory.CreateClient();
        var login = await client.LoginAsync($"cb-moi-{school.Token}", TestSchool.Password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var blocked = await client.GetAsync("/api/classes");

        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await blocked.ReadJsonAsync()).GetProperty("code").GetString().Should().Be("MUST_CHANGE_PASSWORD");

        var changed = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = TestSchool.Password, newPassword = "MatKhauMoi@456" });

        changed.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await changed.ReadJsonAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            tokens.GetProperty("accessToken").GetString());

        (await client.GetAsync("/api/classes")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refreshing_rotates_the_session_and_burns_the_old_refresh_token()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = _factory.CreateClient();

        var login = await client.LoginAsync(school.Teacher91Username, TestSchool.Password);

        var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var reused = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_access_token_stops_working_once_its_fifteen_minutes_are_over()
    {
        var school = await TestSchool.SeedAsync(_factory);
        var client = await _factory.SignInAsync(school.Teacher91Username, TestSchool.Password);

        (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.Time.Advance(TimeSpan.FromMinutes(16));

        try
        {
            (await client.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            // The clock is shared by every test in the collection.
            _factory.Time.Advance(TimeSpan.FromMinutes(-16));
        }
    }
}
