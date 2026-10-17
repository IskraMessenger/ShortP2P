using Microsoft.AspNetCore.Http;
using ShortP2P.MessengerServer.Api.Filters;

namespace ShortP2P.MessengerServer.Tests.Api;

public class JsonBodyContentTypeFilterTests
{
    [Fact]
    public void HasRequestBody_WhenContentLengthPositive_IsTrue()
    {
        var request = new DefaultHttpContext().Request;
        request.ContentLength = 12;
        Assert.True(JsonBodyContentTypeFilter.HasRequestBody(request));
    }

    [Fact]
    public void HasRequestBody_WhenContentLengthZero_IsFalse()
    {
        var request = new DefaultHttpContext().Request;
        request.ContentLength = 0;
        Assert.False(JsonBodyContentTypeFilter.HasRequestBody(request));
    }

    [Fact]
    public void HasRequestBody_WhenChunkedWithoutContentLength_IsTrue()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.TransferEncoding = "chunked";
        Assert.True(JsonBodyContentTypeFilter.HasRequestBody(request));
    }

    [Fact]
    public void HasRequestBody_WhenNoLengthAndNoTransferEncoding_IsFalse()
    {
        var request = new DefaultHttpContext().Request;
        Assert.False(JsonBodyContentTypeFilter.HasRequestBody(request));
    }
}
