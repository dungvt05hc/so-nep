namespace NeNep.Domain.Enums;

/// <summary>Where an alert stands in the school's handling process.</summary>
public enum AlertStatus
{
    /// <summary>Close to the threshold — an early nudge while there is still time to correct course.</summary>
    APPROACHING,

    /// <summary>Threshold crossed, waiting for the homeroom teacher to pick it up.</summary>
    TRIGGERED,

    /// <summary>Seen by the homeroom teacher.</summary>
    ACKNOWLEDGED,

    /// <summary>Scheduled for a school board / homeroom teacher / parent meeting.</summary>
    IN_MEETING,

    /// <summary>The meeting took place and reached a conclusion.</summary>
    RESOLVED,

    /// <summary>Dismissed, with a recorded reason.</summary>
    DISMISSED,
}
