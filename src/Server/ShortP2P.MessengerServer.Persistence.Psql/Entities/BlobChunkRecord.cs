using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Persistence.Psql.Entities;

public sealed class BlobChunkRecord
{
    public string BlobId { get; set; } = "";

    public int ChunkIndex { get; set; }

    public string SrcNetworkId { get; set; } = "";

    public string TgtNetworkId { get; set; } = "";

    public int TotalChunks { get; set; }

    public long TotalBytes { get; set; }

    public byte[] Ciphertext { get; set; } = [];

    public DateTime CreatedUtc { get; set; }

    public BlobChunk ToDomain() => new()
    {
        BlobId = BlobId,
        ChunkIndex = ChunkIndex,
        SrcNetworkId = SrcNetworkId,
        TgtNetworkId = TgtNetworkId,
        TotalChunks = TotalChunks,
        TotalBytes = TotalBytes,
        Ciphertext = Ciphertext,
        CreatedUtc = CreatedUtc
    };

    public static BlobChunkRecord FromDomain(BlobChunk chunk) => new()
    {
        BlobId = chunk.BlobId,
        ChunkIndex = chunk.ChunkIndex,
        SrcNetworkId = chunk.SrcNetworkId,
        TgtNetworkId = chunk.TgtNetworkId,
        TotalChunks = chunk.TotalChunks,
        TotalBytes = chunk.TotalBytes,
        Ciphertext = chunk.Ciphertext,
        CreatedUtc = chunk.CreatedUtc
    };
}
