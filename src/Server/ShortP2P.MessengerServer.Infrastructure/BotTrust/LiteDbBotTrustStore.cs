using LiteDB;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Infrastructure.BotTrust;

public sealed class LiteDbBotTrustStore : IBotTrustStore, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<BotTrustDocument> _collection;
    private readonly object _gate = new();

    public LiteDbBotTrustStore(IOptions<BotTrustLiteDbOptions> options)
    {
        var cs = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("BotTrust:LiteDb:ConnectionString is required.");

        _db = new LiteDatabase(cs);
        _collection = _db.GetCollection<BotTrustDocument>("bot_trust");
        _collection.EnsureIndex(x => x.Id, unique: true);
    }

    public Task<BotTrustState?> GetAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = botNetworkId.Trim();
        lock (_gate)
        {
            var doc = _collection.FindById(id);
            return Task.FromResult(doc?.ToDomain());
        }
    }

    public Task UpsertAsync(BotTrustState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            _collection.Upsert(BotTrustDocument.FromDomain(state));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string botNetworkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = botNetworkId.Trim();
        lock (_gate)
        {
            _collection.Delete(id);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BotTrustState>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            IReadOnlyList<BotTrustState> list = _collection.FindAll()
                .Select(d => d.ToDomain())
                .OrderBy(s => s.BotNetworkId, StringComparer.Ordinal)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public void Dispose() => _db.Dispose();
}
