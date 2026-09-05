namespace NeNep.Api.Features.Catalog;

public static class CatalogEndpoints
{
    /// <summary>
    /// The school-wide catalog. Everything that depends on a class hangs off
    /// <c>/api/classes/{classId}</c> instead, where the guard cannot be forgotten.
    /// </summary>
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog").WithTags("Catalog");

        ListViolationTypes.Map(catalog);

        return app;
    }
}
