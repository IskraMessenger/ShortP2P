using System.Net;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Http.Tests;

public sealed class MessengerServerApiClientRegisterTests : IDisposable
{
    private readonly MessengerServerWireMockHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task RegisterAsync_WhenCreated_PostsRegistrationAndCompletes()
    {
        _host.Stub("POST", ApiRoutes.Register, 201);

        var request = new RegisterRequest
        {
            Nick = "Alice",
            NetworkId = "net-alice",
            Password = "secret",
            DeviceId = new string('a', 64)
        };

        await _host.Client.RegisterAsync(request);

        var sent = _host.ReadJson<RegisterRequest>("POST", ApiRoutes.Register);
        Assert.Equal(request.Nick, sent.Nick);
        Assert.Equal(request.NetworkId, sent.NetworkId);
        Assert.Equal(request.Password, sent.Password);
        Assert.Equal(request.DeviceId, sent.DeviceId);
        Assert.Contains("\"networkId\"", _host.RequireBody("POST", ApiRoutes.Register), StringComparison.Ordinal);
        Assert.Contains("application/json", _host.ContentType("POST", ApiRoutes.Register), StringComparison.OrdinalIgnoreCase);
        Assert.False(_host.HasHeader("POST", ApiRoutes.Register, "Authorization"));
    }

    [Fact]
    public async Task RegisterAsync_WhenConflict_ThrowsWithErrorCode()
    {
        _host.Stub(
            "POST",
            ApiRoutes.Register,
            409,
            """{"code":"Conflict","message":"NetworkId is already registered."}""");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() => _host.Client.RegisterAsync(Sample()));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("Conflict", ex.ErrorCode);
        Assert.Equal("NetworkId is already registered.", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenBodyIsNotApiError_ThrowsWithStatus()
    {
        _host.Stub("POST", ApiRoutes.Register, 500, "boom");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() => _host.Client.RegisterAsync(Sample()));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Contains("500", ex.Message, StringComparison.Ordinal);
        Assert.Contains("boom", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestIsNull_Throws()
    {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _host.Client.RegisterAsync(null!));
        Assert.Equal("request", ex.ParamName);
    }

    private static RegisterRequest Sample() => new()
    {
        Nick = "Alice",
        NetworkId = "net-alice",
        Password = "secret",
        DeviceId = new string('a', 64)
    };
}
