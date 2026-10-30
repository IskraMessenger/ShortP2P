using LiteDB;
using Microsoft.Extensions.Options;
using ShortP2P.MessengerServer.Auth.LiteDB.Entities;
using ShortP2P.MessengerServer.Auth.LiteDB.Options;
using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Auth.LiteDB.Repositories;

public sealed class LiteDbBotAccountRepository : IBotAccountRepository, IDisposable
{
    private readonly ILiteDatabase _db;
    private readonly ILiteCollection<BotAccountDocument> _bots;
    private readonly object _gate = new();

    public LiteDbBotAccountRepository(IOptions<AuthLiteDbOptions> options)
    {
        var cs = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Auth:LiteDb:ConnectionString is required.");

        _db = new LiteDatabase(cs);
        _bots = _db.GetCollection<BotAccountDocument>("bot_accounts");
        _bots.EnsureIndex(x => x.BotName, unique: true);
    }

    public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var doc = _bots.FindById(networkId);
            return Task.FromResult(doc?.ToDomain());
        }
    }

    public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var doc = _bots.FindOne(x => x.BotName == botName);
            return Task.FromResult(doc?.ToDomain());
        }
    }

    public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_bots.Exists(x => x.Id == account.NetworkId || x.BotName == account.BotName))
                throw new InvalidOperationException("Bot with the same networkId or botName already exists.");

            _bots.Insert(BotAccountDocument.FromDomain(account));
        }

        return Task.CompletedTask;
    }

    public Task<bool> UpdateVersionAsync(
        string networkId,
        int version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var doc = _bots.FindById(networkId);
            if (doc == null)
                return Task.FromResult(false);

            doc.Version = version;
            return Task.FromResult(_bots.Update(doc));
        }
    }

    public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
            _bots.Delete(networkId);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            var list = _bots.FindAll().Select(doc => doc.ToDomain()).ToArray();
            return Task.FromResult<IReadOnlyList<BotAccount>>(list);
        }
    }

    public void Dispose() => _db.Dispose();
}
