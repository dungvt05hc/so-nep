using System.Net;

namespace NeNep.Api.Common;

/// <summary>
/// An expected failure that maps to a specific HTTP status.
/// <para>
/// <see cref="AppException.Message"/> is shown to the user, so it is written in
/// Vietnamese; <see cref="Code"/> is the stable machine-readable identifier the front
/// end branches on.
/// </para>
/// </summary>
public class AppException : Exception
{
    public AppException(HttpStatusCode status, string code, string message)
        : base(message)
    {
        Status = status;
        Code = code;
    }

    public HttpStatusCode Status { get; }

    public string Code { get; }

    /// <summary>Per-field messages, used by <see cref="AppValidationException"/>.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; protected init; }
}

/// <summary>The row exists but the caller may not touch it, or it simply is not there.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, "NOT_FOUND", message)
    {
    }
}

/// <summary>
/// The caller is authenticated but the operation is outside their role or their classes.
/// </summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message, string code = "FORBIDDEN")
        : base(HttpStatusCode.Forbidden, code, message)
    {
    }
}

/// <summary>The request contradicts the current state of the data.</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message, string code = "CONFLICT")
        : base(HttpStatusCode.Conflict, code, message)
    {
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message, string code = "UNAUTHORIZED")
        : base(HttpStatusCode.Unauthorized, code, message)
    {
    }
}

/// <summary>The request body failed validation.</summary>
public sealed class AppValidationException : AppException
{
    public AppValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base(HttpStatusCode.BadRequest, "VALIDATION_FAILED", "Dữ liệu gửi lên không hợp lệ.")
    {
        Errors = errors;
    }

    public AppValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }
}
