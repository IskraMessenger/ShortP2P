using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.BotServer;

/// <summary>Trims command fields before they are stored. An empty argument list becomes null.</summary>
public static class BotCommandNormalizer
{
    public static IReadOnlyList<BotCommand> Normalize(IReadOnlyList<BotCommand>? commands)
    {
        if (commands is null || commands.Count == 0)
            return [];

        var result = new BotCommand[commands.Count];
        for (var i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            result[i] = new BotCommand
            {
                Command = (command.Command ?? "").Trim(),
                Args = NormalizeArgs(command.Args),
                Description = (command.Description ?? "").Trim()
            };
        }

        return result;
    }

    private static IReadOnlyList<BotCommandArgument>? NormalizeArgs(IReadOnlyList<BotCommandArgument>? args)
    {
        if (args is null || args.Count == 0)
            return null;

        var result = new BotCommandArgument[args.Count];
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            result[i] = new BotCommandArgument
            {
                ArgName = (arg.ArgName ?? "").Trim(),
                ArgType = (arg.ArgType ?? "").Trim().ToLowerInvariant()
            };
        }

        return result;
    }
}
