namespace NeNep.Domain.Entities;

/// <summary>A login session, used to revoke refresh tokens.</summary>
public class Session
{
    public string Id { get; set; } = string.Empty;

    public int UserId { get; set; }

    public string RefreshHash { get; set; } = string.Empty;

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
