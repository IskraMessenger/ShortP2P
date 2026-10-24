using System.Net;
using ShortP2P.MessengerServer.Contracts;

namespace ShortP2P.MessengerServer.Http.Tests;

public sealed class MessengerServerApiClientCertificateTests : IDisposable
{
    private static readonly DateTime NotAfterUtc = new(2027, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    private readonly MessengerServerWireMockHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task GetServerCertificateAsync_WhenOk_ReturnsFingerprintSubjectAndExpiry()
    {
        _host.Stub(
            "GET",
            ApiRoutes.ServerCertificate,
            200,
            """{"fingerprintSha256":"DE:AD:BE:EF","subject":"CN=unit","notAfterUtc":"2027-01-02T03:04:05Z"}""");

        var cert = await _host.Client.GetServerCertificateAsync();

        Assert.Equal("DE:AD:BE:EF", cert.FingerprintSha256);
        Assert.Equal("CN=unit", cert.Subject);
        Assert.Equal(NotAfterUtc, cert.NotAfterUtc);
        Assert.Equal(1, _host.Count("GET", ApiRoutes.ServerCertificate));
        Assert.False(_host.HasHeader("GET", ApiRoutes.ServerCertificate, "Authorization"));
    }

    [Fact]
    public async Task GetServerCertificateAsync_WhenApiError_ThrowsWithErrorCode()
    {
        _host.Stub(
            "GET",
            ApiRoutes.ServerCertificate,
            503,
            """{"code":"Unavailable","message":"Certificate is not loaded."}""");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            _host.Client.GetServerCertificateAsync());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Equal("Unavailable", ex.ErrorCode);
        Assert.Equal("Certificate is not loaded.", ex.Message);
    }
}
