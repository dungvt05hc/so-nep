namespace NeNep.Domain.Entities;

/// <summary>
/// A 6-digit code issued to a parent. Always paired with a long token in the URL.
/// </summary>
public class ParentAccessCode
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    /// <summary>32+ random characters, carried in the URL.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>bcrypt hash of the 6-digit code — the plain code is NEVER stored.</summary>
    public string CodeHash { get; set; } = string.Empty;

    public int IssuedById { get; set; }

    public DateTimeOffset IssuedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public int FailedAttempts { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public int ViewCount { get; set; }

    public Student Student { get; set; } = null!;
    public User IssuedBy { get; set; } = null!;
}
