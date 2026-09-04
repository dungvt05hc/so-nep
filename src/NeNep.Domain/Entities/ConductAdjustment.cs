namespace NeNep.Domain.Entities;

/// <summary>
/// The DECISION to adjust a classification after a school board / homeroom teacher /
/// parent meeting. This is the only thing allowed to differ from the result computed
/// from the score.
/// </summary>
public class ConductAdjustment
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int TermId { get; set; }

    public int LevelBeforeId { get; set; }

    public int LevelAfterId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateOnly MeetingDate { get; set; }

    /// <summary>"BGH, GVCN, phụ huynh em A" — recorded as written, stays in Vietnamese.</summary>
    public string? Attendees { get; set; }

    /// <summary>Reference number of the meeting minutes.</summary>
    public string? MinutesRef { get; set; }

    public int DecidedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public ClassificationLevel LevelBefore { get; set; } = null!;
    public ClassificationLevel LevelAfter { get; set; } = null!;
    public User DecidedBy { get; set; } = null!;
    public ICollection<ConductAlert> Alerts { get; set; } = new List<ConductAlert>();
}
