namespace ShortP2P.Client.Services.MessengerServers;

/// <summary>
/// Preference order for messenger-server outbound delivery: higher trust, lower peer load, then live rank stats.
/// </summary>
public static class MessengerServerDeliveryPreference
{
    public static int Compare(
        float trustRatingA,
        int registeredClientCountA,
        MessengerServerRankStats statsA,
        float trustRatingB,
        int registeredClientCountB,
        MessengerServerRankStats statsB)
    {
        var trustCmp = trustRatingB.CompareTo(trustRatingA);
        if (trustCmp != 0)
            return trustCmp;

        var loadCmp = registeredClientCountA.CompareTo(registeredClientCountB);
        if (loadCmp != 0)
            return loadCmp;

        return MessengerServerRankComparer.Compare(statsA, statsB);
    }

    public static int CompareConnections(
        MessengerServerConnection a,
        MessengerServerConnection b,
        Func<int, MessengerServerRankStats> getRankStats,
        Func<int, int> getRegisteredClientCount)
    {
        Require.NotNull(a);
        Require.NotNull(b);
        Require.NotNull(getRankStats);
        Require.NotNull(getRegisteredClientCount);

        return Compare(
            a.Entity.TrustRating,
            getRegisteredClientCount(a.Entity.Id),
            getRankStats(a.Entity.Id),
            b.Entity.TrustRating,
            getRegisteredClientCount(b.Entity.Id),
            getRankStats(b.Entity.Id));
    }
}
