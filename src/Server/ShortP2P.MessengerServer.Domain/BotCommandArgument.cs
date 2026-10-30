namespace ShortP2P.MessengerServer.Domain;

/// <summary>One positional argument of a bot command. The name is internal; clients send only the value.</summary>
public sealed class BotCommandArgument
{
    public string ArgName { get; set; } = "";

    /// <summary><c>dynamic</c>, <c>string</c>, <c>number</c>, or <c>datetime</c>.</summary>
    public string ArgType { get; set; } = "";
}
