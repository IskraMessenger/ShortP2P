using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Infrastructure.Caching;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;
using ShortP2P.MessengerServer.UseCases.BotRateLimit;
using ShortP2P.MessengerServer.UseCases.BotServer;
using ShortP2P.MessengerServer.UseCases.Hosting;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class DeliverBotMessagesInjectionTests
{
    private static readonly DateTime T0 = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);
    private const string Cipher = "Y2lwaGVy";
    private static readonly string BotKey = new('A', BotLimits.BotKeyLength);

    [Fact]
    public async Task ExecuteAsync_WhenClientNetworkIdUnsafe_ThrowsValidation()
    {
        var h = new Harness();
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Deliver().ExecuteAsync(new DeliverBotMessagesCommand(
                "bot",
                BotKey,
                [new DeliverBotMessageItem("client';drop", Cipher, null)])));
        Assert.Equal("Validation", ex.Code);
        Assert.Empty(h.ToClient.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCiphertextNotBase64_ThrowsValidation()
    {
        var h = new Harness();
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Deliver().ExecuteAsync(new DeliverBotMessagesCommand(
                "bot",
                BotKey,
                [new DeliverBotMessageItem("client", "not!!!base64", null)])));
        Assert.Equal("Validation", ex.Code);
        Assert.Empty(h.ToClient.Items);
    }

    private sealed class Harness
    {
        public MemoryBots Bots { get; } = new();
        public MemoryToClient ToClient { get; } = new();
        public BotToClientWaitService Wait { get; } = new();
        public FakeClock Clock { get; } = new(T0);
        public FakePasswordHasher Hasher { get; } = new();

        public Harness()
        {
            var hashed = Hasher.Hash(BotKey);
            Bots.Add(new BotAccount
            {
                NetworkId = "bot",
                BotName = "echo",
                BotReadableName = "Echo",
                BotDescription = "test",
                KeySalt = hashed.Salt,
                KeyHash = hashed.Hash,
                CertificateFingerprintSha256 = new string('a', 64),
                CreatedAtUtc = T0
            });
        }

        public DeliverBotMessagesUseCase Deliver()
        {
            var cache = new InMemoryBotBlacklistCache();
            var blacklist = new BotBlacklistService(
                new EmptyBlacklistRepo(),
                cache,
                new MessengerCacheOptions { CacheEnabled = true, RepositoryEnabled = true },
                Clock);
            var accessLog = new BotAccessLogService(
                new EmptyAccessLogRepo(),
                Clock,
                NullLogger<BotAccessLogService>.Instance);
            var blacklistGuard = new BotBlacklistGuard(blacklist, accessLog);
            var rateLimit = new BotRateLimitGuard(
                new AlwaysAllowRateLimiter(),
                Options.Create(new BotRateLimitOptions { Enabled = false }));
            return new DeliverBotMessagesUseCase(
                Bots, ToClient, Wait, Hasher, Clock, blacklistGuard, rateLimit);
        }
    }

    private sealed class AlwaysAllowRateLimiter : IBotRateLimiter
    {
        public void NoteRequest()
        {
        }

        public long TakeRequestCount() => 0;

        public Task<double> GetRpsLimitAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1000.0);

        public Task<BotRateLimitDecision> TryAcquireAsync(
            string botNetworkId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(BotRateLimitDecision.Allow());
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

    private sealed class MemoryBots : IBotAccountRepository
    {
        private readonly Dictionary<string, BotAccount> _items = new(StringComparer.Ordinal);

        public void Add(BotAccount account) => _items[account.NetworkId] = account;

        public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(networkId, out var a) ? a : null);

        public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(x => x.BotName == botName));

        public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default)
        {
            _items[account.NetworkId] = account;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
        {
            _items.Remove(networkId);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryToClient : IBotToClientMessageRepository
    {
        public List<BotToClientMessage> Items { get; } = [];

        public Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default)
        {
            Items.Add(message);
            return Task.CompletedTask;
        }

        public Task<BotToClientMessage?> FindByIdAsync(string messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(m => m.MessageId == messageId));

        public Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
            string clientNetworkId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotToClientMessage>>(
                Items.Where(m => m.ClientNetworkId == clientNetworkId).ToArray());

        public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(m => m.MessageId == messageId);
            return Task.CompletedTask;
        }

        public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(m => m.BotNetworkId == botNetworkId);
            return Task.CompletedTask;
        }

        public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            Items.RemoveAll(m => m.CreatedUtc < cutoffUtc);
            return Task.CompletedTask;
        }
    }
}
