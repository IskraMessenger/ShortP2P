using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class ClaimBotRequestValidator : AbstractValidator<ClaimBotRequest>
{
    public ClaimBotRequestValidator()
    {
        RuleFor(x => x.BotNetworkId).NetworkId("botNetworkId");
    }
}
