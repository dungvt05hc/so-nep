using NeNep.Domain.Enums;

namespace NeNep.Domain.Entities;

/// <summary>
/// A class-officer appointment with a validity period, so officers can change mid-term.
/// </summary>
public class ClassOfficer
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int StudentId { get; set; }

    /// <summary>LOP_TRUONG | PHO_HOC_TAP | PHO_LAO_DONG</summary>
    public Role Role { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly? ValidTo { get; set; }

    public Class Class { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
