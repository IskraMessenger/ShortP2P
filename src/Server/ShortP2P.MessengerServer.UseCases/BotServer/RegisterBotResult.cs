namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record RegisterBotResult(
    string NetworkId,
    string BotName,
    string BotReadableName,
    string BotDescription,
    string BotKey,
    string ServerFingerprintSha256);
