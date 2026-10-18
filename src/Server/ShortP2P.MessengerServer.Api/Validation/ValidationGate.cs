using FluentValidation;
using ShortP2P.MessengerServer.UseCases;

namespace ShortP2P.MessengerServer.Api.Validation;

/// <summary>Validates non-body arguments (route/query) via FluentValidation.</summary>
public static class ValidationGate
{
    public static async Task EnsureValidAsync<T>(
        IServiceProvider services,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var validator = services.GetService<IValidator<T>>();
        if (validator is null)
            return;

        var result = await validator.ValidateAsync(instance, cancellationToken).ConfigureAwait(false);
        if (!result.IsValid)
            throw UseCaseException.Validation(result.Errors[0].ErrorMessage);
    }
}
