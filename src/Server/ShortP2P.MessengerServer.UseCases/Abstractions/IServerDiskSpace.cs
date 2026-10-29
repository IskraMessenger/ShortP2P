namespace ShortP2P.MessengerServer.UseCases.Abstractions;

/// <summary>Free bytes on the messenger server disk that stores uploads.</summary>
public interface IServerDiskSpace
{
    long AvailableFreeBytes { get; }
}
