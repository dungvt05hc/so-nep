using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// A finalised score and classification. periodKey: "W:12" | "T:1" | "Y:1".
/// </summary>
public class ConductScore
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int ClassId { get; set; }

    public int YearId { get; set; }

    public PeriodScope Scope { get; set; }

    public string PeriodKey { get; set; } = string.Empty;

    public int? WeekId { get; set; }

    public int? TermId { get; set; }

    public decimal BaseScore { get; set; }

    public decimal BonusTotal { get; set; }

    public decimal PenaltyTotal { get; set; }

    /// <summary>
    /// The score before any cap or floor is applied. Per decisions Q1 and Q2 there is
    /// no cap and negatives are allowed, so <see cref="RawScore"/> and
    /// <see cref="FinalScore"/> are usually equal.
    /// </summary>
    public decimal RawScore { get; set; }

    /// <summary>The final score used for classification and ranking.</summary>
    public decimal FinalScore { get; set; }

    /// <summary>
    /// The denominator of a term score: the number of weeks that are LOCKED and have
    /// <c>isCounted = true</c> as of the moment of calculation. NOT the total number
    /// of weeks in the term.
    /// Dividing by 18 when viewing week 5 would put every student in "Chưa đạt".
    /// </summary>
    public int? CountedWeeks { get; set; }

    /// <summary>The level derived purely from the score — the one the app can compute itself.</summary>
    public int? LevelByScoreId { get; set; }

    /// <summary>
    /// How many levels the rules PROPOSE moving down. Display only; never applied to
    /// <see cref="FinalLevelId"/>.
    /// </summary>
    public int SuggestedDowngrades { get; set; }

    /// <summary>
    /// How many levels the rules PROPOSE moving up. Display only; never applied to
    /// <see cref="FinalLevelId"/>.
    /// </summary>
    public int SuggestedUpgrades { get; set; }

    /// <summary>
    /// The final level: equal to <see cref="LevelByScoreId"/> unless a
    /// <see cref="ConductAdjustment"/> came out of a school board / homeroom teacher /
    /// parent meeting.
    /// </summary>
    public int? FinalLevelId { get; set; }

    public int? RankInClass { get; set; }

    public int? ClassSize { get; set; }

    public DateTimeOffset ComputedAt { get; set; }

    public Student Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public AcademicYear Year { get; set; } = null!;
    public AcademicWeek? Week { get; set; }
    public Term? Term { get; set; }
    public ClassificationLevel? LevelByScore { get; set; }
    public ClassificationLevel? FinalLevel { get; set; }
}
