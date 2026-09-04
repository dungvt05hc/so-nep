namespace NeNep.Api.Features.Students;

public sealed record StudentResponse(
    int Id,
    string Code,
    string FullName,
    DateOnly? Dob,
    string? Gender,
    string? Note,
    int EnrollmentId,
    int ClassId,
    string ClassCode,
    int? OrderNo,
    bool IsActive,
    DateOnly? LeftAt,
    bool HasAccount);

/// <summary>What the import will do with one row of the spreadsheet.</summary>
public enum ImportAction
{
    /// <summary>New student, new enrollment.</summary>
    CREATE,

    /// <summary>The student is already in this class this year; their details are refreshed.</summary>
    UPDATE,

    /// <summary>The student exists from an earlier year and is enrolled into this class.</summary>
    ENROLL,

    /// <summary>The row cannot be imported; see the messages.</summary>
    ERROR,
}

public sealed record ImportRowResponse(
    int RowNumber,
    string? Code,
    string? FullName,
    DateOnly? Dob,
    string? Gender,
    int? OrderNo,
    ImportAction Action,
    IReadOnlyList<string> Errors);

public sealed record ImportPreviewResponse(
    int ClassId,
    string ClassCode,
    int TotalRows,
    int WillCreate,
    int WillUpdate,
    int WillEnroll,
    int WithErrors,
    IReadOnlyList<ImportRowResponse> Rows);

public sealed record ImportResultResponse(
    int ClassId,
    string ClassCode,
    int Created,
    int Updated,
    int Enrolled,
    int Skipped,
    IReadOnlyList<ImportRowResponse> Rows);
