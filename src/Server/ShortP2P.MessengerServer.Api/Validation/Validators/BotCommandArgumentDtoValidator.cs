using System.Text.RegularExpressions;
using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotCommandArgumentDtoValidator : AbstractValidator<BotCommandArgumentDto>
{
    private static readonly Regex ArgumentName = new(
        "^[A-Za-z][A-Za-z0-9_]{0,63}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        "dynamic",
        "string",
        "number",
        "datetime"
    };

    public BotCommandArgumentDtoValidator()
    {
        RuleFor(x => x.ArgName)
            .Must(name => name is not null && ArgumentName.IsMatch(name.Trim()))
            .WithMessage("argName is invalid.");
        RuleFor(x => x.ArgType)
            .Must(type => type is not null && Types.Contains(type.Trim()))
            .WithMessage("argType is invalid.");
    }

    public static bool AreNamesUnique(IReadOnlyList<BotCommandArgumentDto>? args)
    {
        if (args is null)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in args)
        {
            if (arg is null)
                return false;

            if (!seen.Add(arg.ArgName?.Trim() ?? ""))
                return false;
        }

        return true;
    }
}
