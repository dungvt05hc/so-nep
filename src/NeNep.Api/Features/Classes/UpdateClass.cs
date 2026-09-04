using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeNep.Api.Common;
using NeNep.Api.Security;
using NeNep.Infrastructure.Persistence;

namespace NeNep.Api.Features.Classes;

/// <summary>
/// Renames a class or changes who is in charge of it. Reassigning the homeroom teacher
/// takes effect at once: <see cref="IClassAccessGuard"/> reads this column, so the
/// previous teacher loses access on their very next request.
/// </summary>
public static class UpdateClass
{
    public sealed record Request(string Code, string Name, int? HomeroomTeacherId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Vui lòng nhập mã lớp.").MaximumLength(20);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Vui lòng nhập tên lớp.").MaximumLength(100);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder group) =>
        group.MapPut("/{classId:int}", HandleAsync)
            .RequireAuthorization(Policies.Admin)
            .WithValidation<Request>()
            .WithName("UpdateClass")
            .WithSummary("Sửa lớp");

    private static async Task<ClassResponse> HandleAsync(
        int classId,
        Request request,
        NeNepDbContext db,
        CancellationToken cancellationToken)
    {
        var entity = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy lớp.");

        var code = request.Code.Trim();

        await SoftDelete.EnsureFreeAsync(
            db.Classes.Where(c => c.YearId == entity.YearId && c.Code == code && c.Id != classId),
            $"Lớp \"{code}\" đã tồn tại trong năm học này.",
            $"Mã lớp \"{code}\" trùng với một lớp đã xoá của năm học này.",
            cancellationToken);

        if (request.HomeroomTeacherId is { } teacherId && teacherId != entity.HomeroomTeacherId)
        {
            await ClassQueries.EnsureIsHomeroomTeacherAsync(db, teacherId, cancellationToken);
        }

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.HomeroomTeacherId = request.HomeroomTeacherId;

        await db.SaveChangesAsync(cancellationToken);

        return await ClassQueries
            .Project(db.Classes.AsNoTracking().Where(c => c.Id == classId))
            .FirstAsync(cancellationToken);
    }
}
