using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotTrust;

namespace ShortP2P.MessengerServer.Infrastructure.BotTrust;

public static class BotTrustServiceCollectionExtensions
{
    public static IServiceCollection AddBotTrustLiteDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotTrustOptions>(configuration.GetSection(BotTrustOptions.Section));
        services.Configure<BotTrustLiteDbOptions>(configuration.GetSection(BotTrustLiteDbOptions.Section));

        var lite = new BotTrustLiteDbOptions();
        configuration.GetSection(BotTrustLiteDbOptions.Section).Bind(lite);
        if (string.IsNullOrWhiteSpace(lite.ConnectionString))
            throw new InvalidOperationException("BotTrust:LiteDb:ConnectionString is required.");

        services.AddSingleton<IBotTrustStore, LiteDbBotTrustStore>();
        services.AddSingleton(sp =>
        {
            var options = new BotTrustOptions();
            configuration.GetSection(BotTrustOptions.Section).Bind(options);
            return new BotTrustEngine(
                sp.GetRequiredService<IBotTrustStore>(),
                sp.GetRequiredService<IClock>(),
                options);
        });
        return services;
    }
}
