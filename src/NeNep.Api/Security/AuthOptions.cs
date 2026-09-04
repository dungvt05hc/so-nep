namespace NeNep.Api.Security;

/// <summary>
/// Sign-in settings, bound from the <c>Auth</c> section of configuration.
/// <para>
/// These are operational security settings, not numbers from the school regulation, so
/// configuration is the right home for them — the regulation's own numbers live in
/// <c>school_settings</c>, <c>classification_levels</c> and <c>conduct_rules</c>.
/// </para>
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "nenep";

    public string Audience { get; set; } = "nenep";

    /// <summary>HMAC signing key. At least 32 bytes; supplied per environment, never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Short-lived on purpose: a stolen phone stays useful for minutes, not days.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Lifetime of a <c>sessions</c> row, so a teacher is not asked to sign in daily.</summary>
    public int RefreshTokenDays { get; set; } = 30;

    /// <summary>Consecutive wrong passwords before the account is locked temporarily.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>How long that temporary lock lasts.</summary>
    public int LockMinutes { get; set; } = 15;

    /// <summary>Length of the temporary password handed out with a new or reset account.</summary>
    public int TemporaryPasswordLength { get; set; } = 10;

    /// <summary>Shortest password a user may choose for themselves.</summary>
    public int MinPasswordLength { get; set; } = 8;
}
