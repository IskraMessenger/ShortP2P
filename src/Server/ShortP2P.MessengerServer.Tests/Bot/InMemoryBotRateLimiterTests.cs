using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Infrastructure.BotRateLimit;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.BotRateLimit;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class InMemoryBotRateLimiterTests
{
    private static readonly DateTime T0 = new(2026, 10, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AllowsBurstUpToCapacity_ThenDenies()
    {
        var h = CreateHarness(baseRps: 2, cores: 8, ramGb: 8, bandwidthMbps: 10);
        // RPS = 2 * 1 * 1.4 * 1 * 1 = 2.8 → capacity max(1, 2.8)=2.8 → 2 full tokens

        Assert.True((await h.Limiter.TryAcquireAsync("bot-a")).IsAllowed);
        Assert.True((await h.Limiter.TryAcquireAsync("bot-a")).IsAllowed);
        var denied = await h.Limiter.TryAcquireAsync("bot-a");
        Assert.False(denied.IsAllowed);
        Assert.True(denied.RetryAfterSeconds is > 0);
    }

    [Fact]
    public async Task DifferentBots_AreIsolated()
    {
        var h = CreateHarness(baseRps: 1, cores: 8, ramGb: 8, bandwidthMbps: 10);
        // RPS = 1.4 → capacity 1.4

        Assert.True((await h.Limiter.TryAcquireAsync("bot-a")).IsAllowed);
        Assert.True((await h.Limiter.TryAcquireAsync("bot-b")).IsAllowed);
    }

    [Fact]
    public async Task ThirdConsecutiveDenial_StartsThirtySecondCooldown()
    {
        var h = CreateHarness(baseRps: 1, cores: 8, ramGb: 8, bandwidthMbps: 10);
        // capacity 1.4 → one allow, then denials

        Assert.True((await h.Limiter.TryAcquireAsync("bot")).IsAllowed);
        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=1
        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=2

        var third = await h.Limiter.TryAcquireAsync("bot"); // N=3 → cooldown 30s
        Assert.False(third.IsAllowed);
        Assert.Equal(30, third.RetryAfterSeconds);

        h.Clock.Advance(TimeSpan.FromSeconds(10));
        var duringCooldown = await h.Limiter.TryAcquireAsync("bot"); // N=4 → 60s
        Assert.False(duringCooldown.IsAllowed);
        Assert.Equal(60, duringCooldown.RetryAfterSeconds);
    }

    [Fact]
    public async Task SuccessfulAcquire_ResetsConsecutive429()
    {
        var h = CreateHarness(baseRps: 1, cores: 8, ramGb: 8, bandwidthMbps: 10);

        Assert.True((await h.Limiter.TryAcquireAsync("bot")).IsAllowed);
        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=1
        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=2

        h.Clock.Advance(TimeSpan.FromSeconds(2));
        Assert.True((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // reset

        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=1 again
        Assert.False((await h.Limiter.TryAcquireAsync("bot")).IsAllowed); // N=2
        var third = await h.Limiter.TryAcquireAsync("bot"); // N=3 → 30s
        Assert.Equal(30, third.RetryAfterSeconds);
    }

    [Fact]
    public async Task Guard_WhenDenied_ThrowsTooManyRequests()
    {
        var h = CreateHarness(baseRps: 0.1, cores: 1, ramGb: 0.25, bandwidthMbps: 1);
        // Force tiny limit → MinRpsLimit 0.1, capacity 1

        Assert.True((await h.Limiter.TryAcquireAsync("bot")).IsAllowed);
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            h.Guard.EnsureAllowedAsync("bot"));

        Assert.Equal("TooManyRequests", ex.Code);
        Assert.True(ex.RetryAfterSeconds is > 0);
    }

    private static Harness CreateHarness(
        double baseRps,
        int cores,
        double ramGb,
        double bandwidthMbps)
    {
        var clock = new FakeClock(T0);
        var options = new BotRateLimitOptions
        {
            Enabled = true,
            BaseRps = baseRps,
            BandwidthMbps = bandwidthMbps,
            CpuType = "ryzen_5",
            CooldownBaseSeconds = 30,
            CooldownTriggerCount = 3
        };

        var hardware = new FakeHostHardwareInfoProvider
        {
            Info = new HostHardwareInfo(3000, cores, ramGb * 1024)
        };

        var limiter = new InMemoryBotRateLimiter(
            new StaticOptionsMonitor<BotRateLimitOptions>(options),
            hardware,
            clock);

        var guard = new BotRateLimitGuard(limiter, Options.Create(options));
        return new Harness(limiter, guard, clock);
    }

    private sealed class Harness(
        InMemoryBotRateLimiter limiter,
        BotRateLimitGuard guard,
        FakeClock clock)
    {
        public InMemoryBotRateLimiter Limiter { get; } = limiter;
        public BotRateLimitGuard Guard { get; } = guard;
        public FakeClock Clock { get; } = clock;
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
