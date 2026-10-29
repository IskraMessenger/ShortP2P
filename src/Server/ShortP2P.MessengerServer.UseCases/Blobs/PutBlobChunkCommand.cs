namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed record PutBlobChunkCommand(
    string BlobId,
    int ChunkIndex,
    string SrcNetworkId,
    string TgtNetworkId,
    int TotalChunks,
    long TotalBytes,
    byte[] Ciphertext);
