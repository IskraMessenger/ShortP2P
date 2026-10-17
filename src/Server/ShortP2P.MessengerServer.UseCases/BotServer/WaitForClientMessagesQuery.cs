namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record WaitForClientMessagesQuery(string BotNetworkId, string BotKey, int? TimeoutSeconds);
