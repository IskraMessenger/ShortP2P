using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotLoginRequestValidator : AbstractValidator<BotLoginRequest>
{
    public BotLoginRequestValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.BotName).RequiredTrimmed("NetworkId, botName and botKey are required.");
        RuleFor(x => x.BotKey).BotKey();
        RuleFor(x => x.FingerprintSha256).CertificateFingerprintSha256();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0).WithMessage("version is invalid.");
    }
}
