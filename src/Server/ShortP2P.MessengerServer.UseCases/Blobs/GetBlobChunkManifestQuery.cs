namespace ShortP2P.MessengerServer.UseCases.Blobs;

public sealed record GetBlobChunkManifestQuery(string BlobId, string CallerNetworkId);
