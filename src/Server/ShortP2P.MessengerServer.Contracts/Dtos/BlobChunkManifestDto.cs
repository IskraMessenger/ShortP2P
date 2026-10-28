namespace ShortP2P.MessengerServer.Contracts.Dtos;

/// <summary>Which ciphertext chunks of a bot attachment are already stored.</summary>
public sealed class BlobChunkManifestDto
{
    public required string BlobId { get; init; }

    public required int TotalChunks { get; init; }

    public required long TotalBytes { get; init; }

    public required int[] Indexes { get; init; }
}
