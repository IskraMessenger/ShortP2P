namespace ShortP2P.MessengerServer.UseCases.BotClient;

public sealed record AcknowledgeBotMessageCommand(string CallerNetworkId, string MessageId);
