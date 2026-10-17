using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Persistence.Psql.Entities;

public sealed class ClientToBotMessageRecord
{
    public string MessageId { get; set; } = "";
    public string ClientNetworkId { get; set; } = "";
    public string BotNetworkId { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public string EncryptedMessageBase64 { get; set; } = "";
    public string? CorrelationId { get; set; }

    public ClientToBotMessage ToDomain() => new()
    {
        MessageId = MessageId,
        ClientNetworkId = ClientNetworkId,
        BotNetworkId = BotNetworkId,
        CreatedUtc = CreatedUtc,
        EncryptedMessageBase64 = EncryptedMessageBase64,
        CorrelationId = CorrelationId
    };

    public static ClientToBotMessageRecord FromDomain(ClientToBotMessage message) => new()
    {
        MessageId = message.MessageId,
        ClientNetworkId = message.ClientNetworkId,
        BotNetworkId = message.BotNetworkId,
        CreatedUtc = message.CreatedUtc,
        EncryptedMessageBase64 = message.EncryptedMessageBase64,
        CorrelationId = message.CorrelationId
    };
}
