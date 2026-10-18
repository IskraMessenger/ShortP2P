using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotRemoveRequestValidator : AbstractValidator<BotRemoveRequest>
{
    public BotRemoveRequestValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.BotKey).BotKey();
    }
}
