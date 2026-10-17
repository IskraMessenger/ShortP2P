using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases;

namespace ShortP2P.MessengerServer.Api.Controllers;

internal static class UseCaseExceptionActionResultExtensions
{
    public static IActionResult ToApiErrorResult(this ControllerBase controller, UseCaseException ex)
    {
        if (ex.RetryAfterSeconds is int retryAfter and > 0)
        {
            controller.Response.Headers.RetryAfter =
                retryAfter.ToString(CultureInfo.InvariantCulture);
        }

        return controller.StatusCode(
            ex.Code switch
            {
                "Validation" => StatusCodes.Status400BadRequest,
                "Unauthorized" => StatusCodes.Status401Unauthorized,
                "Forbidden" => StatusCodes.Status403Forbidden,
                "NotFound" => StatusCodes.Status404NotFound,
                "Conflict" => StatusCodes.Status409Conflict,
                "PeerOffline" => StatusCodes.Status409Conflict,
                "PayloadTooLarge" => StatusCodes.Status413PayloadTooLarge,
                "TooManyRequests" => StatusCodes.Status429TooManyRequests,
                "Unavailable" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            },
            new ApiError { Code = ex.Code, Message = ex.Message });
    }
}

