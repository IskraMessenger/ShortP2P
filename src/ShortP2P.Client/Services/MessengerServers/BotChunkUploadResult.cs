namespace ShortP2P.Client.Services.MessengerServers;

public sealed record BotChunkUploadResult(string BaseUrl, int TotalChunks, string CiphertextSha256);
