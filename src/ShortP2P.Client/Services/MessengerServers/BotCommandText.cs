using System.Text;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.Client.Services.MessengerServers;

/// <summary>Formats <see cref="BotCommandDto"/> rows for chat UI lists.</summary>
public static class BotCommandText
{
    public static string FormatArgs(IReadOnlyList<BotCommandArgumentDto>? args)
    {
        if (args == null || args.Count == 0)
            return "";

        var parts = new string[args.Count];
        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i].ArgName?.Trim() ?? "";
            var type = args[i].ArgType?.Trim() ?? "";
            if (name.Length == 0)
                parts[i] = type.Length > 0 ? "<" + type + ">" : "?";
            else if (type.Length == 0)
                parts[i] = "<" + name + ">";
            else
                parts[i] = "<" + name + ":" + type + ">";
        }

        return string.Join(" ", parts);
    }

    public static string FormatLine(BotCommandDto command)
    {
        var name = command.Command?.Trim() ?? "";
        var args = FormatArgs(command.Args);
        var description = command.Description?.Trim() ?? "";
        var sb = new StringBuilder();
        sb.Append(name);
        if (args.Length > 0)
        {
            sb.Append(' ');
            sb.Append(args);
        }

        if (description.Length > 0)
        {
            sb.Append(" — ");
            sb.Append(description);
        }

        return sb.ToString();
    }

    public static string FormatList(IReadOnlyList<BotCommandDto>? commands)
    {
        if (commands == null || commands.Count == 0)
            return "";

        var sb = new StringBuilder();
        for (var i = 0; i < commands.Count; i++)
        {
            if (i > 0)
                sb.AppendLine();
            sb.Append(FormatLine(commands[i]));
        }

        return sb.ToString();
    }
}
