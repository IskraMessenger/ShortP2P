using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Auth.LiteDB.Entities;

internal sealed class BotAccountDocument
{
    public string Id { get; set; } = "";
    public string BotName { get; set; } = "";
    public string BotReadableName { get; set; } = "";
    public string BotDescription { get; set; } = "";
    public string KeySalt { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string CertificateFingerprintSha256 { get; set; } = "";
    public string MessagePublicKey { get; set; } = "";
    public int Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public BotAccount ToDomain() => new()
    {
        NetworkId = Id,
        BotName = BotName,
        BotReadableName = BotReadableName,
        BotDescription = BotDescription,
        KeySalt = KeySalt,
        KeyHash = KeyHash,
        CertificateFingerprintSha256 = CertificateFingerprintSha256,
        MessagePublicKey = MessagePublicKey,
        Version = Version,
        CreatedAtUtc = CreatedAtUtc
    };

    public static BotAccountDocument FromDomain(BotAccount account) => new()
    {
        Id = account.NetworkId,
        BotName = account.BotName,
        BotReadableName = account.BotReadableName,
        BotDescription = account.BotDescription,
        KeySalt = account.KeySalt,
        KeyHash = account.KeyHash,
        CertificateFingerprintSha256 = account.CertificateFingerprintSha256,
        MessagePublicKey = account.MessagePublicKey,
        Version = account.Version,
        CreatedAtUtc = account.CreatedAtUtc
    };
}
