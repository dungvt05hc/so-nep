using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NeNep.Domain.Entities;
using NeNep.Domain.Enums;

namespace NeNep.Api.Security;

/// <summary>A signed access token and the moment it stops being accepted.</summary>
public readonly record struct AccessToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// A refresh token as handed to the client, together with the hash stored in
/// <c>sessions.refresh_hash</c>. The raw value never touches the database.
/// </summary>
public readonly record struct RefreshToken(string Value, string Hash);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<Role> roles, string sessionId);

    /// <summary>Creates the refresh token for a session that already has an id.</summary>
    RefreshToken CreateRefreshToken(string sessionId);

    /// <summary>
    /// Splits a presented refresh token back into its session id and secret. Returns
    /// false for anything malformed, so a caller never has to guard the format itself.
    /// </summary>
    bool TryRead(string value, out string sessionId, out string secret);

    string HashSecret(string secret);
}

/// <summary>
/// JWT access tokens plus opaque refresh tokens.
/// <para>
/// A refresh token is <c>{sessionId}.{secret}</c>. Carrying the session id means the
/// server looks the session up by primary key instead of scanning the table for a hash,
/// and only the SHA-256 of the secret is stored — a leaked database backup cannot be
/// replayed.
/// </para>
/// </summary>
public sealed class TokenService : ITokenService
{
    private const int SecretBytes = 32;
    private const char Separator = '.';

    private readonly AuthOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _credentials;

    public TokenService(IOptions<AuthOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));

        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user, IReadOnlyCollection<Role> roles, string sessionId)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(TokenClaims.UserId, user.Id.ToString()),
            new(TokenClaims.SessionId, sessionId),
            new(TokenClaims.Username, user.Username),
            new(TokenClaims.FullName, user.FullName),
        };

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(TokenClaims.MustChangePassword, "1"));
        }

        claims.AddRange(roles.Distinct().Select(r => new Claim(TokenClaims.Role, r.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    public RefreshToken CreateRefreshToken(string sessionId)
    {
        var secret = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(SecretBytes));

        return new RefreshToken($"{sessionId}{Separator}{secret}", HashSecret(secret));
    }

    public bool TryRead(string value, out string sessionId, out string secret)
    {
        sessionId = string.Empty;
        secret = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separator = value.LastIndexOf(Separator);

        if (separator <= 0 || separator == value.Length - 1)
        {
            return false;
        }

        sessionId = value[..separator];
        secret = value[(separator + 1)..];

        return true;
    }

    public string HashSecret(string secret) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
