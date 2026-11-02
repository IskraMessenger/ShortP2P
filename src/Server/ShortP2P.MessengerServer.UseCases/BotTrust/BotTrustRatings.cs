using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases.BotTrust;

public static class BotTrustRatings
{
    public const float Default = 0.8f;

    public const float HighThreshold = 0.8f;

    public const float LowThreshold = 0.5f;

    public static BotTrustLevel Classify(float rating, BotTrustOptions? options = null)
    {
        var high = options?.HighThreshold ?? HighThreshold;
        var low = options?.LowThreshold ?? LowThreshold;
        if (rating >= high)
            return BotTrustLevel.High;
        if (rating >= low)
            return BotTrustLevel.Middle;
        return BotTrustLevel.Low;
    }

    public static bool IsBanned(float rating, BotTrustOptions? options = null) =>
        Classify(rating, options) == BotTrustLevel.Low;

    public static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
