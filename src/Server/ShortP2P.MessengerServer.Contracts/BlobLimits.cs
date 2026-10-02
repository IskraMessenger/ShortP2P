namespace ShortP2P.MessengerServer.Contracts;

/// <summary>Limits and headers for opaque encrypted attachment blobs.</summary>
public static class BlobLimits
{
    /// <summary>Max ciphertext size (covers 30 MiB video + hybrid envelope). Keep in sync with <c>PutBlobUseCase.MaxCiphertextBytes</c>.</summary>
    public const int MaxCiphertextBytes = 32 * 1024 * 1024;

    public const string TargetNetworkIdHeader = "X-ShortP2P-Target-NetworkId";

    public static string BlobById(string blobId)
    {
        Require.NotNullOrWhiteSpace(blobId);
        return $"{ApiRoutes.Blobs}/{Uri.EscapeDataString(blobId.Trim())}";
    }
}
