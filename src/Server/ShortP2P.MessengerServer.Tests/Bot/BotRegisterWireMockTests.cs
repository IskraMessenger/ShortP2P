using System.Net;
using System.Text.Json;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Http;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ShortP2P.MessengerServer.Tests.Bot;

/// <summary>
/// SHO-18: bot registration/login over HTTP with WireMock instead of a real messenger server.
/// Emphasizes generating a bot TLS certificate and providing its fingerprint on the wire.
/// </summary>
public sealed class BotRegisterWireMockTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly string ServerFingerprint =
        "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";

    private static readonly string IssuedBotKey = new('K', BotLimits.BotKeyLength);

    private readonly WireMockServer _server;
    private readonly HttpClient _http;
    private readonly BotServerApiClient _bots;

    public BotRegisterWireMockTests()
    {
        _server = WireMockServer.Start();
        _http = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
        _bots = new BotServerApiClient(_http);
    }

    [Fact]
    public async Task Register_GeneratesBotCertificate_AndSendsFingerprint_ReceivesServerFingerprint()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned();

        var registerBody = JsonSerializer.Serialize(
            new BotRegisterResponse
            {
                NetworkId = "bot-net-1",
                BotName = "echo",
                BotReadableName = "Echo Bot",
                BotDescription = "test",
                BotKey = IssuedBotKey,
                ServerFingerprintSha256 = ServerFingerprint
            },
            JsonOptions);

        _server
            .Given(Request.Create().WithPath(ApiRoutes.BotServerRegister).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.Created)
                .WithHeader("Content-Type", "application/json")
                .WithBody(registerBody));

        // Provide fingerprint derived from the freshly generated certificate (colon form accepted).
        var response = await _bots.RegisterAsync(new BotRegisterRequest
        {
            NetworkId = "bot-net-1",
            BotName = "echo",
            BotReadableName = "Echo Bot",
            BotDescription = "test",
            FingerprintSha256 = botCert.FingerprintSha256ColonSeparated,
            MessagePublicKey = "{\"k\":\"bot-pub\"}"
        });

        var log = Assert.Single(_server.LogEntries);
        var captured = JsonSerializer.Deserialize<BotRegisterRequest>(log.RequestMessage.Body ?? "", JsonOptions);

        Assert.NotNull(captured);
        Assert.True(CertificateFingerprintRules.IsValid(botCert.FingerprintSha256));
        Assert.True(
            CertificateFingerprintRules.AreEqual(botCert.FingerprintSha256, captured!.FingerprintSha256),
            "Register request must carry the fingerprint of the generated bot certificate.");
        Assert.Equal(IssuedBotKey, response.BotKey);
        Assert.Equal(ServerFingerprint, response.ServerFingerprintSha256);
        Assert.True(CertificateFingerprintRules.IsValid(response.ServerFingerprintSha256));
    }

    [Fact]
    public async Task Login_SendsSameCertificateFingerprint_AndReceivesServerFingerprint()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned("CN=shortp2p-login-bot");

        var loginBody = JsonSerializer.Serialize(
            new BotLoginResponse
            {
                Token = "tok:bot-net-1:device",
                ExpiresAtUtc = new DateTime(2026, 10, 18, 13, 0, 0, DateTimeKind.Utc),
                ServerFingerprintSha256 = ServerFingerprint
            },
            JsonOptions);

        _server
            .Given(Request.Create().WithPath(ApiRoutes.BotServerLogin).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(loginBody));

        var response = await _bots.LoginAsync(new BotLoginRequest
        {
            NetworkId = "bot-net-1",
            BotName = "echo",
            BotKey = IssuedBotKey,
            FingerprintSha256 = botCert.FingerprintSha256
        });

        var log = Assert.Single(_server.LogEntries);
        var captured = JsonSerializer.Deserialize<BotLoginRequest>(log.RequestMessage.Body ?? "", JsonOptions);

        Assert.NotNull(captured);
        Assert.True(
            CertificateFingerprintRules.AreEqual(botCert.FingerprintSha256, captured!.FingerprintSha256),
            "Login must present the same bot certificate fingerprint that was registered.");
        Assert.Equal(ServerFingerprint, response.ServerFingerprintSha256);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
    }

    [Fact]
    public async Task Register_WhenServerRejectsFingerprint_SurfacesApiError()
    {
        using var botCert = TestBotCertificate.CreateSelfSigned();

        _server
            .Given(Request.Create().WithPath(ApiRoutes.BotServerRegister).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode((int)HttpStatusCode.BadRequest)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(
                    new ApiError
                    {
                        Code = "Validation",
                        Message = "FingerprintSha256 is invalid."
                    },
                    JsonOptions)));

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            _bots.RegisterAsync(new BotRegisterRequest
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

    public void Dispose()
    {
        _http.Dispose();
        _server.Stop();
        _server.Dispose();
    }
}
