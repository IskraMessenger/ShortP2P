using System.Net.Http.Headers;
using System.Text.Json;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ShortP2P.MessengerServer.Http.Tests;

/// <summary>WireMock stand-in for the messenger server, plus the HTTP client pointed at it.</summary>
public sealed class MessengerServerWireMockHost : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpClient _http;

    public MessengerServerWireMockHost()
    {
        _server = WireMockServer.Start();
        var session = new MessengerServerSession();
        _http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client = new MessengerServerApiClient(_http, session);
    }

    public MessengerServerApiClient Client { get; }

    public string BaseUrl => (_server.Url ?? _server.Urls.First()).TrimEnd('/');

    public void Stub(string method, string path, int statusCode, string? jsonBody = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);
        if (jsonBody is not null)
        {
            response
                .WithHeader("Content-Type", "application/json")
                .WithBody(jsonBody);
        }

        _server
            .Given(Request.Create().WithPath(path).UsingMethod(method))
            .RespondWith(response);
    }

    public string RequireBody(string method, string path)
    {
        var body = Find(method, path).BodyData?.BodyAsString;
        Assert.False(string.IsNullOrWhiteSpace(body));
        return body!;
    }

    public int Count(string method, string path) =>
        _server.LogEntries.Count(entry =>
            entry.RequestMessage is { } request && Matches(request, method, path));

    public string? ContentType(string method, string path)
    {
        var headers = Find(method, path).Headers;
        if (headers is null)
            return null;

        foreach (var pair in headers)
        {
            if (pair.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                return string.Join(",", pair.Value);
        }

        return null;
    }

    public bool HasHeader(string method, string path, string name)
    {
        var headers = Find(method, path).Headers;
        if (headers is null)
            return false;

        foreach (var pair in headers)
        {
            if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public T ReadJson<T>(string method, string path)
    {
        var value = JsonSerializer.Deserialize<T>(RequireBody(method, path), JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    public void Dispose()
    {
        _http.Dispose();
        _server.Stop();
        _server.Dispose();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IRequestMessage Find(string method, string path)
    {
        var matches = _server.LogEntries
            .Select(entry => entry.RequestMessage)
            .OfType<IRequestMessage>()
            .Where(request => Matches(request, method, path))
            .ToList();
        return Assert.Single(matches);
    }

    private static bool Matches(IRequestMessage request, string method, string path) =>
        string.Equals(request.Method, method, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(request.Path, path, StringComparison.OrdinalIgnoreCase);
}
