using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotRateLimit;

namespace ShortP2P.MessengerServer.Infrastructure.BotRateLimit;

/// <summary>In-process token-bucket limiter keyed by bot network id.</summary>
public sealed class InMemoryBotRateLimiter(
    IOptionsMonitor<BotRateLimitOptions> options,
    IHostHardwareInfoProvider hardware,
    IClock clock) : IBotRateLimiter
{
    private static readonly TimeSpan LimitRefreshInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, BotBucket> _buckets = new(StringComparer.Ordinal);
    private readonly object _limitGate = new();
    private double _cachedRpsLimit = BotRpsLimitCalculator.MinRpsLimit;
    private DateTime _limitComputedAtUtc = DateTime.MinValue;

    public async Task<BotRateLimitDecision> TryAcquireAsync(
        string botNetworkId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(botNetworkId))
            return BotRateLimitDecision.Allow();

        var opts = options.CurrentValue;
        if (!opts.Enabled)
            return BotRateLimitDecision.Allow();

        var now = clock.UtcNow;
        var rpsLimit = await GetRpsLimitAsync(opts, now, cancellationToken).ConfigureAwait(false);
        var bucket = _buckets.GetOrAdd(botNetworkId.Trim(), _ => new BotBucket(rpsLimit, now));

        lock (bucket.Gate)
        {
            if (now < bucket.CooldownUntilUtc)
                return DenyAndEscalate(bucket, opts, now);

            Refill(bucket, rpsLimit, now);

            if (bucket.Tokens >= 1.0)
            {
                bucket.Tokens -= 1.0;
                bucket.Consecutive429 = 0;
                return BotRateLimitDecision.Allow();
            }

            return DenyAndEscalate(bucket, opts, now);
        }
    }

    private static BotRateLimitDecision DenyAndEscalate(
        BotBucket bucket,
        BotRateLimitOptions opts,
        DateTime now)
    {
        bucket.Consecutive429++;
        var n = bucket.Consecutive429;
        var trigger = Math.Max(1, opts.CooldownTriggerCount);
        var baseSeconds = Math.Max(1, opts.CooldownBaseSeconds);

        if (n >= trigger)
        {
            var cooldownSeconds = baseSeconds * Math.Max(1, n - 2);
            bucket.CooldownUntilUtc = now.AddSeconds(cooldownSeconds);
            return BotRateLimitDecision.Deny(cooldownSeconds);
        }

        var retryAfter = Math.Max(
            1,
            (int)Math.Ceiling(
                (1.0 - bucket.Tokens) / Math.Max(bucket.RpsLimit, BotRpsLimitCalculator.MinRpsLimit)));

        return BotRateLimitDecision.Deny(retryAfter);
    }

    private static void Refill(BotBucket bucket, double rpsLimit, DateTime now)
    {
        bucket.RpsLimit = rpsLimit;
        var elapsed = (now - bucket.LastRefillUtc).TotalSeconds;
        if (elapsed <= 0)
            return;

        var capacity = Math.Max(1.0, rpsLimit);
        bucket.Tokens = Math.Min(capacity, bucket.Tokens + elapsed * rpsLimit);
        bucket.LastRefillUtc = now;
    }

    private async Task<double> GetRpsLimitAsync(
        BotRateLimitOptions opts,
        DateTime now,
        CancellationToken cancellationToken)
    {
        lock (_limitGate)
        {
            if (now - _limitComputedAtUtc < LimitRefreshInterval && _limitComputedAtUtc != DateTime.MinValue)
                return _cachedRpsLimit;
        }

        var info = await hardware.TryGetAsync(cancellationToken).ConfigureAwait(false);
        var cores = info?.LogicalCoreCount > 0
            ? info.LogicalCoreCount
            : Math.Max(1, Environment.ProcessorCount);
        var defaultRamGb = opts.RamGigabytes > 0 ? opts.RamGigabytes : 1;
        var ramMb = info is { TotalRamMegabytes: > 0 }
            ? info.TotalRamMegabytes
            : defaultRamGb * 1024.0;

        var limit = BotRpsLimitCalculator.Compute(
            opts.BaseRps,
            cores,
            ramMb,
            opts.BandwidthMbps,
            opts.CpuType);

        lock (_limitGate)
        {
            _cachedRpsLimit = limit;
            _limitComputedAtUtc = now;
            return _cachedRpsLimit;
        }
    }

    private sealed class BotBucket
    {
        public BotBucket(double rpsLimit, DateTime now)
        {
            RpsLimit = rpsLimit;
            Tokens = Math.Max(1.0, rpsLimit);
            LastRefillUtc = now;
        }

        public object Gate { get; } = new();
        public double Tokens { get; set; }
        public double RpsLimit { get; set; }
        public DateTime LastRefillUtc { get; set; }
        public int Consecutive429 { get; set; }
        public DateTime CooldownUntilUtc { get; set; }
    }
}
