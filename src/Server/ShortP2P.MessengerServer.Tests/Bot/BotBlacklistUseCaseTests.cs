using Microsoft.Extensions.Logging.Abstractions;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Infrastructure.Caching;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotBlacklistUseCaseTests
{
    private static readonly DateTime T0 = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WhenCacheEnabled_ContainsReadsCacheOnly()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: true);
        await h.Repo.AddAsync(new BotBlacklistEntry { NetworkId = "only-repo", AddedAtUtc = T0 });

        Assert.False(await h.Service.ContainsAsync("only-repo"));

        await h.Cache.AddAsync(new BotBlacklistEntry { NetworkId = "only-cache", AddedAtUtc = T0 });
        Assert.True(await h.Service.ContainsAsync("only-cache"));
    }

    [Fact]
    public async Task WhenCacheDisabled_ContainsReadsRepositoryOnly()
    {
        var h = new Harness(cacheEnabled: false, repositoryEnabled: true);
        await h.Repo.AddAsync(new BotBlacklistEntry { NetworkId = "bot", AddedAtUtc = T0 });
        await h.Cache.AddAsync(new BotBlacklistEntry { NetworkId = "other", AddedAtUtc = T0 });

        Assert.True(await h.Service.ContainsAsync("bot"));
        Assert.False(await h.Service.ContainsAsync("other"));
    }

    [Fact]
    public async Task Add_WhenBothEnabled_UpdatesCacheAndRepository()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: true);
        await h.Service.AddAsync("bot");

        Assert.True(await h.Cache.ContainsAsync("bot"));
        Assert.True(await h.Repo.ContainsAsync("bot"));
    }

    [Fact]
    public async Task Add_WhenRepositoryDisabled_UpdatesCacheOnly()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: false);
        await h.Service.AddAsync("bot");

        Assert.True(await h.Cache.ContainsAsync("bot"));
        Assert.False(await h.Repo.ContainsAsync("bot"));
    }

    [Fact]
    public async Task Remove_IsIdempotent()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: true);
        await h.Service.AddAsync("bot");
        await h.Service.RemoveAsync("bot");
        await h.Service.RemoveAsync("bot");

        Assert.False(await h.Service.ContainsAsync("bot"));
        Assert.Empty(await h.GetList.ExecuteAsync());
    }

    [Fact]
    public async Task Guard_WhenBlacklisted_ThrowsForbidden_AndWritesAccessLogPlusWarningPath()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: true);
        await h.Service.AddAsync("bot");

        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Guard.EnsureAllowedAsync("bot", BotAccessOperations.Register));

        Assert.Equal("Forbidden", ex.Code);
        var log = Assert.Single(h.AccessLogRepo.Entries);
        Assert.Equal(BotAccessOperations.Register, log.Operation);
        Assert.Equal(BotAccessLogService.OutcomeForbidden, log.Outcome);
        Assert.Equal("bot", log.NetworkId);
    }

    [Fact]
    public async Task GetBotBlackList_ReturnsSortedIds()
    {
        var h = new Harness(cacheEnabled: true, repositoryEnabled: true);
        await h.Service.AddAsync("b");
        await h.Service.AddAsync("a");

        var list = await h.GetList.ExecuteAsync();
        Assert.Equal(["a", "b"], list);
    }

    private sealed class Harness
    {
        public MemoryRepo Repo { get; } = new();
        public InMemoryBotBlacklistCache Cache { get; } = new();
        public MemoryAccessLog AccessLogRepo { get; } = new();
        public BotBlacklistService Service { get; }
        public BotBlacklistGuard Guard { get; }
        public GetBotBlackListUseCase GetList { get; }

        public Harness(bool cacheEnabled, bool repositoryEnabled)
        {
            var clock = new FakeClock(T0);
            var options = new MessengerCacheOptions
            {
                CacheEnabled = cacheEnabled,
                RepositoryEnabled = repositoryEnabled
            };
            Service = new BotBlacklistService(Repo, Cache, options, clock);
            var accessLog = new BotAccessLogService(AccessLogRepo, clock, NullLogger<BotAccessLogService>.Instance);
            Guard = new BotBlacklistGuard(Service, accessLog);
            GetList = new GetBotBlackListUseCase(Service);
        }
    }

    private sealed class MemoryRepo : IBotBlacklistRepository
    {
        private readonly Dictionary<string, BotBlacklistEntry> _items = new(StringComparer.Ordinal);

        public Task AddAsync(BotBlacklistEntry entry, CancellationToken cancellationToken = default)
        {
            _items[entry.NetworkId] = entry;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
        {
            _items.Remove(networkId);
            return Task.CompletedTask;
        }

        public Task<bool> ContainsAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.ContainsKey(networkId));

        public Task<IReadOnlyList<BotBlacklistEntry>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotBlacklistEntry>>(_items.Values.ToArray());
    }

    private sealed class MemoryAccessLog : IBotAccessLogRepository
    {
        public List<BotAccessLogEntry> Entries { get; } = [];

        public Task AppendAsync(BotAccessLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
