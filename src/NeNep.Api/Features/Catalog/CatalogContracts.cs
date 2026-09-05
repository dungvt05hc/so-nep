using NeNep.Domain.Enums;

namespace NeNep.Api.Features.Catalog;

/// <summary>One code of the school-wide catalog.</summary>
public sealed record ViolationTypeResponse(
    int Id,
    string Code,
    CategoryKind Kind,
    string CategoryName,
    string Name,
    int Points,
    bool CountsForScore,
    IReadOnlyList<Role> AllowedRoles,
    bool IsAutoComputed,
    int? MaxPerWeek,
    bool RequiresRemediation,
    string? RemediationNote,
    int? RemediationDays,
    int Ordinal,
    bool IsActive,
    string? Note);

/// <summary>
/// One code as it applies TO ONE CLASS: the school catalog with the homeroom teacher's
/// adjustments already worked in, which is what the quick-entry screen shows.
/// </summary>
/// <param name="EffectivePoints">
/// The points a record created now would snapshot. Equal to <c>Points</c> unless the
/// school has switched on <c>AllowClassPointOverride</c> and the teacher set a value.
/// </param>
/// <param name="IsEnabled">false = this class does not use the code at all.</param>
/// <param name="PointsOverride">The stored adjustment, whether or not it is in effect.</param>
public sealed record ClassViolationTypeResponse(
    int Id,
    string Code,
    CategoryKind Kind,
    string CategoryName,
    string Name,
    int Points,
    int EffectivePoints,
    bool CountsForScore,
    IReadOnlyList<Role> AllowedRoles,
    bool IsAutoComputed,
    int? MaxPerWeek,
    bool RequiresRemediation,
    string? RemediationNote,
    int? RemediationDays,
    int Ordinal,
    bool IsEnabled,
    int? PointsOverride,
    string? OverrideNote,
    string? Note);
