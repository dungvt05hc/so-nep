using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// An ALERT raised by the rule engine. The app does NOT downgrade conduct itself
/// (decisions Q3/Q4) — this table only says "this student has reached a threshold
/// and needs to be brought to a meeting".
/// </summary>
public class ConductAlert
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public int TermId { get; set; }

    public int RuleId { get; set; }

    public string RuleCodeSnapshot { get; set; } = string.Empty;

    public string RuleNameSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// The count observed at detection time, so the UI can show "9/10 times" without
    /// having to trace the rule back.
    /// </summary>
    public int TriggerCount { get; set; }

    /// <summary>
    /// The rule threshold at detection time, so the UI can show "9/10 times" without
    /// having to trace the rule back.
    /// </summary>
    public int? ThresholdValue { get; set; }

    /// <summary>What the rule proposes — for reference during the meeting only.</summary>
    public RuleEffect SuggestedEffect { get; set; }

    /// <summary>How many levels the rule proposes — for reference during the meeting only.</summary>
    public int SuggestedLevels { get; set; }

    /// <summary>Ids of the violation records used as evidence.</summary>
    public List<int> EvidenceRecordIds { get; set; } = new();

    public AlertStatus Status { get; set; }

    public DateTimeOffset FirstDetectedAt { get; set; }

    /// <summary>Set by <c>TimestampInterceptor</c>, the equivalent of Prisma's <c>@updatedAt</c>.</summary>
    public DateTimeOffset LastCheckedAt { get; set; }

    public int? AcknowledgedById { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolutionNote { get; set; }

    public int? AdjustmentId { get; set; }

    public Student Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public ConductRule Rule { get; set; } = null!;
    public ConductAdjustment? Adjustment { get; set; }
}
