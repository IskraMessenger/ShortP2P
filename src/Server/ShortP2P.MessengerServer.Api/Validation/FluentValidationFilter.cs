using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation;

/// <summary>
/// Validates action arguments that have a registered <see cref="IValidator{T}"/>.
/// Failures return the same <see cref="ApiError"/> shape as use-case Validation errors.
/// </summary>
public sealed class FluentValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator
                .ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted)
                .ConfigureAwait(false);

            if (result.IsValid)
                continue;

            var message = result.Errors[0].ErrorMessage;
            context.Result = new ObjectResult(new ApiError
            {
                Code = "Validation",
                Message = message
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            return;
        }

        await next().ConfigureAwait(false);
    }
}
