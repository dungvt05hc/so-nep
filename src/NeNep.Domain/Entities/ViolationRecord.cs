using NeNep.Domain.Abstractions;
using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// A violation or commendation record — the central table of the system.
/// Only <see cref="ViolationStatus.APPROVED"/> records ever count towards a score.
/// </summary>
public class ViolationRecord : ISoftDeletable
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int StudentId { get; set; }

    public int WeekId { get; set; }

    public int TypeId { get; set; }

    // --- IMMUTABLE SNAPSHOT ---
    // Editing the scoring catalog must NEVER change a report already published to parents.

    /// <summary>Code as it was when the record was created. Unaffected by later catalog edits.</summary>
    public string TypeCodeSnapshot { get; set; } = string.Empty;

    /// <summary>Name as it was when the record was created. Unaffected by later catalog edits.</summary>
    public string TypeNameSnapshot { get; set; } = string.Empty;

    /// <summary>Signed points as they were when the record was created. Unaffected by later edits.</summary>
    public int PointsSnapshot { get; set; }

    public int Quantity { get; set; }

    public DateOnly OccurredDate { get; set; }

    /// <summary>Class period.</summary>
    public int? PeriodNo { get; set; }

    public string? Note { get; set; }

    public ViolationStatus Status { get; set; }

    public string? RejectReason { get; set; }

    /// <summary>Reserved for bulk entry. DECISION Q5: currently unused.</summary>
    public bool IsBulk { get; set; }

    public string? BulkGroupId { get; set; }

    /// <summary>
    /// One class officer recorded this against another class officer, so the homeroom
    /// teacher should look closely when reviewing.
    /// </summary>
    public bool IsFlagged { get; set; }

    // --- Remediation ---

    public RemediationStatus RemediationStatus { get; set; }

    public string? RemediationNote { get; set; }

    public DateOnly? RemediationDeadline { get; set; }

    public int? RemediationConfirmedById { get; set; }

    public DateTimeOffset? RemediationConfirmedAt { get; set; }

    public int ReportedById { get; set; }

    public DateTimeOffset ReportedAt { get; set; }

    public int? ReviewedById { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public bool IsLocked { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public Class Class { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public AcademicWeek Week { get; set; } = null!;
    public ViolationType Type { get; set; } = null!;
    public User ReportedBy { get; set; } = null!;
    public User? ReviewedBy { get; set; }
    public User? RemediationConfirmedBy { get; set; }
}
