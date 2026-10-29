namespace ShortP2P.Client.ChatMedia;

public sealed record ChatWireFile(string FileName, string MimeType, byte[] FileBytes) : ChatWireMessage;
