using NeNep.Api.Features.SchoolBreaks;
using NeNep.Api.Features.Terms;
using NeNep.Api.Features.Weeks;

namespace NeNep.Api.Features.AcademicYears;

public static class AcademicYearEndpoints
{
    /// <summary>
    /// The school calendar: years, and everything that hangs off a year — terms, weeks
    /// and breaks.
    /// </summary>
    public static IEndpointRouteBuilder MapAcademicYearEndpoints(this IEndpointRouteBuilder app)
    {
        var years = app.MapGroup("/api/academic-years").WithTags("AcademicYears");

        CreateAcademicYear.Map(years);
        ListAcademicYears.Map(years);
        UpdateAcademicYear.Map(years);
        SetCurrentAcademicYear.Map(years);
        DeleteAcademicYear.Map(years);

        CreateTerm.Map(years);

        GenerateWeeks.Map(years);
        ListWeeks.Map(years);

        CreateSchoolBreak.Map(years);
        ListSchoolBreaks.Map(years);

        var terms = app.MapGroup("/api/terms").WithTags("AcademicYears");

        UpdateTerm.Map(terms);
        DeleteTerm.Map(terms);

        var weeks = app.MapGroup("/api/weeks").WithTags("AcademicYears");

        UpdateWeek.Map(weeks);

        var breaks = app.MapGroup("/api/breaks").WithTags("AcademicYears");

        ApplySchoolBreak.Map(breaks);

        return app;
    }
}
