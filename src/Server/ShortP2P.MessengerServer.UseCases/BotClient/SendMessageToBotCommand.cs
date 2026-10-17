namespace ShortP2P.MessengerServer.UseCases.BotClient;

public sealed record SendMessageToBotCommand(
    string CallerNetworkId,
    string MessageId,
    string BotNetworkId,
    DateTime DateTime,
    string Message,
    string? CorrelationId);
