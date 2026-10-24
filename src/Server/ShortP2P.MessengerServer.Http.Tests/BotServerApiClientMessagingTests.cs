using System.Net;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Http.Tests;

public sealed class BotServerApiClientMessagingTests : IDisposable
{
    private static readonly string BotKey = new('K', BotLimits.BotKeyLength);

    private readonly BotServerWireMockHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task RemoveAsync_PostsNetworkIdAndBotKey_AndReturnsEcho()
    {
        _host.Stub(
            "POST",
            ApiRoutes.BotServerRemove,
            200,
            $$"""{"networkId":"bot-net-1","botKey":"{{BotKey}}"}""");

        var response = await _host.Client.RemoveAsync(new BotRemoveRequest
        {
            NetworkId = "bot-net-1",
            BotKey = BotKey
        });

        var sent = _host.ReadJson<BotRemoveRequest>("POST", ApiRoutes.BotServerRemove);
        Assert.Equal("bot-net-1", sent.NetworkId);
        Assert.Equal(BotKey, sent.BotKey);
        Assert.Equal("bot-net-1", response.NetworkId);
        Assert.Equal(BotKey, response.BotKey);
    }

    [Fact]
    public async Task SendMessagesAsync_PostsEncryptedBatch_AndReturnsRequestId()
    {
        _host.Stub(
            "POST",
            ApiRoutes.BotServerSendMessages,
            200,
            """{"requestId":"req-1"}""");

        var response = await _host.Client.SendMessagesAsync(new BotSendMessagesRequest
        {
            RequestId = "req-1",
            BotNetworkId = "bot-net-1",
            BotKey = BotKey,
            Messages =
            [
                new BotClientMessageDto
                {
                    NetworkId = "client-a",
                    EncryptedMessageBase64 = "Y2lwaGVy",
                    CorrelationId = "corr-1"
                }
            ]
        });

        var sent = _host.ReadJson<BotSendMessagesRequest>("POST", ApiRoutes.BotServerSendMessages);
        Assert.Equal("req-1", sent.RequestId);
        Assert.Equal("bot-net-1", sent.BotNetworkId);
        Assert.Equal(BotKey, sent.BotKey);
        var message = Assert.Single(sent.Messages);
        Assert.Equal("client-a", message.NetworkId);
        Assert.Equal("Y2lwaGVy", message.EncryptedMessageBase64);
        Assert.Equal("corr-1", message.CorrelationId);
        Assert.Equal("req-1", response.RequestId);
    }

    [Fact]
    public async Task SendMessagesAsync_WhenRateLimited_ThrowsTooManyRequests()
    {
        _host.Stub(
            "POST",
            ApiRoutes.BotServerSendMessages,
            429,
            """{"code":"TooManyRequests","message":"Bot RPS limit exceeded."}""");

        var ex = await Assert.ThrowsAsync<MessengerServerApiException>(() =>
            _host.Client.SendMessagesAsync(new BotSendMessagesRequest
            {
                RequestId = "req-2",
                BotNetworkId = "bot-net-1",
                BotKey = BotKey,
                Messages =
                [
                    new BotClientMessageDto
                    {
                        NetworkId = "client-a",
                        EncryptedMessageBase64 = "Y2lwaGVy"
                    }
                ]
            }));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.Equal("TooManyRequests", ex.ErrorCode);
    }

    [Fact]
    public async Task WaitForIncomeMessagesAsync_ReturnsClientMessages()
    {
        _host.Stub(
            "POST",
            ApiRoutes.BotServerWaitForIncomeMessages,
            200,
            """
            {
              "requestId":"wait-1",
              "messages":[
                {
                  "networkId":"client-a",
                  "encryptedMessageBase64":"aW5ib3VuZA==",
                  "correlationId":"c1"
                }
              ]
            }
            """);

        var response = await _host.Client.WaitForIncomeMessagesAsync(new BotWaitForIncomeMessagesRequest
        {
            RequestId = "wait-1",
            BotNetworkId = "bot-net-1",
            BotKey = BotKey,
            TimeoutSeconds = 5
        });

        var sent = _host.ReadJson<BotWaitForIncomeMessagesRequest>(
            "POST",
            ApiRoutes.BotServerWaitForIncomeMessages);
        Assert.Equal("wait-1", sent.RequestId);
        Assert.Equal("bot-net-1", sent.BotNetworkId);
        Assert.Equal(BotKey, sent.BotKey);
        Assert.Equal(5, sent.TimeoutSeconds);
        Assert.Equal("wait-1", response.RequestId);
        var message = Assert.Single(response.Messages);
        Assert.Equal("client-a", message.NetworkId);
        Assert.Equal("aW5ib3VuZA==", message.EncryptedMessageBase64);
        Assert.Equal("c1", message.CorrelationId);
    }

    [Fact]
    public async Task WaitForIncomeMessagesAsync_WhenEmpty_ReturnsNoMessages()
    {
        _host.Stub(
            "POST",
            ApiRoutes.BotServerWaitForIncomeMessages,
            200,
            """{"requestId":"wait-2","messages":[]}""");

        var response = await _host.Client.WaitForIncomeMessagesAsync(new BotWaitForIncomeMessagesRequest
        {
            RequestId = "wait-2",
            BotNetworkId = "bot-net-1",
            BotKey = BotKey
        });

        Assert.Equal("wait-2", response.RequestId);
        Assert.Empty(response.Messages);
    }
}
