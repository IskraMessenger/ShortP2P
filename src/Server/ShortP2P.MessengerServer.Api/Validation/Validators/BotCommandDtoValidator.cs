using System.Text.RegularExpressions;
using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotCommandDtoValidator : AbstractValidator<BotCommandDto>
{
    private static readonly Regex CommandName = new(
        "^/[A-Za-z][A-Za-z0-9_]{0,30}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public BotCommandDtoValidator()
    {
        RuleFor(x => x.Command)
            .Must(command => command is not null && CommandName.IsMatch(command.Trim()))
            .WithMessage("command is invalid.");
        RuleFor(x => x.Args)
            .Must(args => args is null || args.Count <= BotLimits.MaxCommandArguments)
            .WithMessage($"args exceed {BotLimits.MaxCommandArguments}.")
            .ForEach(arg => arg.SetValidator(new BotCommandArgumentDtoValidator()));
        RuleFor(x => x.Args)
            .Must(BotCommandArgumentDtoValidator.AreNamesUnique)
            .WithMessage("argument names must be unique.");
        RuleFor(x => x.Description).Cleartext(BotLimits.MaxCommandDescriptionLength, "description");
    }

    public static bool AreUnique(IReadOnlyList<BotCommandDto>? commands)
    {
        if (commands is null)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var command in commands)
        {
            if (command is null)
                return false;

            if (!seen.Add(command.Command?.Trim() ?? ""))
                return false;
        }

        return true;
    }
}
