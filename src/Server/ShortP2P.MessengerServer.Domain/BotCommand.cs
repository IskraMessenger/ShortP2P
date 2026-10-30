namespace ShortP2P.MessengerServer.Domain;

/// <summary>One command a bot accepts. <see cref="Args"/> is null when the command takes no arguments.</summary>
public sealed class BotCommand
{
    public string Command { get; set; } = "";

    public IReadOnlyList<BotCommandArgument>? Args { get; set; }

    public string Description { get; set; } = "";
}
