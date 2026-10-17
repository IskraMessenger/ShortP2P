using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

/// <summary>
/// Once per minute logs <c>server_bots_rps</c> when bots sent at least one send_messages request.
/// </summary>
public sealed class BotRpmMetricsHostedService(
    IBotRateLimiter rateLimiter,
    IClock clock,
    ILogger<BotRpmMetricsHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Period, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await LogMinuteSafeAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task LogMinuteSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rpm = rateLimiter.TakeRequestCount();
            if (rpm <= 0)
                return;

            var rpsLimit = await rateLimiter
                .GetRpsLimitAsync(cancellationToken)
                .ConfigureAwait(false);
            var thresholdRpm = rpsLimit * 60.0;
            var timestamp = clock.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            logger.LogInformation(
                "server_bots_rps {DateTime}. RPM: {Rpm}. Threshold: {Threshold}",
                timestamp,
                rpm,
                thresholdRpm.ToString("0.##", CultureInfo.InvariantCulture));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "server_bots_rps metrics cycle failed.");
        }
    }
}
