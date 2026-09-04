namespace NeNep.Domain.Entities;

/// <summary>
/// DECISION Q7: a homeroom teacher tailoring the standard catalog for their own class.
/// The standard catalog lives in <see cref="ViolationType"/>; this table records only
/// the DIFFERENCES.
/// </summary>
public class ClassViolationOverride
{
    public int Id { get; set; }

    public int ClassId { get; set; }

    public int TypeId { get; set; }

    /// <summary>false = this class does not use that code.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Only usable when <see cref="SchoolSetting.AllowClassPointOverride"/> is true.
    /// </summary>
    public int? PointsOverride { get; set; }

    public string? Note { get; set; }

    public int CreatedById { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Set by <c>TimestampInterceptor</c>, the equivalent of Prisma's <c>@updatedAt</c>.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    public Class Class { get; set; } = null!;
    public ViolationType Type { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
