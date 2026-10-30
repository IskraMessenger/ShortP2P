namespace ShortP2P.MessengerServer.Contracts;

/// <summary>Field length limits for bot registration / auth API payloads.</summary>
public static class BotLimits
{
    public const int MaxBotNameLength = 256;

    public const int MaxBotReadableNameLength = 100;

    public const int MaxBotDescriptionLength = 500;

    /// <summary>
    /// Exact length of bot key (base64). Must match <c>BotKeyGenerator.KeyBase64Length</c>.
    /// <para><b>Secret:</b> shared only between the server and the bot; never expose to clients or third parties.</para>
    /// </summary>
    public const int BotKeyLength = 64;

    /// <summary>Max length of optional end-to-end <c>correlationId</c> on client ↔ bot messages.</summary>
    public const int MaxCorrelationIdLength = 32;

    /// <summary>Max length of bot message RSA public key JSON.</summary>
    public const int MaxMessagePublicKeyLength = 16384;

    /// <summary>Max commands in one bot command list.</summary>
    public const int MaxCommands = 32;

    /// <summary>Max length of a command name, including the leading slash.</summary>
    public const int MaxCommandLength = 32;

    /// <summary>Max positional arguments on one command. Null or empty means the command takes none.</summary>
    public const int MaxCommandArguments = 16;

    /// <summary>Max length of an internal argument name.</summary>
    public const int MaxCommandArgumentNameLength = 64;

    /// <summary>Max length of a command description.</summary>
    public const int MaxCommandDescriptionLength = 256;
}
