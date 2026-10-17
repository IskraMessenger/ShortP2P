namespace ShortP2P.MessengerServer.UseCases.BotClient;

public sealed record WaitForBotMessagesQuery(string CallerNetworkId, int? TimeoutSeconds);
