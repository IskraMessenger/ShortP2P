using FluentValidation;
using ShortP2P.MessengerServer.Api.Validation.Models;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BlacklistNetworkIdArgumentValidator : AbstractValidator<BlacklistNetworkIdArgument>
{
    public BlacklistNetworkIdArgumentValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
    }
}
