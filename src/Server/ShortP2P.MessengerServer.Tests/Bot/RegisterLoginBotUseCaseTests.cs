using Microsoft.Extensions.Logging.Abstractions;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Infrastructure.Caching;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;
using ShortP2P.MessengerServer.UseCases.BotServer;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class RegisterLoginBotUseCaseTests
{
    private static readonly DateTime T0 = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly string BotFingerprint = new('b', 64);
    private const string ServerFingerprint = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";
    private static readonly string FixedBotKey = new('K', BotLimits.BotKeyLength);

    [Fact]
    public async Task Register_StoresBotFingerprint_AndReturnsServerFingerprint()
    {
        var h = new Harness();
        var separated = string.Join(
            ':',
            Enumerable.Range(0, 32).Select(i => BotFingerprint.Substring(i * 2, 2).ToUpperInvariant()));
        var result = await h.Register().ExecuteAsync(new RegisterBotCommand(
            "bot-net",
            "echo",
            "Echo Bot",
            "desc",
            separated,
            "{\"k\":\"bot-pub\"}"));

        Assert.Equal(FixedBotKey, result.BotKey);
        Assert.Equal(ServerFingerprint, result.ServerFingerprintSha256);

        var stored = await h.Bots.FindByNetworkIdAsync("bot-net");
        Assert.NotNull(stored);
        Assert.Equal(BotFingerprint, stored.CertificateFingerprintSha256);
        Assert.Equal("{\"k\":\"bot-pub\"}", stored.MessagePublicKey);
    }

    [Fact]
    public async Task Login_WhenFingerprintMatches_ReturnsTokenAndServerFingerprint()
    {
        var h = new Harness();
        await h.Register().ExecuteAsync(new RegisterBotCommand(
            "bot-net", "echo", "Echo Bot", "desc", BotFingerprint, "{\"k\":\"bot-pub\"}"));

        var login = await h.Login().ExecuteAsync(new LoginBotCommand(
            "bot-net", "echo", FixedBotKey, BotFingerprint.ToUpperInvariant()));

        Assert.StartsWith("tok:bot-net:", login.Token);
        Assert.Equal(ServerFingerprint, login.ServerFingerprintSha256);
    }

    [Fact]
    public async Task Login_WhenFingerprintMismatch_ThrowsUnauthorized()
    {
        var h = new Harness();
        await h.Register().ExecuteAsync(new RegisterBotCommand(
            "bot-net", "echo", "Echo Bot", "desc", BotFingerprint, "{\"k\":\"bot-pub\"}"));

        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Login().ExecuteAsync(new LoginBotCommand(
                "bot-net", "echo", FixedBotKey, new string('c', 64))));
        Assert.Equal("Unauthorized", ex.Code);
    }

    private sealed class Harness
    {
        public MemoryBots Bots { get; } = new();
        public FakeClock Clock { get; } = new(T0);
        public FakePasswordHasher Hasher { get; } = new();
        public FakeAuthTokenService Tokens { get; }
        public FakeServerCertificateReader ServerCert { get; } = new()
        {
            Info = new ServerCertificateInfo(ServerFingerprint, "CN=test", T0.AddYears(1))
        };

        private readonly BotBlacklistGuard _blacklistGuard;
        private readonly BotAccessLogService _accessLog;

        public Harness()
        {
            Tokens = new FakeAuthTokenService(Clock);
            var clock = Clock;
            var cache = new InMemoryBotBlacklistCache();
            var blacklist = new BotBlacklistService(
                new EmptyBlacklistRepo(),
                cache,
                new MessengerCacheOptions { CacheEnabled = true, RepositoryEnabled = true },
                clock);
            _accessLog = new BotAccessLogService(
                new EmptyAccessLogRepo(),
                clock,
                NullLogger<BotAccessLogService>.Instance);
            _blacklistGuard = new BotBlacklistGuard(blacklist, _accessLog);
        }

        public RegisterBotUseCase Register() =>
            new(
                Bots,
                new FixedBotKeyGenerator(FixedBotKey),
                Hasher,
                Clock,
                ServerCert,
                _blacklistGuard,
                _accessLog);

        public LoginBotUseCase Login() =>
            new(Bots, Hasher, Tokens, ServerCert, _blacklistGuard, _accessLog);
    }

    private sealed class FixedBotKeyGenerator(string key) : IBotKeyGenerator
    {
        public string Generate(int length = 64) => key;
    }

    private sealed class MemoryBots : IBotAccountRepository
    {
        private readonly Dictionary<string, BotAccount> _byNetwork = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BotAccount> _byName = new(StringComparer.Ordinal);

        public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byNetwork.TryGetValue(networkId, out var a) ? a : null);

        public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byName.TryGetValue(botName, out var a) ? a : null);

        public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default)
        {
            if (!_byNetwork.TryAdd(account.NetworkId, account) || !_byName.TryAdd(account.BotName, account))
                throw new InvalidOperationException("Duplicate.");
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
        {
            if (_byNetwork.Remove(networkId, out var account))
                _byName.Remove(account.BotName);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotAccount>>(_byNetwork.Values.ToArray());
    }

    private sealed class EmptyBlacklistRepo : IBotBlacklistRepository
    {
        public Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotBlacklistEntry>>([]);
    }

    private sealed class EmptyAccessLogRepo : IBotAccessLogRepository
    {
        public Task AppendAsync(BotAccessLogEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
