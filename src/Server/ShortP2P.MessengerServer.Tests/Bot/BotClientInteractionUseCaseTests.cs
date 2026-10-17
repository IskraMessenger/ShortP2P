using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotClient;
using ShortP2P.MessengerServer.UseCases.Hosting;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotClientInteractionUseCaseTests
{
    private static readonly DateTime T0 = new(2026, 10, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Wait_ReturnsQueuedBotMessage_WithoutDeletingIt()
    {
        var h = new Harness();
        h.ToClient.Add(Sample("m1"));

        var first = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));
        var second = await h.Wait().ExecuteAsync(new WaitForBotMessagesQuery("client", 1));

        Assert.Single(first);
        Assert.Equal("cipher", first[0].EncryptedMessageBase64);
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
            CreatedAtUtc = T0
        });

        var wake = h.BotWait.WaitAsync("bot", TimeSpan.FromSeconds(5));
        await h.Send().ExecuteAsync(new SendMessageToBotCommand(
            "client", "mid", "bot", T0, "cipher", "corr"));
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
            h.Send().ExecuteAsync(new SendMessageToBotCommand("client", "mid", "bot", T0, "cipher", null)));
        Assert.Equal("NotFound", ex.Code);
    }

    [Fact]
    public async Task Send_WhenCorrelationTooLong_ThrowsValidation()
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
            CreatedAtUtc = T0
        });

        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Send().ExecuteAsync(new SendMessageToBotCommand(
                "client", "mid", "bot", T0, "cipher", new string('x', 33))));
        Assert.Equal("Validation", ex.Code);
    }

    private static BotToClientMessage Sample(string id) => new()
    {
        MessageId = id,
        BotNetworkId = "bot",
        ClientNetworkId = "client",
        CreatedUtc = T0,
        EncryptedMessageBase64 = "cipher",
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

        public WaitForBotMessagesUseCase Wait() =>
            new(ToClient, ClientWait, Options.Create(new MessengerInboxOptions { MaxPollTimeoutSeconds = 30 }), Clock);

        public AcknowledgeBotMessageUseCase Ack() => new(ToClient);

        public SendMessageToBotUseCase Send() => new(Bots, ToBot, BotWait);
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
