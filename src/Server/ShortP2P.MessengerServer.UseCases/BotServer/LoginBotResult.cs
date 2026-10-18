namespace ShortP2P.MessengerServer.UseCases.BotServer;

public sealed record LoginBotResult(string Token, DateTime ExpiresAtUtc, string ServerFingerprintSha256);
