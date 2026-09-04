namespace NeNep.Api.Features.Students;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").WithTags("Students");

        StudentImportEndpoints.MapTemplate(group);

        return app;
    }
}
