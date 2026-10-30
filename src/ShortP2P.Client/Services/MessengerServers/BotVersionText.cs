using System.Globalization;
using ShortP2P.Client.Services;

namespace ShortP2P.Client.Services.MessengerServers;

public static class BotVersionText
{
    public static string EndpointFromBaseUrl(string baseUrl)
    {
        var uri = new Uri(SqliteMessengerServerRepository.NormalizeBaseUrl(baseUrl));
        var host = uri.Host;
        if (host.Contains(':'))
            host = "[" + host + "]";
        return host + ":" + uri.Port.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Every place is kept, including the same version on different servers.
    /// Ordered by version descending, then endpoint.
    /// </summary>
    public static string Format(IEnumerable<BotVersionPlace> places)
    {
        var unique = new List<BotVersionPlace>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var place in places
                     .OrderByDescending(p => p.Version)
                     .ThenBy(p => p.Endpoint, StringComparer.OrdinalIgnoreCase))
        {
            var endpoint = place.Endpoint?.Trim() ?? "";
            if (place.Version < 0 || endpoint.Length == 0)
                continue;
            var key = place.Version.ToString(CultureInfo.InvariantCulture) + "\n" + endpoint;
            if (!seen.Add(key))
                continue;
            unique.Add(new BotVersionPlace(place.Version, endpoint));
        }

        return string.Join("; ", unique.Select(p =>
            p.Version.ToString(CultureInfo.InvariantCulture) + " · " + p.Endpoint));
    }

    public static string Lookup(IReadOnlyDictionary<string, string> versions, string? networkId)
    {
        if (versions.Count == 0 || string.IsNullOrWhiteSpace(networkId))
            return "";
        var id = ChatRepository.CanonicalPeerNetworkId(networkId);
        return id.Length > 0 && versions.TryGetValue(id, out var text) ? text : "";
    }
}
