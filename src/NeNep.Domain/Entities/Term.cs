namespace NeNep.Domain.Entities;

/// <summary>A term within an academic year.</summary>
public class Term
{
    public int Id { get; set; }

    public int YearId { get; set; }

    /// <summary>"HK1" | "HK2"</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int Ordinal { get; set; }

    public AcademicYear Year { get; set; } = null!;
    public ICollection<AcademicWeek> Weeks { get; set; } = new List<AcademicWeek>();
    public ICollection<ConductScore> Scores { get; set; } = new List<ConductScore>();
    public ICollection<ConductAlert> Alerts { get; set; } = new List<ConductAlert>();
    public ICollection<ConductAdjustment> Adjustments { get; set; } = new List<ConductAdjustment>();
}
