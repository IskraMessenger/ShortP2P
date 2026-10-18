namespace ShortP2P.MessengerServer.Api.Validation.Models;

public sealed record EventsPollArguments(string DeviceId, int? TimeoutSeconds);
