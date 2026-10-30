using ShortP2P.MessengerServer.Domain;
using ShortP2P.MessengerServer.Tests.Fakes;
using ShortP2P.MessengerServer.UseCases;
using ShortP2P.MessengerServer.UseCases.Abstractions;
using ShortP2P.MessengerServer.UseCases.Blobs;

namespace ShortP2P.MessengerServer.Tests.Blobs;

public class BlobChunkUseCaseTests
{
    [Fact]
    public async Task Put_SameBytes_IsIdempotent_DifferentBytes_Conflict()
    {
        var repo = new MemoryChunks();
        var put = Put(repo);
        var bytes = new byte[] { 1, 2, 3 };

        await put.ExecuteAsync(new PutBlobChunkCommand("m1", 0, "alice", "bot", 1, bytes.Length, bytes));
        await put.ExecuteAsync(new PutBlobChunkCommand("m1", 0, "alice", "bot", 1, bytes.Length, bytes));

        var conflict = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m1", 0, "alice", "bot", 1, bytes.Length, [9, 9, 9])));
        Assert.Equal("Conflict", conflict.Code);
        Assert.Single(await repo.ListByBlobIdAsync("m1"));
    }

    [Fact]
    public async Task ManifestAndGet_AllowParticipantsOnly()
    {
        var repo = new MemoryChunks();
        var clock = new FakeClock(TestHarness.T0);
        var put = Put(repo, clock);
        var first = new byte[BlobLimitsSize()];
        var last = new byte[] { 7, 8 };
        first[0] = 4;
        await put.ExecuteAsync(new PutBlobChunkCommand("m1", 0, "alice", "bot", 2, first.Length + last.Length, first));
        await put.ExecuteAsync(new PutBlobChunkCommand("m1", 1, "alice", "bot", 2, first.Length + last.Length, last));

        var manifest = await new GetBlobChunkManifestUseCase(repo)
            .ExecuteAsync(new GetBlobChunkManifestQuery("m1", "bot"));
        Assert.Equal(2, manifest.TotalChunks);
        Assert.Equal(new[] { 0, 1 }, manifest.Indexes);

        var chunk = await new GetBlobChunkUseCase(repo).ExecuteAsync(new GetBlobChunkQuery("m1", 1, "alice"));
        Assert.Equal(last, chunk.Ciphertext);

        var denied = await Assert.ThrowsAsync<UseCaseException>(() =>
            new GetBlobChunkUseCase(repo).ExecuteAsync(new GetBlobChunkQuery("m1", 0, "eve")));
        Assert.Equal("Unauthorized", denied.Code);
    }

    [Fact]
    public async Task Put_WhenMiddleChunkIsShort_ThrowsValidation()
    {
        var put = Put(new MemoryChunks());
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m1", 0, "alice", "bot", 2, 60, new byte[60])));
        Assert.Equal("Validation", ex.Code);
    }

    [Fact]
    public async Task ClientReceipt_DeletesMessageAndChunks()
    {
        var messages = new MemoryBotToClient();
        var chunks = new MemoryChunks();
        messages.Add(new BotToClientMessage
        {
            MessageId = "m1",
            BotNetworkId = "bot",
            ClientNetworkId = "alice",
            CreatedUtc = TestHarness.T0,
            EncryptedMessageBase64 = "YQ=="
        });
        await chunks.AddAsync(Chunk("m1"));

        var ack = new ShortP2P.MessengerServer.UseCases.BotClient.AcknowledgeBotMessageUseCase(messages, chunks);
        await ack.ExecuteAsync(new ShortP2P.MessengerServer.UseCases.BotClient.AcknowledgeBotMessageCommand("alice", "m1"));
        await ack.ExecuteAsync(new ShortP2P.MessengerServer.UseCases.BotClient.AcknowledgeBotMessageCommand("alice", "m1"));

        Assert.Null(await messages.FindByIdAsync("m1"));
        Assert.False(await chunks.HasAnyAsync("m1"));
    }

    [Fact]
    public async Task Put_FromBot_RejectsWhenOtherUploadsLeaveNoBudget()
    {
        var repo = new MemoryChunks();
        await repo.AddAsync(Incomplete("other", totalBytes: 300, storedChunks: 1, declaredChunks: 2));
        await repo.AddAsync(Complete("done", totalBytes: 900));

        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(1000));
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 501, new byte[501])));

        Assert.Equal("InsufficientStorage", ex.Code);
        Assert.False(await repo.HasAnyAsync("m2"));
    }

    [Fact]
    public async Task Put_FromBot_RejectsWhenFileExceedsEightyPercentOfFreeSpace()
    {
        // 0.8 * 1000 = 800; 801 exceeds the free-space budget with no other uploads.
        var repo = new MemoryChunks();
        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(1000));
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 801, new byte[801])));

        Assert.Equal("InsufficientStorage", ex.Code);
        Assert.False(await repo.HasAnyAsync("m2"));
    }

    [Fact]
    public async Task Put_FromBot_RejectsWhenDiskIsEmpty()
    {
        var repo = new MemoryChunks();
        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(0));
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 1, [1])));

        Assert.Equal("InsufficientStorage", ex.Code);
        Assert.False(await repo.HasAnyAsync("m2"));
    }

    [Fact]
    public async Task Put_FromBot_RejectsWhenSumOfIncompleteUploadsExhaustsBudget()
    {
        // 0.8 * 1000 = 800; two incomplete uploads reserve 500 + 300 = 800.
        var repo = new MemoryChunks();
        await repo.AddAsync(Incomplete("a", totalBytes: 500, storedChunks: 1, declaredChunks: 2));
        await repo.AddAsync(Incomplete("b", totalBytes: 300, storedChunks: 1, declaredChunks: 3));

        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(1000));
        var ex = await Assert.ThrowsAsync<UseCaseException>(() =>
            put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 1, [1])));

        Assert.Equal("InsufficientStorage", ex.Code);
        Assert.False(await repo.HasAnyAsync("m2"));
        Assert.Equal(800, await repo.SumIncompleteUploadBytesAsync("m2"));
    }

    [Fact]
    public async Task Put_FromBot_AcceptsWhenBudgetCoversTheFile()
    {
        var repo = new MemoryChunks();
        await repo.AddAsync(Incomplete("other", totalBytes: 300, storedChunks: 1, declaredChunks: 2));
        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(1000));

        await put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 500, new byte[500]));
        await put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 500, new byte[500]));

        Assert.True(await repo.HasAnyAsync("m2"));
    }

    [Fact]
    public async Task Put_FromBot_AcceptsExactlyEightyPercentOfFreeSpace()
    {
        var repo = new MemoryChunks();
        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(1000));

        await put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "bot", "alice", 1, 800, new byte[800]));

        Assert.True(await repo.HasAnyAsync("m2"));
    }

    [Fact]
    public async Task Put_FromClient_IgnoresDiskBudget()
    {
        var repo = new MemoryChunks();
        var put = Put(repo, bots: new MemoryBots("bot"), disk: new FixedDisk(0));

        await put.ExecuteAsync(new PutBlobChunkCommand("m2", 0, "alice", "bot", 1, 3, [1, 2, 3]));

        Assert.True(await repo.HasAnyAsync("m2"));
    }

    private static PutBlobChunkUseCase Put(
        MemoryChunks repo,
        IClock? clock = null,
        IBotAccountRepository? bots = null,
        IServerDiskSpace? disk = null) =>
        new(repo, clock ?? new FakeClock(TestHarness.T0), bots ?? new NoBots(), disk ?? new FixedDisk(long.MaxValue));

    private static BlobChunk Incomplete(string id, long totalBytes, int storedChunks, int declaredChunks) => new()
    {
        BlobId = id,
        ChunkIndex = storedChunks - 1,
        SrcNetworkId = "alice",
        TgtNetworkId = "bob",
        TotalChunks = declaredChunks,
        TotalBytes = totalBytes,
        Ciphertext = [1],
        CreatedUtc = TestHarness.T0
    };

    private static BlobChunk Complete(string id, long totalBytes) => new()
    {
        BlobId = id,
        ChunkIndex = 0,
        SrcNetworkId = "alice",
        TgtNetworkId = "bob",
        TotalChunks = 1,
        TotalBytes = totalBytes,
        Ciphertext = [1],
        CreatedUtc = TestHarness.T0
    };

    private static int BlobLimitsSize() =>
        ShortP2P.MessengerServer.Contracts.BlobLimits.ChunkSizeBytes;

    private static BlobChunk Chunk(string id) => new()
    {
        BlobId = id,
        ChunkIndex = 0,
        SrcNetworkId = "bot",
        TgtNetworkId = "alice",
        TotalChunks = 1,
        TotalBytes = 1,
        Ciphertext = [1],
        CreatedUtc = TestHarness.T0
    };

    private sealed class MemoryChunks : IBlobChunkRepository
    {
        private readonly Dictionary<(string Id, int Index), BlobChunk> _items = new();

        public Task<BlobChunk?> FindAsync(string blobId, int chunkIndex, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue((blobId, chunkIndex), out var chunk) ? chunk : null);

        public Task<IReadOnlyList<BlobChunk>> ListByBlobIdAsync(string blobId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BlobChunk>>(
                _items.Values.Where(c => c.BlobId == blobId).OrderBy(c => c.ChunkIndex).ToArray());

        public Task<bool> HasAnyAsync(string blobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Keys.Any(k => k.Id == blobId));

        public Task<long> SumIncompleteUploadBytesAsync(string excludeBlobId, CancellationToken cancellationToken = default)
        {
            var sum = _items.Values
                .Where(c => !string.Equals(c.BlobId, excludeBlobId, StringComparison.Ordinal))
                .GroupBy(c => c.BlobId, StringComparer.Ordinal)
                .Where(g => g.Count() < g.First().TotalChunks)
                .Sum(g => g.First().TotalBytes);
            return Task.FromResult(sum);
        }

        public Task AddAsync(BlobChunk chunk, CancellationToken cancellationToken = default)
        {
            _items[(chunk.BlobId, chunk.ChunkIndex)] = chunk;
            return Task.CompletedTask;
        }

        public Task RemoveByBlobIdAsync(string blobId, CancellationToken cancellationToken = default)
        {
            foreach (var key in _items.Keys.Where(k => k.Id == blobId).ToArray())
                _items.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MemoryBotToClient : IBotToClientMessageRepository
    {
        private readonly Dictionary<string, BotToClientMessage> _items = new(StringComparer.Ordinal);

        public void Add(BotToClientMessage message) => _items[message.MessageId] = message;

        public Task AddAsync(BotToClientMessage message, CancellationToken cancellationToken = default)
        {
            _items[message.MessageId] = message;
            return Task.CompletedTask;
        }

        public Task<BotToClientMessage?> FindByIdAsync(string messageId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.TryGetValue(messageId, out var message) ? message : null);

        public Task<IReadOnlyList<BotToClientMessage>> ListForClientAsync(
            string clientNetworkId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotToClientMessage>>(
                _items.Values.Where(m => m.ClientNetworkId == clientNetworkId).ToArray());

        public Task RemoveAsync(string messageId, CancellationToken cancellationToken = default)
        {
            _items.Remove(messageId);
            return Task.CompletedTask;
        }

        public Task RemoveForBotAsync(string botNetworkId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoBots : IBotAccountRepository
    {
        public Task<BotAccount?> FindByNetworkIdAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BotAccount?>(null);

        public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default) =>
            Task.FromResult<BotAccount?>(null);

        public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> UpdateVersionAsync(
            string networkId,
            int version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task RemoveAsync(string networkId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotAccount>>([]);
    }

    private sealed class MemoryBots(string networkId) : IBotAccountRepository
    {
        private readonly BotAccount _account = new()
        {
            NetworkId = networkId,
            BotName = networkId,
            BotReadableName = networkId,
            BotDescription = "",
            KeySalt = "s",
            KeyHash = "h",
            CertificateFingerprintSha256 = new string('a', 64),
            MessagePublicKey = "{}",
            Version = 1,
            CreatedAtUtc = TestHarness.T0
        };

        public Task<BotAccount?> FindByNetworkIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<BotAccount?>(string.Equals(id, _account.NetworkId, StringComparison.Ordinal) ? _account : null);

        public Task<BotAccount?> FindByBotNameAsync(string botName, CancellationToken cancellationToken = default) =>
            Task.FromResult<BotAccount?>(null);

        public Task AddAsync(BotAccount account, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> UpdateVersionAsync(
            string networkId,
            int version,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(networkId, _account.NetworkId, StringComparison.Ordinal))
                return Task.FromResult(false);

            _account.Version = version;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<BotAccount>> ListAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BotAccount>>([_account]);
    }

    private sealed class FixedDisk(long bytes) : IServerDiskSpace
    {
        public long AvailableFreeBytes => bytes;
    }
}
