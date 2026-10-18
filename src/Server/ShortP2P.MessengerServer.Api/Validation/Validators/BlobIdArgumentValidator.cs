using FluentValidation;
using ShortP2P.MessengerServer.Api.Validation.Models;

namespace ShortP2P.MessengerServer.Api.Validation.Validators;

public sealed class BlobIdArgumentValidator : AbstractValidator<BlobIdArgument>
{
    public BlobIdArgumentValidator()
    {
        RuleFor(x => x.BlobId).BlobId();
    }
}
