namespace NeNep.Domain.Enums;

/// <summary>What a rule PROPOSES when it fires.</summary>
public enum RuleEffect
{
    /// <summary>
    /// PROPOSES a downgrade. The app never downgrades on its own; it only raises
    /// the matter for the review meeting.
    /// </summary>
    DOWNGRADE,

    /// <summary>PROPOSES an upgrade.</summary>
    UPGRADE,

    /// <summary>Warning only, unrelated to classification (N04 over 45 sessions a year).</summary>
    WARNING,
}
