using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BotRegisterRequestValidator : AbstractValidator<BotRegisterRequest>
{
    public BotRegisterRequestValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.BotName).Cleartext(BotLimits.MaxBotNameLength, "botName");
        RuleFor(x => x.BotReadableName).Cleartext(BotLimits.MaxBotReadableNameLength, "botReadableName");
        RuleFor(x => x.BotDescription).Cleartext(BotLimits.MaxBotDescriptionLength, "botDescription");
        RuleFor(x => x.FingerprintSha256).CertificateFingerprintSha256();
        RuleFor(x => x.MessagePublicKey)
            .RequiredTrimmed("messagePublicKey is required.")
            .MaximumLength(BotLimits.MaxMessagePublicKeyLength);
    }
}
