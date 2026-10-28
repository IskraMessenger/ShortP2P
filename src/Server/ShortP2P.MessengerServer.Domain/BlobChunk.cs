namespace ShortP2P.MessengerServer.Domain;

/// <summary>One piece of an opaque encrypted attachment. Ciphertext is not inspected.</summary>
public sealed class BlobChunk
{
    public required string BlobId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string SrcNetworkId { get; init; }

    public required string TgtNetworkId { get; init; }

    public required int TotalChunks { get; init; }

    public required long TotalBytes { get; init; }

    public required byte[] Ciphertext { get; init; }

    public required DateTime CreatedUtc { get; init; }
}
