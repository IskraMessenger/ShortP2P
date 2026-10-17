using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Persistence.Psql.Entities;

public sealed class BotToClientMessageRecord
{
    public string MessageId { get; set; } = "";
    public string BotNetworkId { get; set; } = "";
    public string ClientNetworkId { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string EncryptedMessageBase64 { get; set; } = "";
    public string? CorrelationId { get; set; }

    public BotToClientMessage ToDomain() => new()
    {
        MessageId = MessageId,
        BotNetworkId = BotNetworkId,
        ClientNetworkId = ClientNetworkId,
        CreatedUtc = CreatedUtc,
        EncryptedMessageBase64 = EncryptedMessageBase64,
        CorrelationId = CorrelationId
    };

    public static BotToClientMessageRecord FromDomain(BotToClientMessage message) => new()
    {
        MessageId = message.MessageId,
        BotNetworkId = message.BotNetworkId,
        ClientNetworkId = message.ClientNetworkId,
        CreatedUtc = message.CreatedUtc,
        EncryptedMessageBase64 = message.EncryptedMessageBase64,
        CorrelationId = message.CorrelationId
    };
}
