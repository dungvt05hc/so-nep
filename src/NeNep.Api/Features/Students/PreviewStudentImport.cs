using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Students;

/// <summary>
/// Shows what an uploaded student list would do, line by line, without writing anything.
/// The import is always a two-step move: look, then commit.
/// </summary>
public static class PreviewStudentImport
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder classGroup) =>
        classGroup.MapPost("/{classId:int}/students/import/preview", HandleAsync)
            .RequireAuthorization(Policies.ManageClass)
            .DisableAntiforgery()
            .WithName("PreviewStudentImport")
            .WithSummary("Xem trước danh sách học sinh nhập từ Excel");

    private static async Task<ImportPreviewResponse> HandleAsync(
        int classId,
        IFormFile file,
        NeNepDbContext db,
        IClassAccessGuard guard,
        CancellationToken cancellationToken)
    {
        await guard.EnsureCanWriteAsync(classId, cancellationToken);

        var target = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        StudentImportEndpoints.EnsureAcceptableFile(file);

        await using var stream = file.OpenReadStream();

        var rows = StudentImportFile.Read(stream);
        var planned = await StudentImportPlanner.PlanAsync(db, target, rows, cancellationToken);

        return new ImportPreviewResponse(
            target.Id,
            target.Code,
            planned.Count,
            planned.Count(p => p.Action == ImportAction.CREATE),
            planned.Count(p => p.Action == ImportAction.UPDATE),
            planned.Count(p => p.Action == ImportAction.ENROLL),
            planned.Count(p => p.Action == ImportAction.ERROR),
            [.. planned.Select(p => p.ToResponse())]);
    }
}
