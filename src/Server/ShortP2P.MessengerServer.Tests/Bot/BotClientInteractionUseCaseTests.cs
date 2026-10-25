using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Infrastructure.Caching;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotBlacklist;
using ShortP2P.MessengerServer.UseCases.BotClient;
using ShortP2P.MessengerServer.UseCases.Hosting;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotClientInteractionUseCaseTests
{
    private const string Cipher = "Y2lwaGVy";

    private static readonly DateTime T0 = new(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Wait_ReturnsQueuedBotMessage_WithoutDeletingIt()
    {
        var h = new Harness();
        h.ToClient.Add(Sample("m1"));

        var first = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));
        var second = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));

        Assert.Single(first);
        Assert.Equal(Cipher, first[0].EncryptedMessageBase64);
        Assert.Single(second);
    }

    [Fact]
    public async Task Wait_WhenEmpty_TimesOut()
    {
        var h = new Harness();
        var result = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));
        Assert.Empty(result);
    }

    [Fact]
    public async Task Wait_WakesWhenBotMessageArrives()
    {
        var h = new Harness();
        var pending = h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 5));
        await Task.Delay(50);
        h.ToClient.Add(Sample("m2"));
        h.ClientWait.Notify("client");

        var result = await pending;
        Assert.Equal("m2", Assert.Single(result).MessageId);
    }

    [Fact]
    public async Task Receipt_RemovesMessage_ForRecipientOnly()
    {
        var h = new Harness();
        h.ToClient.Add(Sample("m1"));

        var denied = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Ack().ExecuteAsync(new AcknowledgeBotMessageCommand("other", "m1")));
        Assert.Equal("Unauthorized", denied.Code);

        await h.Ack().ExecuteAsync(new AcknowledgeBotMessageCommand("client", "m1"));
        var left = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));
        Assert.Empty(left);
    }

    [Fact]
    public async Task Send_StoresMessageAndWakesBot()
    {
        var h = new Harness();
        h.Bots.Add(new BotAccount
        {
            NetworkId = "bot",
            BotName = "echo",
            BotReadableName = "Echo",
            BotDescription = "test",
            KeySalt = "s",
            KeyHash = "h",
            CertificateFingerprintSha256 = new string('a', 64),
            CreatedAtUtc = T0
        });

        var wake = h.BotWait.WaitAsync("bot", TimeSpan.FromSeconds(5));
        await h.Send().ExecuteAsync(new SendMessageToBotCommand(
            "client", "mid", "bot", T0, Cipher, "corr"));
        await wake;

        var taken = await h.ToBot.TakeForBotAsync("bot");
        Assert.Equal("client", Assert.Single(taken).ClientNetworkId);
        Assert.Equal("corr", taken[0].CorrelationId);
    }

    [Fact]
    public async Task Send_WhenBotMissing_ThrowsNotFound()
    {
        var h = new Harness();
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Send().ExecuteAsync(new SendMessageToBotCommand("client", "mid", "bot", T0, Cipher, null)));
        Assert.Equal("NotFound", ex.Code);
    }

    [Fact]
    public async Task Send_WhenBotBlacklisted_ThrowsForbidden_AndDoesNotStore()
    {
        var h = new Harness();
        h.Bots.Add(new BotAccount
        {
            NetworkId = "bot",
            BotName = "echo",
            BotReadableName = "Echo",
            BotDescription = "test",
            KeySalt = "s",
            KeyHash = "h",
            CertificateFingerprintSha256 = new string('a', 64),
            CreatedAtUtc = T0
        });
        await h.Blacklist.AddAsync("bot");

        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Send().ExecuteAsync(new SendMessageToBotCommand("client", "mid", "bot", T0, Cipher, null)));
        Assert.Equal("Forbidden", ex.Code);
        Assert.Empty(await h.ToBot.TakeForBotAsync("bot"));
    }

    [Fact]
    public async Task Wait_DropsMessagesFromBlacklistedBots()
    {
        var h = new Harness();
        h.ToClient.Add(Sample("m1"));
        await h.Blacklist.AddAsync("bot");

        var result = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));
        Assert.Empty(result);
        Assert.Null(await h.ToClient.FindByIdAsync("m1"));
    }

    private static BotToClientMessage Sample(string id) => new()
    {
        MessageId = id,
        BotNetworkId = "bot",
        ClientNetworkId = "client",
        CreatedUtc = T0,
        EncryptedMessageBase64 = Cipher,
        CorrelationId = "c"
    };

    private sealed class Harness
    {
        public MemoryBotAccounts Bots { get; } = new();
        public MemoryBotToClient ToClient { get; } = new();
        public MemoryClientToBot ToBot { get; } = new();
        public BotToClientWaitService ClientWait { get; } = new();
        public ClientToBotWaitService BotWait { get; } = new();
        public FakeClock Clock { get; } = new(T0);
        public BotBlacklistService Blacklist { get; }

        public Harness()
        {
            var cache = new InMemoryBotBlacklistCache();
            var repo = new MemoryBotBlacklistRepo();
            Blacklist = new BotBlacklistService(
                repo,
                cache,
                new MessengerCacheOptions { CacheEnabled = true, RepositoryEnabled = true },
                Clock);
        }

        public WaitForBotMessagesUseCase Wait() =>
            new(
                ToClient,
                ClientWait,
                Options.Create(new MessengerInboxOptions { MaxPollTimeoutSeconds = 30 }),
                Clock,
                Blacklist);

        public AcknowledgeBotMessageUseCase Ack() => new(ToClient);

        public SendMessageToBotUseCase Send() => new(Bots, ToBot, BotWait, Blacklist);
    }

    private sealed class MemoryBotBlacklistRepo : IBotBlacklistRepository
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

    private sealed class MemoryBotAccounts : IBotAccountRepository
    {
        private readonly Dictionary<string, BotAccount> _items = new(StringComparer.Ordinal);

        public void Add(BotAccount account) => _items[account.NetworkId] = account;

        public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(networkId, out var account) ? account : null);

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

        public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotAccount>>(_items.Values.ToArray());
    }

    private sealed class MemoryBotToClient : IBotToClientMessageRepository
    {
        private readonly Dictionary<string, BotToClientMessage> _items = new(StringComparer.Ordinal);

        public void Add(BotToClientMessage message) => _items[message.MessageId] = message;

        public Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default)
        {
            _items[message.MessageId] = message;
            return Task.CompletedTask;
        }

        public Task<BotToClientMessage?> FindByIdAsync(string messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(messageId, out var message) ? message : null);

        public Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
            string clientNetworkId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotToClientMessage>>(
                _items.Values.Where(m => m.ClientNetworkId == clientNetworkId).OrderBy(m => m.CreatedUtc).ToArray());

        public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
        {
            _items.Remove(messageId);
            return Task.CompletedTask;
        }

        public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
        {
            foreach (var id in _items.Values.Where(m => m.BotNetworkId == botNetworkId).Select(m => m.MessageId).ToArray())
                _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            foreach (var id in _items.Values.Where(m => m.CreatedUtc < cutoffUtc).Select(m => m.MessageId).ToArray())
                _items.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryClientToBot : IClientToBotMessageRepository
    {
        private readonly Dictionary<string, ClientToBotMessage> _items = new(StringComparer.Ordinal);

        public Task AddAsync(ClientToBotMessage message, CancellationToken cancellationToken = default)
        {
            _items.TryAdd(message.MessageId, message);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.ContainsKey(messageId));

        public Task<IReadOnlyList<ClientToBotMessage>> TakeForBotAsync(
            string botNetworkId,
            CancellationToken cancellationToken = default)
        {
            var taken = _items.Values.Where(m => m.BotNetworkId == botNetworkId).OrderBy(m => m.CreatedUtc).ToArray();
            foreach (var message in taken)
                _items.Remove(message.MessageId);
            return Task.FromResult<IReadOnlyList<ClientToBotMessage>>(taken);
        }

        public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default)
        {
            foreach (var id in _items.Values.Where(m => m.BotNetworkId == botNetworkId).Select(m => m.MessageId).ToArray())
                _items.Remove(id);
            return Task.CompletedTask;
        }

        public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            foreach (var id in _items.Values.Where(m => m.CreatedUtc < cutoffUtc).Select(m => m.MessageId).ToArray())
                _items.Remove(id);
            return Task.CompletedTask;
        }
    }
}
