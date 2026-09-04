using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NeNep.Api.Tests.Infrastructure;

public sealed record LoginResult(string AccessToken, string RefreshToken);

public static class ApiClientExtensions
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Signs in and returns a client that carries the access token.</summary>
    public static async Task<HttpClient> SignInAsync(this ApiFactory factory, string username, string password)
    {
        var client = factory.CreateClient();
        var login = await client.LoginAsync(username, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        return client;
    }

    public static async Task<LoginResult> LoginAsync(this HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return new LoginResult(
            body.GetProperty("accessToken").GetString()!,
            body.GetProperty("refreshToken").GetString()!);
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
