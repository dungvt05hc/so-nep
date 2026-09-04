using FluentValidation;

namespace NeNep.Api.Common;

/// <summary>
/// Runs the FluentValidation validator registered for <typeparamref name="TRequest"/>
/// before the handler sees the request.
/// </summary>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    private readonly IValidator<TRequest> _validator;

    public ValidationFilter(IValidator<TRequest> validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            throw new InvalidOperationException(
                $"No argument of type {typeof(TRequest).Name} was found on this endpoint.");
        }

        var result = await _validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            throw new AppValidationException(
                result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    /// <summary>Validates the request body of this endpoint before the handler runs.</summary>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>();
}
