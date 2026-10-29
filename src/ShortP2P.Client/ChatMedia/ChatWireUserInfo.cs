namespace ShortP2P.Client.ChatMedia;

/// <summary>TRL-7: peer profile push (AboutMe + Avatar), not persisted as a chat bubble.</summary>
public sealed record ChatWireUserInfo(string AboutMe, byte[]? Avatar) : ChatWireMessage;
