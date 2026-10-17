using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Filters;

/// <summary>
/// Rejects unexpected Content-Type on JSON API bodies (415). Blob octet-stream endpoints are skipped.
/// </summary>
public sealed class JsonBodyContentTypeFilter : IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsPost(request.Method)
            && !HttpMethods.IsPut(request.Method)
            && !HttpMethods.IsPatch(request.Method))
        {
            return;
        }

        var path = request.Path.Value ?? "";
        if (path.Contains("/blobs", StringComparison.OrdinalIgnoreCase))
            return;

        if (!request.ContentLength.HasValue || request.ContentLength.Value == 0)
            return;

        var contentType = request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
            return;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        if (mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase))
            return;

        context.Result = new ObjectResult(new ApiError
        {
            Code = "UnsupportedMediaType",
            Message = "Content-Type must be application/json."
        })
        {
            StatusCode = StatusCodes.Status415UnsupportedMediaType
        };
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
