namespace NeNep.Domain.Enums;

/// <summary>State of an academic week; also used for a class's weekly report.</summary>
public enum WeekStatus
{
    /// <summary>Week in progress, records may be entered.</summary>
    OPEN,

    /// <summary>Week is over but records are still awaiting review, so it cannot be locked.</summary>
    PENDING_REVIEW,

    /// <summary>Locked: scores computed, editing closed.</summary>
    LOCKED,

    /// <summary>Published to parents.</summary>
    PUBLISHED,
}
