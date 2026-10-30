using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record RegisterBotCommand(
    string NetworkId,
    string BotName,
    string BotReadableName,
    string BotDescription,
    string FingerprintSha256,
    string MessagePublicKey,
    int Version,
    IReadOnlyList<BotCommand>? Commands = null);
