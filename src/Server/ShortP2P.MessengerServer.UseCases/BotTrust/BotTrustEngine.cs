using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.UseCases.BotTrust;

/// <summary>
/// Bot trust scores on this messenger server.
/// Claims are ingested immediately; rating changes are applied by the antifraud window processor.
/// </summary>
public sealed class BotTrustEngine(IBotTrustStore store, IClock clock, BotTrustOptions options)
{
    private readonly IBotTrustStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly BotTrustOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<BotTrustState> EnsurePublishedAsync(
        string botNetworkId,
        DateTime? publishedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var id = RequireId(botNetworkId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var existing = await _store.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing != null)
                return existing;

            var created = new BotTrustState
            {
                BotNetworkId = id,
                Rating = BotTrustRatings.Clamp01(_options.DefaultRating),
                PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc ?? _clock.UtcNow, DateTimeKind.Utc)
            };
            await _store.UpsertAsync(created, cancellationToken).ConfigureAwait(false);
            return created;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        var id = RequireId(botNetworkId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _store.RemoveAsync(id, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<float?> GetRatingAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        var id = RequireId(botNetworkId);
        var state = await _store.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return state?.Rating;
    }

    public async Task<bool> IsBannedAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        var rating = await GetRatingAsync(botNetworkId, cancellationToken).ConfigureAwait(false);
        return rating is { } r && BotTrustRatings.IsBanned(r, _options);
    }

    public async Task RememberUserAsync(
        string botNetworkId,
        string clientNetworkId,
        CancellationToken cancellationToken = default)
    {
        var botId = RequireId(botNetworkId);
        var userId = RequireId(clientNetworkId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await _store.GetAsync(botId, cancellationToken).ConfigureAwait(false);
            if (state is null)
                return;

            if (!ContainsUser(state, userId))
            {
                state.UserNetworkIds.Add(userId);
                await _store.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Ingests a claim. Rating is not adjusted here — the antifraud hosted service does that.</summary>
    public async Task AddClaimAsync(
        string botNetworkId,
        string complainantNetworkId,
        CancellationToken cancellationToken = default)
    {
        var botId = RequireId(botNetworkId);
        var complainant = RequireId(complainantNetworkId);
        if (string.Equals(botId, complainant, StringComparison.Ordinal))
            throw new InvalidOperationException("A bot cannot claim itself.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await _store.GetAsync(botId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Bot trust record was not found.");

            var now = _clock.UtcNow;
            state.Claims.Add(new BotClaimRecord
            {
                ComplainantNetworkId = complainant,
                Utc = now,
                Processed = false
            });
            state.LastClaimUtc = now;
            state.GrowthMonthsApplied = 0;
            if (!ContainsUser(state, complainant))
                state.UserNetworkIds.Add(complainant);

            await _store.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Applies antifraud for the rolling claim window and quiet monthly growth.
    /// Rating changes happen here (not on each inbound claim).
    /// </summary>
    public async Task<IReadOnlyList<BotTrustProcessEvent>> ProcessAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = _clock.UtcNow;
            var all = await _store.ListAsync(cancellationToken).ConfigureAwait(false);
            var events = new List<BotTrustProcessEvent>();
            foreach (var state in all)
            {
                events.AddRange(ProcessOne(state, now));
                await _store.UpsertAsync(state, cancellationToken).ConfigureAwait(false);
            }

            return events;
        }
        finally
        {
            _gate.Release();
        }
    }

    private List<BotTrustProcessEvent> ProcessOne(BotTrustState state, DateTime now)
    {
        var events = new List<BotTrustProcessEvent>();
        ApplyClaimWindow(state, now, events);
        ApplyGrowth(state, now, events);
        state.Rating = BotTrustRatings.Clamp01(state.Rating);
        return events;
    }

    private void ApplyClaimWindow(BotTrustState state, DateTime now, List<BotTrustProcessEvent> events)
    {
        var pending = state.Claims
            .Where(c => !c.Processed && c.Utc <= now)
            .OrderBy(c => c.Utc)
            .ToList();
        if (pending.Count == 0)
            return;

        // Score claims together for a full hour starting at the oldest pending claim.
        // Rating moves only after that hour has fully elapsed (surge is not applied instantly).
        var bucketStart = pending[0].Utc;
        var bucketEnd = bucketStart + _options.ClaimWindow;
        if (now < bucketEnd)
            return;

        var bucketClaims = pending
            .Where(c => c.Utc >= bucketStart && c.Utc < bucketEnd)
            .ToList();
        if (bucketClaims.Count == 0)
            return;

        var ratingBefore = state.Rating;
        var byComplainant = bucketClaims
            .GroupBy(c => c.ComplainantNetworkId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var fraudIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (id, list) in byComplainant)
        {
            if (list.Count <= _options.MaxClaimsPerComplainantPerWindow)
                continue;

            fraudIds.Add(id);
            events.Add(new BotTrustProcessEvent
            {
                BotNetworkId = state.BotNetworkId,
                Kind = BotTrustProcessEventKind.FraudRejected,
                Message =
                    $"Rejected {list.Count} claims from {id} in one hour (max {_options.MaxClaimsPerComplainantPerWindow}); rating restored.",
                OldRating = ratingBefore,
                NewRating = ratingBefore
            });
        }

        var uniqueValid = byComplainant.Keys
            .Where(id => !fraudIds.Contains(id))
            .Where(id => !IsPenalized(state, id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        var userCount = Math.Max(1, state.UserNetworkIds.Count);
        var floodThreshold = _options.FloodUserShareThreshold * userCount;
        var uniqueInHour = byComplainant.Keys.Count(id => !fraudIds.Contains(id));
        var step = _options.StepG;
        if (uniqueInHour > floodThreshold)
        {
            step = Math.Max(_options.MinFloodStepG, _options.StepG / Math.Max(1, uniqueInHour));
            events.Add(new BotTrustProcessEvent
            {
                BotNetworkId = state.BotNetworkId,
                Kind = BotTrustProcessEventKind.ClaimFlood,
                Message =
                    $"Claim flood: {uniqueInHour} unique complainants in one hour exceeds {floodThreshold:0.##} (10% of {userCount} bot users); step G={step:0.####}.",
                OldRating = state.Rating,
                NewRating = state.Rating
            });
        }

        if (uniqueValid.Count > 0)
        {
            var old = state.Rating;
            var penalty = step * uniqueValid.Count;
            state.Rating = BotTrustRatings.Clamp01(state.Rating - penalty);
            foreach (var id in uniqueValid)
                state.PenalizedComplainants.Add(id);

            events.Add(new BotTrustProcessEvent
            {
                BotNetworkId = state.BotNetworkId,
                Kind = BotTrustProcessEventKind.RatingDecreased,
                Message =
                    $"Bot rating decreased by {penalty:0.####} ({uniqueValid.Count} unique claim(s), G={step:0.####}): {old:0.####} → {state.Rating:0.####}.",
                OldRating = old,
                NewRating = state.Rating
            });
        }
        else if (fraudIds.Count > 0)
        {
            state.Rating = ratingBefore;
            events.Add(new BotTrustProcessEvent
            {
                BotNetworkId = state.BotNetworkId,
                Kind = BotTrustProcessEventKind.RatingRestored,
                Message = $"Bot rating restored to {ratingBefore:0.####} after fraud rejection.",
                OldRating = ratingBefore,
                NewRating = ratingBefore
            });
        }

        foreach (var claim in bucketClaims)
            claim.Processed = true;

        var trimBefore = now - _options.ClaimWindow - _options.ClaimWindow;
        state.Claims.RemoveAll(c => c.Processed && c.Utc < trimBefore);
    }

    private void ApplyGrowth(BotTrustState state, DateTime now, List<BotTrustProcessEvent> events)
    {
        // Low trust (< 0.5) does not recover automatically — only middle/high can grow while quiet.
        if (BotTrustRatings.IsBanned(state.Rating, _options))
            return;

        var quietSince = state.LastClaimUtc ?? state.PublishedAtUtc;
        var quietFor = now - quietSince;
        if (quietFor < _options.GrowthPeriod)
            return;

        var months = (int)(quietFor.Ticks / _options.GrowthPeriod.Ticks);
        if (months <= state.GrowthMonthsApplied)
            return;

        var extra = months - state.GrowthMonthsApplied;
        var old = state.Rating;
        // Quiet growth: +G per full quiet month, capped at 1.0 (starts from current rating).
        state.Rating = BotTrustRatings.Clamp01(state.Rating + _options.StepG * extra);
        state.GrowthMonthsApplied = months;

        if (Math.Abs(state.Rating - old) > 0.0001f)
        {
            events.Add(new BotTrustProcessEvent
            {
                BotNetworkId = state.BotNetworkId,
                Kind = BotTrustProcessEventKind.RatingIncreased,
                Message =
                    $"Bot rating increased by {_options.StepG * extra:0.####} after {extra} quiet month(s): {old:0.####} → {state.Rating:0.####}.",
                OldRating = old,
                NewRating = state.Rating
            });
        }
    }

    private static bool ContainsUser(BotTrustState state, string userId) =>
        state.UserNetworkIds.Any(id => string.Equals(id, userId, StringComparison.Ordinal));

    private static bool IsPenalized(BotTrustState state, string complainantId) =>
        state.PenalizedComplainants.Any(id => string.Equals(id, complainantId, StringComparison.Ordinal));

    private static string RequireId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Network id is required.", nameof(value));
        return value.Trim();
    }
}
