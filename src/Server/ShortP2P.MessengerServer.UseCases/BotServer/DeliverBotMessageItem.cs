namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record DeliverBotMessageItem(
    string ClientNetworkId,
    string EncryptedMessageBase64,
    string? CorrelationId);
