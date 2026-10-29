namespace ShortP2P.Client.ChatMedia;

public sealed record ChatWireTransferOffer(
    string TransferId,
    string TransferToken,
    string PayloadKind,
    string FileName,
    string MimeType,
    long SizeBytes,
    string Host,
    int Port,
    long ExpiresUtcTicks,
    string? BlobId = null,
    int TotalChunks = 0,
    string? CiphertextSha256 = null) : ChatWireMessage
{
    public string ResolveBlobId() =>
        BlobId is null || string.IsNullOrWhiteSpace(BlobId) ? TransferId : BlobId.Trim();
}
