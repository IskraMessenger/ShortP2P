namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record DeliverBotMessagesCommand(
    string BotNetworkId,
    string BotKey,
    IReadOnlyList<DeliverBotMessageItem> Messages);
