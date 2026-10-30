using System.Text.Json;

namespace ShortP2P.MessengerServer.Domain;

/// <summary>JSON form of a bot command list stored with the bot account.</summary>
public static class BotCommandsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(IReadOnlyList<BotCommand>? commands) =>
        JsonSerializer.Serialize(commands ?? [], Options);

    public static IReadOnlyList<BotCommand> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<BotCommand>>(json, Options) ?? [];
    }
}
