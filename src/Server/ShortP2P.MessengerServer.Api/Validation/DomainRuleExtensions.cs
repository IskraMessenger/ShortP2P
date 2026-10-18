using FluentValidation;
using ShortP2P.MessengerServer.Contracts;
using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.Api.Validation;

/// <summary>FluentValidation rules that wrap Domain / Contracts input rules.</summary>
public static class DomainRuleExtensions
{
    public static IRuleBuilderOptions<T, string?> NetworkId<T>(
        this IRuleBuilder<T, string?> rule,
        string fieldName = "networkId") =>
        rule
            .Must(v => NetworkIdRules.IsValid(v?.Trim()))
            .WithMessage($"{fieldName} is invalid.");

    public static IRuleBuilderOptions<T, string?> DeviceId<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v => DeviceIdRules.IsValid(v?.Trim()))
            .WithMessage("DeviceId must be 64 lowercase hex characters (SHA-256).");

    public static IRuleBuilderOptions<T, string?> MessageId<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v => OpaqueIdRules.IsValid(v?.Trim(), OpaqueIdRules.MaxMessageIdLength))
            .WithMessage("messageId is invalid.");

    public static IRuleBuilderOptions<T, string?> BlobId<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v => OpaqueIdRules.IsValid(v?.Trim(), OpaqueIdRules.MaxBlobIdLength))
            .WithMessage("blobId is invalid.");

    public static IRuleBuilderOptions<T, string?> ForwardId<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v => OpaqueIdRules.IsValid(v?.Trim(), OpaqueIdRules.MaxForwardIdLength))
            .WithMessage("forwardId is invalid.");

    public static IRuleBuilderOptions<T, string?> Cleartext<T>(
        this IRuleBuilder<T, string?> rule,
        int maxLength,
        string fieldName) =>
        rule
            .Must(v => CleartextFieldRules.IsValid(v?.Trim(), maxLength))
            .WithMessage($"{fieldName} is invalid.");

    public static IRuleBuilderOptions<T, string?> CertificateFingerprintSha256<T>(
        this IRuleBuilder<T, string?> rule,
        string fieldName = "fingerprintSha256") =>
        rule
            .Must(v => CertificateFingerprintRules.IsValid(v))
            .WithMessage($"{fieldName} is invalid.");

    public static IRuleBuilderOptions<T, string?> CiphertextBase64<T>(
        this IRuleBuilder<T, string?> rule,
        int maxDecodedBytes,
        string fieldName) =>
        rule
            .Must(v => CiphertextEnvelopeRules.TryValidate(v, maxDecodedBytes, out _, out _))
            .WithMessage($"{fieldName} must be valid base64 of at most {maxDecodedBytes} decoded bytes.");

    public static IRuleBuilderOptions<T, string?> OptionalCorrelationId<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v =>
                string.IsNullOrWhiteSpace(v)
                || OpaqueIdRules.IsValid(v.Trim(), OpaqueIdRules.MaxCorrelationIdLength))
            .WithMessage("correlationId is invalid.");

    public static IRuleBuilderOptions<T, string?> BotKey<T>(this IRuleBuilder<T, string?> rule) =>
        rule
            .Must(v => !string.IsNullOrWhiteSpace(v) && v.Trim().Length == BotLimits.BotKeyLength)
            .WithMessage($"botKey must be {BotLimits.BotKeyLength} characters.");

    public static IRuleBuilderOptions<T, string?> RequiredTrimmed<T>(
        this IRuleBuilder<T, string?> rule,
        string message) =>
        rule
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage(message);

    public static IRuleBuilderOptions<T, DateTime> RequiredUtcTimestamp<T>(
        this IRuleBuilder<T, DateTime> rule,
        string fieldName) =>
        rule
            .Must(v => v != default)
            .WithMessage($"{fieldName} is required.");

    public static IRuleBuilderOptions<T, int?> OptionalTimeoutSeconds<T>(
        this IRuleBuilder<T, int?> rule,
        int maxSeconds) =>
        rule
            .Must(v => v is null || (v >= 1 && v <= maxSeconds))
            .WithMessage($"timeoutSeconds must be between 1 and {maxSeconds}.");
}
