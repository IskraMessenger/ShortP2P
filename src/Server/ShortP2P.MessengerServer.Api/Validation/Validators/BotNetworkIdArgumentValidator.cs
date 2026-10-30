using FluentValidation;
using ShortP2P.MessengerServer.Api.Validation.Models;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotNetworkIdArgumentValidator : AbstractValidator<BotNetworkIdArgument>
{
    public BotNetworkIdArgumentValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId("botNetworkId");
    }
}
