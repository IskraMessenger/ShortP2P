using System.Text.Json;
using ShortP2P.Auth;
using ShortP2P.Client;
using ShortP2P.Client.Data;
using ShortP2P.Client.Services.MessengerServers;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Http;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ShortP2P.Messenger.Tests;

/// <summary>
/// Client registration and certificate pinning against a WireMock messenger server.
/// </summary>
public sealed class MessengerServerManagerWireMockTests : IAsyncLifetime
{
    private const string LoginJson = """{"token":"unit-token","expiresAtUtc":"2030-01-01T00:00:00Z"}""";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dir;
    private readonly WireMockServer _server;
    private readonly SqliteMessengerServerRepository _servers;
    private readonly FileSessionStorage _session;
    private readonly AuthService _auth;
    private MessengerServerManager? _manager;

    static MessengerServerManagerWireMockTests()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public MessengerServerManagerWireMockTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "msm-wm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _server = WireMockServer.Start();
        var db = new AppDatabase(Path.Combine(_dir, "app.db"));
        var users = new SqliteUserAuthRepository(db);
        _servers = new SqliteMessengerServerRepository(db);
        _session = new FileSessionStorage(Path.Combine(_dir, "session"));
        _auth = new AuthService(users, _session);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_manager is not null)
            await _manager.DisposeAsync().ConfigureAwait(false);

        _server.Stop();
        _server.Dispose();
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // SQLite may still hold the file; the directory is under the temp path.
        }
    }

    [Fact]
    public async Task AddServerAsync_FetchesCertificate_ThenRegistersAndLogsIn()
    {
        StubCertificate("DE:AD:BE:EF");
        Stub("POST", ApiRoutes.Register, 201);
        Stub("POST", ApiRoutes.Login, 200, LoginJson);
        var manager = await CreateSignedInManagerAsync();

        var entity = await manager.AddServerAsync(ServerUrl, CancelAfter());

        Assert.Equal(ServerUrl, entity.BaseUrl);
        Assert.Equal("deadbeef", entity.FingerprintSha256);
        Assert.True(entity.Trusted);
        Assert.True(entity.Active);
        Assert.True(entity.IsRegistered);
        Assert.Equal("alice", entity.Nick);
        Assert.Equal(_auth.CurrentUser!.NetworkIdShort, entity.NetworkId);
        Assert.True(MessengerServerPasswordGenerator.IsValid(entity.AccountPassword));

        var register = ReadJson<RegisterRequest>("POST", ApiRoutes.Register);
        Assert.Equal(entity.Nick, register.Nick);
        Assert.Equal(entity.NetworkId, register.NetworkId);
        Assert.Equal(entity.AccountPassword, register.Password);
        var installId = await _session.GetAsync(DeviceIdProvider.StorageKey);
        Assert.False(string.IsNullOrWhiteSpace(installId));
        Assert.Equal(DeviceIdProvider.ComputeDeviceId(installId!), register.DeviceId);

        var login = ReadJson<LoginRequest>("POST", ApiRoutes.Login);
        Assert.Equal(register.NetworkId, login.NetworkId);
        Assert.Equal(register.Password, login.Password);
        Assert.Equal(register.DeviceId, login.DeviceId);

        var stored = await _servers.GetByIdAsync(entity.Id);
        Assert.NotNull(stored);
        Assert.Equal("deadbeef", stored!.FingerprintSha256);
        Assert.True(stored.IsRegistered);
        Assert.Equal(entity.AccountPassword, stored.AccountPassword);
    }

    [Fact]
    public async Task AddServerAsync_WhenRegisterConflicts_LogsInAndStoresServer()
    {
        StubCertificate("AB:CD");
        Stub(
            "POST",
            ApiRoutes.Register,
            409,
            """{"code":"Conflict","message":"NetworkId is already registered."}""");
        Stub("POST", ApiRoutes.Login, 200, LoginJson);
        var manager = await CreateSignedInManagerAsync();

        var entity = await manager.AddServerAsync(ServerUrl, CancelAfter());

        Assert.Equal("abcd", entity.FingerprintSha256);
        Assert.True(entity.IsRegistered);
        Assert.True(entity.Trusted);
        Assert.Equal(1, Count("POST", ApiRoutes.Login));
        var stored = await _servers.ListByUserAsync(_auth.CurrentUser!.Id);
        Assert.Single(stored);
    }

    [Fact]
    public async Task AddServerAsync_WhenFingerprintEmpty_DoesNotStoreServer()
    {
        StubCertificate("");
        var manager = await CreateSignedInManagerAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            manager.AddServerAsync(ServerUrl, CancelAfter()));

        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, Count("POST", ApiRoutes.Register));
        Assert.Empty(await _servers.ListByUserAsync(_auth.CurrentUser!.Id));
    }

    [Fact]
    public async Task AddServerAsync_WhenRegisterFails_RemovesServer()
    {
        StubCertificate("DE:AD");
        Stub("POST", ApiRoutes.Register, 500, """{"code":"Internal","message":"nope"}""");
        var manager = await CreateSignedInManagerAsync();

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            manager.AddServerAsync(ServerUrl, CancelAfter()));

        Assert.Equal("Internal", ex.ErrorCode);
        Assert.Equal(0, Count("POST", ApiRoutes.Login));
        Assert.Empty(await _servers.ListByUserAsync(_auth.CurrentUser!.Id));
    }

    [Fact]
    public async Task RecheckServerAsync_WhenFingerprintMatches_KeepsTrusted()
    {
        StubCertificate("DE:AD:BE:EF");
        Stub("POST", ApiRoutes.Register, 201);
        Stub("POST", ApiRoutes.Login, 200, LoginJson);
        var manager = await CreateSignedInManagerAsync();
        var added = await manager.AddServerAsync(ServerUrl, CancelAfter());

        _server.Reset();
        StubCertificate("de:ad:be:ef");

        var result = await manager.RecheckServerAsync(added.Id, CancelAfter());

        Assert.Equal(MessengerServerRecheckStatus.AvailableAndTrusted, result.Status);
        Assert.Equal("deadbeef", result.ExpectedFingerprint);
        Assert.Equal("deadbeef", result.ActualFingerprint);
        Assert.True(result.Server.Trusted);
        Assert.True(result.Server.Active);
    }

    [Fact]
    public async Task RecheckServerAsync_WhenFingerprintChanges_MarksUntrusted()
    {
        StubCertificate("DE:AD:BE:EF");
        Stub("POST", ApiRoutes.Register, 201);
        Stub("POST", ApiRoutes.Login, 200, LoginJson);
        var manager = await CreateSignedInManagerAsync();
        var added = await manager.AddServerAsync(ServerUrl, CancelAfter());

        MessengerServerFailoverEventArgs? failover = null;
        manager.FailoverCompleted += (_, args) => failover = args;

        _server.Reset();
        StubCertificate("CA:FE:BA:BE");

        var result = await manager.RecheckServerAsync(added.Id, CancelAfter());

        Assert.Equal(MessengerServerRecheckStatus.FingerprintMismatch, result.Status);
        Assert.Equal("deadbeef", result.ExpectedFingerprint);
        Assert.Equal("cafebabe", result.ActualFingerprint);
        Assert.False(result.Server.Trusted);
        Assert.False(result.Server.Active);
        var stored = await _servers.GetByIdAsync(added.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.Trusted);
        Assert.False(stored.Active);
        Assert.NotNull(failover);
        Assert.True(failover!.SwitchedToMesh);
    }

    [Fact]
    public async Task RecheckServerAsync_WhenCertificateRequestFails_MarksInactive()
    {
        StubCertificate("DE:AD:BE:EF");
        Stub("POST", ApiRoutes.Register, 201);
        Stub("POST", ApiRoutes.Login, 200, LoginJson);
        var manager = await CreateSignedInManagerAsync();
        var added = await manager.AddServerAsync(ServerUrl, CancelAfter());

        _server.Reset();
        Stub("GET", ApiRoutes.ServerCertificate, 503, """{"code":"Unavailable","message":"down"}""");

        var result = await manager.RecheckServerAsync(added.Id, CancelAfter());

        Assert.Equal(MessengerServerRecheckStatus.Unreachable, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        var stored = await _servers.GetByIdAsync(added.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.Active);
        Assert.True(stored.Trusted);
    }

    private string ServerUrl => (_server.Url ?? _server.Urls.First()).TrimEnd('/');

    private async Task<MessengerServerManager> CreateSignedInManagerAsync()
    {
        var (ok, err) = await _auth.RegisterAsync("alice", "Password1");
        Assert.True(ok, err);
        _manager = new MessengerServerManager(_servers, _auth, new DeviceIdProvider(_session));
        return _manager;
    }

    private void StubCertificate(string fingerprint) =>
        Stub("GET", ApiRoutes.ServerCertificate, 200, CertificateJson(fingerprint));

    private static string CertificateJson(string fingerprint) =>
        "{\"fingerprintSha256\":\"" + fingerprint + "\",\"subject\":\"CN=unit\",\"notAfterUtc\":\"2027-01-02T03:04:05Z\"}";

    private void Stub(string method, string path, int statusCode, string? jsonBody = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);
        if (jsonBody is not null)
        {
            response
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonBody);
        }

        _server
            .Given(Request.Create().WithPath(path).UsingMethod(method))
            .RespondWith(response);
    }

    private T ReadJson<T>(string method, string path)
    {
        var body = Find(method, path).BodyData?.BodyAsString;
        Assert.False(string.IsNullOrWhiteSpace(body));
        var value = JsonSerializer.Deserialize<T>(body!, JsonOptions);
        Assert.NotNull(value);
        return value!;
    }

    private int Count(string method, string path) =>
        _server.LogEntries.Count(entry =>
            entry.RequestMessage is { } request && Matches(request, method, path));

    private IRequestMessage Find(string method, string path)
    {
        var matches = _server.LogEntries
            .Select(entry => entry.RequestMessage)
            .OfType<IRequestMessage>()
            .Where(request => Matches(request, method, path))
            .ToList();
        return Assert.Single(matches);
    }

    private static bool Matches(IRequestMessage request, string method, string path) =>
        string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(request.Path, path, StringComparison.OrdinalIgnoreCase);

    private static CancellationToken CancelAfter()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        return cts.Token;
    }
}
