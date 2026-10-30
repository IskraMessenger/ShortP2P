using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotCatalog;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class ListBotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsPublicFieldsOrderedByDisplayName()
    {
        var repo = new MemoryBots();
        await repo.AddAsync(Sample("net-z", "zeta", "Zeta", "last"));
        await repo.AddAsync(Sample("net-a", "alpha", "Alpha", "first"));
        await repo.AddAsync(Sample("  ", "skip", "Skip", "no id"));

        var list = await new ListBotsUseCase(repo).ExecuteAsync();

        Assert.Equal(["net-a", "net-z"], list.Select(b => b.NetworkId).ToArray());
        Assert.Equal("Alpha", list[0].BotReadableName);
        Assert.Equal("first", list[0].BotDescription);
        Assert.Equal("zeta", list[1].BotName);
        Assert.Equal("{\"k\":\"alpha\"}", list[0].MessagePublicKey);
    }

    private static BotAccount Sample(string networkId, string name, string readable, string description) => new()
    {
        NetworkId = networkId,
        BotName = name,
        BotReadableName = readable,
        BotDescription = description,
        KeySalt = "salt",
        KeyHash = "hash",
        CertificateFingerprintSha256 = new string('a', 64),
        MessagePublicKey = string.IsNullOrWhiteSpace(networkId) ? "" : $"{{\"k\":\"{name}\"}}",
        Version = 1,
        CreatedAtUtc = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc)
    };

    private sealed class MemoryBots : IBotAccountRepository
    {
        private readonly Dictionary<string, BotAccount> _items = new(StringComparer.Ordinal);

        public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(networkId, out var account) ? account : null);

        public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Values.FirstOrDefault(account => account.BotName == botName));

        public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default)
        {
            _items[account.NetworkId] = account;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateVersionAsync(
            string networkId,
            int version,
            CancellationToken cancellationToken = default)
        {
            if (!_items.TryGetValue(networkId, out var account))
                return Task.FromResult(false);

            account.Version = version;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
        {
            _items.Remove(networkId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotAccount>>(_items.Values.ToArray());
    }
}
