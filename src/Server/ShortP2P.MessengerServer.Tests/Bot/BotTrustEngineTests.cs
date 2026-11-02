using Microsoft.Extensions.Logging.Abstractions;
using ShortP2P.MessengerServer.Api.Persistence.InMemory;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases.BotTrust;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotTrustEngineTests
{
    private static readonly DateTime T0 = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnsurePublished_DefaultsTo08_High()
    {
        var engine = CreateEngine(out _);
        var state = await engine.EnsurePublishedAsync("bot-1");
        Assert.Equal(0.8f, state.Rating);
        Assert.Equal(BotTrustLevel.High, BotTrustRatings.Classify(state.Rating));
        Assert.False(await engine.IsBannedAsync("bot-1"));
    }

    [Fact]
    public async Task UniqueClaim_AfterOneHour_DecreasesBy005()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        // 20 users → flood only when uniqueInHour > 2
        for (var i = 0; i < 20; i++)
            await engine.RememberUserAsync("bot-1", $"user{i}");
        await engine.AddClaimAsync("bot-1", "user0");

        Assert.Equal(0.8f, (await engine.GetRatingAsync("bot-1"))!.Value);

        clock.Advance(TimeSpan.FromHours(1));
        var events = await engine.ProcessAsync();

        Assert.DoesNotContain(events, e => e.Kind == BotTrustProcessEventKind.ClaimFlood);
        Assert.Contains(events, e => e.Kind == BotTrustProcessEventKind.RatingDecreased);
        Assert.Equal(0.75f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
        Assert.Equal(BotTrustLevel.Middle, BotTrustRatings.Classify((await engine.GetRatingAsync("bot-1"))!.Value));
    }

    [Fact]
    public async Task SameComplainant_SecondUnique_DoesNotDoublePenalize()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        await engine.AddClaimAsync("bot-1", "u1");
        clock.Advance(TimeSpan.FromHours(1));
        await engine.ProcessAsync();
        Assert.Equal(0.75f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);

        clock.Advance(TimeSpan.FromMinutes(1));
        await engine.AddClaimAsync("bot-1", "u1");
        clock.Advance(TimeSpan.FromHours(1));
        await engine.ProcessAsync();

        Assert.Equal(0.75f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
    }

    [Fact]
    public async Task MoreThanThreeClaimsFromOneId_InHour_RejectedAndRatingRestored()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        for (var i = 0; i < 4; i++)
            await engine.AddClaimAsync("bot-1", "fraud");

        clock.Advance(TimeSpan.FromHours(1));
        var events = await engine.ProcessAsync();

        Assert.Contains(events, e => e.Kind == BotTrustProcessEventKind.FraudRejected);
        Assert.Contains(events, e => e.Kind == BotTrustProcessEventKind.RatingRestored);
        Assert.Equal(0.8f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
    }

    [Fact]
    public async Task Flood_OverTenPercentUsers_UsesReducedG()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        // 10 users → flood when uniqueInHour > 1.0
        for (var i = 0; i < 10; i++)
            await engine.RememberUserAsync("bot-1", $"user{i}");

        await engine.AddClaimAsync("bot-1", "user0");
        await engine.AddClaimAsync("bot-1", "user1");

        clock.Advance(TimeSpan.FromHours(1));
        var events = await engine.ProcessAsync();

        Assert.Contains(events, e => e.Kind == BotTrustProcessEventKind.ClaimFlood);
        // G = max(0.01, 0.05/2) = 0.025; penalty = 2 * 0.025 = 0.05 → 0.75
        Assert.Equal(0.75f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
    }

    [Fact]
    public async Task QuietMonth_IncreasesBy005()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        clock.Advance(TimeSpan.FromDays(30));
        var events = await engine.ProcessAsync();

        Assert.Contains(events, e => e.Kind == BotTrustProcessEventKind.RatingIncreased);
        Assert.Equal(0.85f, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
        Assert.Equal(BotTrustLevel.High, BotTrustRatings.Classify((await engine.GetRatingAsync("bot-1"))!.Value));
    }

    [Fact]
    public async Task RatingBelow05_IsBanned_Low()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        // 7 unique claims × 0.05 = 0.35 → 0.45
        for (var i = 0; i < 7; i++)
        {
            await engine.AddClaimAsync("bot-1", $"u{i}");
            clock.Advance(TimeSpan.FromHours(1));
            await engine.ProcessAsync();
            clock.Advance(TimeSpan.FromMinutes(1));
        }

        var rating = await engine.GetRatingAsync("bot-1");
        Assert.True(rating < 0.5f);
        Assert.Equal(BotTrustLevel.Low, BotTrustRatings.Classify(rating!.Value));
        Assert.True(await engine.IsBannedAsync("bot-1"));
    }

    [Fact]
    public async Task LowRating_DoesNotAutoRecoverAfterQuietMonths()
    {
        var engine = CreateEngine(out var clock);
        await engine.EnsurePublishedAsync("bot-1");
        for (var i = 0; i < 7; i++)
        {
            await engine.AddClaimAsync("bot-1", $"u{i}");
            clock.Advance(TimeSpan.FromHours(1));
            await engine.ProcessAsync();
            clock.Advance(TimeSpan.FromMinutes(1));
        }

        var banned = (await engine.GetRatingAsync("bot-1"))!.Value;
        Assert.True(banned < 0.5f);

        clock.Advance(TimeSpan.FromDays(90));
        var events = await engine.ProcessAsync();

        Assert.DoesNotContain(events, e => e.Kind == BotTrustProcessEventKind.RatingIncreased);
        Assert.Equal(banned, (await engine.GetRatingAsync("bot-1"))!.Value, precision: 3);
        Assert.True(await engine.IsBannedAsync("bot-1"));
    }

    [Fact]
    public void AttentionPrefix_IsStable()
    {
        Assert.Equal("ATTENTION!!!", BotTrustLogPrefixes.Attention);
        _ = NullLogger.Instance;
    }

    private static BotTrustEngine CreateEngine(out FakeClock clock)
    {
        clock = new FakeClock(T0);
        var store = new InMemoryBotTrustStore();
        return new BotTrustEngine(store, clock, new BotTrustOptions());
    }
}
