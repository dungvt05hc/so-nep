namespace NeNep.Domain.Enums;

/// <summary>How a conduct rule is triggered.</summary>
public enum RuleType
{
    /// <summary>A single occurrence is enough (N12, N13, H11).</summary>
    IMMEDIATE,

    /// <summary>A count within the period exceeds a threshold (N01 more than 10 times).</summary>
    THRESHOLD,

    /// <summary>Remediation was not completed in time (N08 after 3 days).</summary>
    REMEDIATION_TIMEOUT,

    /// <summary>Recorded manually by the homeroom teacher (B01, B02).</summary>
    MANUAL,
}
