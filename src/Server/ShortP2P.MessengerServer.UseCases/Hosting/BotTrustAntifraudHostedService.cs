using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.UseCases.BotTrust;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

/// <summary>
/// Settles pending bot claims over the hourly window, applies antifraud, growth, and logs outcomes.
/// Rating decreases and claim floods are prefixed with <see cref="BotTrustLogPrefixes.Attention"/>.
/// </summary>
public sealed class BotTrustAntifraudHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<BotTrustOptions> options,
    ILogger<BotTrustAntifraudHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.AntifraudPollInterval;
        if (interval < TimeSpan.FromSeconds(5))
            interval = TimeSpan.FromSeconds(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var engine = scope.ServiceProvider.GetRequiredService<BotTrustEngine>();
                var events = await engine.ProcessAsync(stoppingToken).ConfigureAwait(false);
                foreach (var evt in events)
                    LogEvent(evt);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bot trust antifraud tick failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void LogEvent(BotTrustProcessEvent evt)
    {
        var attention = evt.Kind is BotTrustProcessEventKind.RatingDecreased
            or BotTrustProcessEventKind.ClaimFlood;
        var message = attention
            ? $"{BotTrustLogPrefixes.Attention} {evt.Message}"
            : evt.Message;

        switch (evt.Kind)
        {
            case BotTrustProcessEventKind.RatingDecreased:
            case BotTrustProcessEventKind.ClaimFlood:
                logger.LogWarning(
                    "{Prefix} bot={BotNetworkId} kind={Kind} old={OldRating} new={NewRating} {Message}",
                    BotTrustLogPrefixes.Attention,
                    evt.BotNetworkId,
                    evt.Kind,
                    evt.OldRating,
                    evt.NewRating,
                    evt.Message);
                break;
            case BotTrustProcessEventKind.FraudRejected:
            case BotTrustProcessEventKind.RatingRestored:
                logger.LogWarning(
                    "Bot trust antifraud bot={BotNetworkId} kind={Kind} {Message}",
                    evt.BotNetworkId,
                    evt.Kind,
                    message);
                break;
            default:
                logger.LogInformation(
                    "Bot trust bot={BotNetworkId} kind={Kind} {Message}",
                    evt.BotNetworkId,
                    evt.Kind,
                    message);
                break;
        }
    }
}
