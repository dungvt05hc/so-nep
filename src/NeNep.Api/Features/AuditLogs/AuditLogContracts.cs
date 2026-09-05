using System.Text.Json;
using NeNep.Domain.Enums;

namespace NeNep.Api.Features.AuditLogs;

/// <summary>
/// One line of the change log.
/// <para>
/// <c>Before</c> and <c>After</c> hold only the columns that actually changed, with
/// password and token hashes redacted by the interceptor before they are ever written.
/// </para>
/// </summary>
public sealed record AuditLogResponse(
    long Id,
    int? ActorId,
    string? ActorName,
    Role? ActorRole,
    AuditAction Action,
    string Entity,
    string? EntityId,
    int? ClassId,
    string? ClassCode,
    string? Summary,
    JsonDocument? Before,
    JsonDocument? After,
    string? Ip,
    string? UserAgent,
    DateTimeOffset CreatedAt);

public sealed record AuditLogPageResponse(
    IReadOnlyList<AuditLogResponse> Items,
    int Total,
    int Page,
    int PageSize);
