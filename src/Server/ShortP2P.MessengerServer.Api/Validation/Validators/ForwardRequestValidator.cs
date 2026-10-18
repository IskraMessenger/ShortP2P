using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.UseCases.Forwards;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class ForwardRequestValidator : AbstractValidator<ForwardRequest>
{
    public ForwardRequestValidator()
    {
        RuleFor(x => x.ForwardId).ForwardId();
        RuleFor(x => x.SrcNetworkId).NetworkId("srcNetworkId");
        RuleFor(x => x.TgtNetworkId).NetworkId("tgtNetworkId");
        RuleFor(x => x)
            .Must(x => !string.Equals(x.SrcNetworkId?.Trim(), x.TgtNetworkId?.Trim(), StringComparison.Ordinal))
            .WithMessage("srcNetworkId and tgtNetworkId must differ.");
        RuleFor(x => x.Kind).IsInEnum().WithMessage("Unsupported forward kind.");
        RuleFor(x => x.PayloadBase64)
            .CiphertextBase64(ForwardPayloadLimits.MaxReplyLength, "payloadBase64");
    }
}
