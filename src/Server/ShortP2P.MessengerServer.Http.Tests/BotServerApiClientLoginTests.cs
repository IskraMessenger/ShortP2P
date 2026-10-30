using System.Net;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Http.Tests;

public sealed class BotServerApiClientLoginTests : IDisposable
{
    private static readonly string ServerFingerprint =
        "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";

    private static readonly string BotKey = new('K', BotLimits.BotKeyLength);

    private readonly BotServerWireMockHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task LoginAsync_SendsSameCertificateFingerprint_AndReturnsTokenWithServerFingerprint()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned("CN=shortp2p-login-bot");
        _host.Stub(
            "POST",
            ApiRoutes.BotServerLogin,
            200,
            $$"""
            {
              "token":"tok:bot-net-1:device",
              "expiresAtUtc":"2026-10-18T13:00:00Z",
              "serverFingerprintSha256":"{{ServerFingerprint}}"
            }
            """);

        var response = await _host.Client.LoginAsync(new BotLoginRequest
        {
            NetworkId = "bot-net-1",
            BotName = "echo",
            BotKey = BotKey,
            FingerprintSha256 = botCert.FingerprintSha256,
            Version = 1
        });

        var sent = _host.ReadJson<BotLoginRequest>("POST", ApiRoutes.BotServerLogin);
        Assert.True(
            CertificateFingerprintRules.AreEqual(botCert.FingerprintSha256, sent.FingerprintSha256),
            "Login must present the same bot certificate fingerprint that was registered.");
        Assert.Equal(BotKey, sent.BotKey);
        Assert.Equal(ServerFingerprint, response.ServerFingerprintSha256);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal(new DateTime(2026, 10, 18, 13, 0, 0, DateTimeKind.Utc), response.ExpiresAtUtc);
    }

    [Fact]
    public async Task LoginAsync_WhenUnauthorized_ThrowsApiError()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned();
        _host.Stub(
            "POST",
            ApiRoutes.BotServerLogin,
            401,
            """{"code":"Unauthorized","message":"Invalid bot credentials."}""");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            _host.Client.LoginAsync(new BotLoginRequest
            {
                NetworkId = "bot-net-1",
                BotName = "echo",
                BotKey = BotKey,
                FingerprintSha256 = botCert.FingerprintSha256,
            Version = 1
            }));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("Unauthorized", ex.ErrorCode);
    }
}
