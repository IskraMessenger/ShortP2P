namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed record GetBlobChunkQuery(string BlobId, int ChunkIndex, string CallerNetworkId);
