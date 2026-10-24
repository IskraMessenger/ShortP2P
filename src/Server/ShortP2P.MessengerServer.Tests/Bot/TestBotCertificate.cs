using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Tests.Bot;

/// <summary>Ephemeral self-signed bot TLS cert + SHA-256 fingerprint for SHO-18 tests.</summary>
public sealed class TestBotCertificate : IDisposable
{
    private TestBotCertificate(X509Certificate2 certificate, string fingerprintSha256)
    {
        Certificate = certificate;
        FingerprintSha256 = fingerprintSha256;
    }

    public X509Certificate2 Certificate { get; }

    /// <summary>Normalized lowercase hex SHA-256 of <see cref="Certificate"/> raw data.</summary>
    public string FingerprintSha256 { get; }

    /// <summary>Colon-separated uppercase hex (accepted by <see cref="CertificateFingerprintRules"/>).</summary>
    public string FingerprintSha256ColonSeparated =>
        string.Join(':', Enumerable.Range(0, 32).Select(i => FingerprintSha256.Substring(i * 2, 2).ToUpperInvariant()));

    public static TestBotCertificate CreateSelfSigned(string subjectName = "CN=shortp2p-test-bot")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            subjectName,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        var fingerprint = CertificateFingerprintRules.Normalize(
            Convert.ToHexString(SHA256.HashData(certificate.RawData)));

        if (!CertificateFingerprintRules.IsValid(fingerprint))
            throw new InvalidOperationException("Generated certificate fingerprint is invalid.");

        return new TestBotCertificate(certificate, fingerprint);
    }

    public void Dispose() => Certificate.Dispose();
}
