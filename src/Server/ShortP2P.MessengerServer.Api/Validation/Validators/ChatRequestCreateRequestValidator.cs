using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class ChatRequestCreateRequestValidator : AbstractValidator<ChatRequestCreateRequest>
{
    public ChatRequestCreateRequestValidator()
    {
        RuleFor(x => x.PublicKey).RequiredTrimmed("CallerNetworkId, publicKey and targetNetworkId are required.");
        RuleFor(x => x.TargetNetworkId).NetworkId("targetNetworkId");
    }
}
