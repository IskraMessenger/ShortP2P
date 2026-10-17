using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.Hosting;

/// <summary>
/// Loads durable blacklist into in-memory cache when both stores are enabled.
/// </summary>
public sealed class BotBlacklistWarmupHostedService(
    IServiceScopeFactory scopeFactory,
    IBotBlacklistCache cache,
    MessengerCacheOptions options,
    ILogger<BotBlacklistWarmupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.CacheEnabled || !options.RepositoryEnabled)
            return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IBotBlacklistRepository>();
            var entries = await repository.ListAllAsync(cancellationToken).ConfigureAwait(false);
            await cache.ReplaceAllAsync(entries, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Bot blacklist cache warmed with {Count} entries.", entries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to warm bot blacklist cache from durable storage.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
