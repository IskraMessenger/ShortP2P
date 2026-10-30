namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>
/// One bot command. <see cref="Args"/> is null or empty when the command takes no arguments.
/// </summary>
public sealed class BotCommandDto
{
    /// <summary>Command name, including the leading slash. Example: <c>/start</c>.</summary>
    public required string Command { get; init; }

    /// <summary>
    /// Positional arguments. Null or empty when the command takes none.
    /// Each item is an internal name plus <c>dynamic</c>, <c>string</c>, <c>number</c>, or <c>datetime</c>.
    /// </summary>
    public IReadOnlyList<BotCommandArgumentDto>? Args { get; init; }

    /// <summary>Short description shown to clients.</summary>
    public required string Description { get; init; }
}
