namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed record BlobChunkManifest(
    string BlobId,
    int TotalChunks,
    long TotalBytes,
    IReadOnlyList<int> Indexes);
