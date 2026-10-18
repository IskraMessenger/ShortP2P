using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class AddBotToBlacklistRequestValidator : AbstractValidator<AddBotToBlacklistRequest>
{
    public AddBotToBlacklistRequestValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
    }
}
