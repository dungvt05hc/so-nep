namespace NeNep.Domain.Enums;

/// <summary>
/// Review status of a violation record.
/// Only <see cref="APPROVED"/> records ever count towards a score.
/// </summary>
public enum ViolationStatus
{
    /// <summary>Just entered by a class officer, waiting for the homeroom teacher.</summary>
    PENDING,

    /// <summary>Approved — counts towards the score.</summary>
    APPROVED,

    /// <summary>Rejected by the homeroom teacher; a reason is mandatory.</summary>
    REJECTED,

    /// <summary>Review deadline passed; discarded when the week is locked.</summary>
    EXPIRED,
}
