using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;
using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Nick).Cleartext(CleartextFieldRules.MaxNickLength, "nick");
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.Password).RequiredTrimmed("Nick, networkId and password are required.");
        RuleFor(x => x.DeviceId).DeviceId();
    }
}
