using Microsoft.AspNetCore.Diagnostics;

namespace NeNep.Api.Common;

/// <summary>The single error shape every endpoint returns.</summary>
/// <param name="Code">Stable identifier for the front end.</param>
/// <param name="Message">Message shown to the user, in Vietnamese.</param>
/// <param name="Errors">Per-field messages for a failed validation.</param>
public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>
/// Turns <see cref="AppException"/> into its HTTP status and hides the details of
/// anything unexpected — a stack trace must never reach a teacher's phone.
/// </summary>
public sealed class ApiErrorHandler : IExceptionHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;

    public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is AppException app)
        {
            httpContext.Response.StatusCode = (int)app.Status;

            await httpContext.Response.WriteAsJsonAsync(
                new ApiError(app.Code, app.Message, app.Errors),
                cancellationToken);

            return true;
        }

        // A body that is not valid JSON, or a form that cannot be read: the caller's
        // mistake, not the server's.
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;

            await httpContext.Response.WriteAsJsonAsync(
                new ApiError("MALFORMED_REQUEST", "Dữ liệu gửi lên không đọc được."),
                cancellationToken);

            return true;
        }

        _logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new ApiError("INTERNAL_ERROR", "Hệ thống gặp lỗi. Vui lòng thử lại."),
            cancellationToken);

        return true;
    }
}
