using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record LoginBotCommand(
    string NetworkId,
    string BotName,
    string BotKey,
    string FingerprintSha256,
    int Version,
    IReadOnlyList<BotCommand>? Commands = null);
