namespace NeNep.Domain.Enums;

/// <summary>
/// Role of an account. One person may hold several roles at once (see <c>UserRole</c>);
/// a class-officer role is always tied to a specific class.
/// <para>
/// The member names are the PostgreSQL enum labels and are stored verbatim, so they
/// must never be renamed or reordered.
/// </para>
/// </summary>
public enum Role
{
    /// <summary>School administrator.</summary>
    ADMIN,

    /// <summary>School board — read-only across the whole school.</summary>
    BGH,

    /// <summary>Homeroom teacher.</summary>
    GVCN,

    /// <summary>Class monitor.</summary>
    LOP_TRUONG,

    /// <summary>Deputy for academics.</summary>
    PHO_HOC_TAP,

    /// <summary>Deputy for housekeeping duties.</summary>
    PHO_LAO_DONG,
}
