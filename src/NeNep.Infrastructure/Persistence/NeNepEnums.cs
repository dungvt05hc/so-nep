using NeNep.Domain.Enums;

namespace NeNep.Infrastructure.Persistence;

/// <summary>
/// The 11 enums from <c>docs/schema.prisma</c> that must be registered as PostgreSQL
/// enum types.
/// <para>
/// Each one has to be declared in THREE places. Missing any of them fails in a way
/// that is very hard to spot:
/// </para>
/// <list type="number">
///   <item><c>modelBuilder.HasPostgresEnum&lt;T&gt;()</c> so migrations emit <c>CREATE TYPE</c>.</item>
///   <item><c>NpgsqlDbContextOptionsBuilder.MapEnum()</c> so EF maps the property to the
///     right column type; without it the column silently falls back to <c>integer</c>
///     and stores the DECLARATION ORDER instead of the label.</item>
///   <item><c>NpgsqlDataSourceBuilder.MapEnum()</c> so the ADO.NET layer can read and
///     write the value.</item>
/// </list>
/// <para>
/// This list is the single source for both <c>MapEnum</c> calls.
/// </para>
/// </summary>
public static class NeNepEnums
{
    public static readonly IReadOnlyList<Type> All =
    [
        typeof(Role),
        typeof(CategoryKind),
        typeof(ViolationStatus),
        typeof(RemediationStatus),
        typeof(WeekStatus),
        typeof(RuleType),
        typeof(CompareOp),
        typeof(RuleEffect),
        typeof(AlertStatus),
        typeof(PeriodScope),
        typeof(AuditAction),
    ];
}
