namespace ShortP2P.MessengerServer.Infrastructure.BotTrust;

public sealed class BotTrustLiteDbOptions
{
    public const string Section = "BotTrust:LiteDb";

    public string ConnectionString { get; set; } =
        "Filename=messenger-bot-trust.litedb;Connection=shared";
}
