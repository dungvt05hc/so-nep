namespace NeNep.Domain.Enums;

/// <summary>Remediation state of a violation record that requires follow-up work.</summary>
public enum RemediationStatus
{
    NOT_REQUIRED,
    PENDING,
    DONE,
    OVERDUE,
}
