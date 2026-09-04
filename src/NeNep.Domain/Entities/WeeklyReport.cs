using System.Text.Json;
using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// A class's weekly report: its lock state and whether it has been published to parents.
/// </summary>
public class WeeklyReport
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int WeekId { get; set; }

    public WeekStatus Status { get; set; }

    public DateTimeOffset? LockedAt { get; set; }

    public int? LockedById { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int? PublishedById { get; set; }

    public string? UnlockReason { get; set; }

    public int UnlockCount { get; set; }

    /// <summary>Snapshot of the figures at the moment of publication.</summary>
    public JsonDocument? Snapshot { get; set; }

    public Class Class { get; set; } = null!;
    public AcademicWeek Week { get; set; } = null!;
}
