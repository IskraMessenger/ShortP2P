using FluentValidation;

namespace ShortP2P.MessengerServer.Api.Validation;

public static class ValidationServiceCollectionExtensions
{
    public static IServiceCollection AddMessengerFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<FluentValidationFilter>();
        services.AddScoped<FluentValidationFilter>();
        return services;
    }
}
