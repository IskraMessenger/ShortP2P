using FluentValidation;
using ShortP2P.MessengerServer.Contracts.Dtos;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.NetworkId).NetworkId();
        RuleFor(x => x.Password).RequiredTrimmed("NetworkId and password are required.");
        RuleFor(x => x.DeviceId).DeviceId();
    }
}
