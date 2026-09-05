using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Domain.Enums;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.AuditLogs;

/// <summary>
/// The change log screen: who changed what, when, from which address.
/// <para>
/// The trail is filtered by the SAME class scope as the data it describes. The school
/// board reads the whole school; a homeroom teacher reads their own classes, which means
/// rows carrying no class id (accounts, the school calendar, the catalog) are not theirs
/// to read.
/// </para>
/// </summary>
public static class ListAuditLogs
{
    private const int MaxPageSize = 200;

    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-logs", HandleAsync)
            .RequireAuthorization()
            .WithTags("AuditLogs")
            .WithName("ListAuditLogs")
            .WithSummary("Nhật ký thay đổi");

        return app;
    }

    private static async Task<AuditLogPageResponse> HandleAsync(
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken,
        string? entity = null,
        string? entityId = null,
        AuditAction? action = null,
        int? actorId = null,
        int? classId = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int page = 1,
        int pageSize = 50)
    {
        var scope = await guard.GetScopeAsync(cancellationToken);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        if (classId is not null)
        {
            await guard.EnsureCanReadAsync(classId.Value, cancellationToken);
        }

        var query = db.AuditLogs.AsNoTracking();

        if (!scope.SchoolWideRead)
        {
            var readable = scope.ReadFilter() ?? [];

            if (readable.Length == 0)
            {
                throw new ForbiddenException("Bạn không có quyền xem nhật ký thay đổi.");
            }

            query = query.Where(l => l.ClassId != null && readable.Contains(l.ClassId.Value));
        }

        if (classId is not null)
        {
            query = query.Where(l => l.ClassId == classId);
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            query = query.Where(l => l.Entity == entity);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(l => l.EntityId == entityId);
        }

        if (action is not null)
        {
            query = query.Where(l => l.Action == action);
        }

        if (actorId is not null)
        {
            query = query.Where(l => l.ActorId == actorId);
        }

        if (from is not null)
        {
            query = query.Where(l => l.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(l => l.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogResponse(
                l.Id,
                l.ActorId,
                l.Actor != null ? l.Actor.FullName : null,
                l.ActorRole,
                l.Action,
                l.Entity,
                l.EntityId,
                l.ClassId,
                l.Class != null ? l.Class.Code : null,
                l.Summary,
                l.BeforeJson,
                l.AfterJson,
                l.Ip,
                l.UserAgent,
                l.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AuditLogPageResponse(items, total, page, pageSize);
    }
}
