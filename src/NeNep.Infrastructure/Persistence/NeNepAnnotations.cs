namespace NeNep.Infrastructure.Persistence;

/// <summary>Internal annotations that the interceptors read off the model metadata.</summary>
public static class NeNepAnnotations
{
    /// <summary>
    /// Marks a property that plays the role of Prisma's <c>@updatedAt</c>.
    /// EF Core has no built-in equivalent, so <c>TimestampInterceptor</c> handles it.
    /// </summary>
    public const string UpdatedAt = "NeNep:UpdatedAt";
}
