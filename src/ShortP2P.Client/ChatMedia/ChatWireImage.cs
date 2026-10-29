namespace ShortP2P.Client.ChatMedia;

public sealed record ChatWireImage(string MimeType, byte[] ImageBytes) : ChatWireMessage;
