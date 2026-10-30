namespace ShortP2P.Client.Services.MessengerServers;

/// <summary>One reported bot version and the server endpoint that served it.</summary>
public readonly record struct BotVersionPlace(int Version, string Endpoint);
