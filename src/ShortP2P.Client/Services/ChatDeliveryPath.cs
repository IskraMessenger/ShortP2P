namespace ShortP2P.Client.Services;

/// <summary>
///     How this chat delivers messages. <see cref="Auto"/> keeps the UDP-failure workaround.
///     <see cref="Server"/> and <see cref="Mesh"/> are a manual choice and stay until the user switches.
/// </summary>
public enum ChatDeliveryPath
{
    Auto = 0,
    Server = 1,
    Mesh = 2
}
