namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record AcknowledgeClientMessageCommand(string BotNetworkId, string BotKey, string MessageId);
