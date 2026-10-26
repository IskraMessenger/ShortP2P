using System.Net;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Http.Tests;

public sealed class BotServerApiClientRegisterTests : IDisposable
{
    private static readonly string ServerFingerprint =
        "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";

    private static readonly string IssuedBotKey = new('K', BotLimits.BotKeyLength);

    private readonly BotServerWireMockHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task RegisterAsync_SendsBotCertificateFingerprint_AndReturnsBotKeyWithServerFingerprint()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned();
        _host.Stub(
            "POST",
            ApiRoutes.BotServerRegister,
            201,
            $$"""
            {
              "networkId":"bot-net-1",
              "botName":"echo",
              "botReadableName":"Echo Bot",
              "botDescription":"test",
              "botKey":"{{IssuedBotKey}}",
              "serverFingerprintSha256":"{{ServerFingerprint}}"
            }
            """);

        var response = await _host.Client.RegisterAsync(new BotRegisterRequest
        {
            NetworkId = "bot-net-1",
            BotName = "echo",
            BotReadableName = "Echo Bot",
            BotDescription = "test",
            FingerprintSha256 = botCert.FingerprintSha256ColonSeparated,
            MessagePublicKey = "{\"k\":\"bot-pub\"}"
        });

        var sent = _host.ReadJson<BotRegisterRequest>("POST", ApiRoutes.BotServerRegister);
        Assert.True(CertificateFingerprintRules.IsValid(botCert.FingerprintSha256));
        Assert.True(
            CertificateFingerprintRules.AreEqual(botCert.FingerprintSha256, sent.FingerprintSha256),
            "Register request must carry the fingerprint of the generated bot certificate.");
        Assert.Equal(IssuedBotKey, response.BotKey);
        Assert.Equal(ServerFingerprint, response.ServerFingerprintSha256);
        Assert.True(CertificateFingerprintRules.IsValid(response.ServerFingerprintSha256));
    }

    [Fact]
    public async Task RegisterAsync_WhenServerRejectsFingerprint_ThrowsApiError()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned();
        _host.Stub(
            "POST",
            ApiRoutes.BotServerRegister,
            400,
            """{"code":"Validation","message":"FingerprintSha256 is invalid."}""");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            _host.Client.RegisterAsync(new BotRegisterRequest
            {
                NetworkId = "bot-net-1",
                BotName = "echo",
                BotReadableName = "Echo Bot",
                BotDescription = "test",
                FingerprintSha256 = botCert.FingerprintSha256,
                MessagePublicKey = "{\"k\":\"bot-pub\"}"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("Validation", ex.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_WhenRequestIsNull_Throws()
    {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _host.Client.RegisterAsync(null!));
        Assert.Equal("request", ex.ParamName);
    }
}
