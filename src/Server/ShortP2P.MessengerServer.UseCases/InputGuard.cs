using ShortP2P.MessengerServer.Domain;

namespace ShortP2P.MessengerServer.UseCases;

/// <summary>Maps domain input rules to <see cref="UseCaseException.Validation"/> without echoing raw input.</summary>
public static class InputGuard
{
    public static string NetworkId(string? value, string fieldName = "networkId")
    {
        var trimmed = value?.Trim() ?? "";
        if (!NetworkIdRules.IsValid(trimmed))
            throw UseCaseException.Validation($"{fieldName} is invalid.");
        return trimmed;
    }

    public static string MessageId(string? value)
    {
        var trimmed = value?.Trim() ?? "";
        if (!OpaqueIdRules.IsValid(trimmed, OpaqueIdRules.MaxMessageIdLength))
            throw UseCaseException.Validation("messageId is invalid.");
        return trimmed;
    }

    public static string BlobId(string? value)
    {
        var trimmed = value?.Trim() ?? "";
        if (!OpaqueIdRules.IsValid(trimmed, OpaqueIdRules.MaxBlobIdLength))
            throw UseCaseException.Validation("blobId is invalid.");
        return trimmed;
    }

    public static string ForwardId(string? value)
    {
        var trimmed = value?.Trim() ?? "";
        if (!OpaqueIdRules.IsValid(trimmed, OpaqueIdRules.MaxForwardIdLength))
            throw UseCaseException.Validation("forwardId is invalid.");
        return trimmed;
    }

    public static string? CorrelationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (!OpaqueIdRules.IsValid(trimmed, OpaqueIdRules.MaxCorrelationIdLength))
            throw UseCaseException.Validation("correlationId is invalid.");

        return trimmed;
    }

    public static string Cleartext(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim() ?? "";
        if (!CleartextFieldRules.IsValid(trimmed, maxLength))
            throw UseCaseException.Validation($"{fieldName} is invalid.");
        return trimmed;
    }

    public static string CiphertextBase64(string? value, int maxDecodedBytes, string fieldName = "ciphertext")
    {
        if (!CiphertextEnvelopeRules.TryValidate(value, maxDecodedBytes, out var normalized, out _))
        {
            throw UseCaseException.Validation(
                $"{fieldName} must be valid base64 of at most {maxDecodedBytes} decoded bytes.");
        }

        return normalized;
    }
}
