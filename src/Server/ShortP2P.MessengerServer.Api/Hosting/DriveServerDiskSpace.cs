using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.Hosting;

/// <summary>Free space on the drive that holds the server process.</summary>
public sealed class DriveServerDiskSpace : IServerDiskSpace
{
    public long AvailableFreeBytes
    {
        get
        {
            try
            {
                var root = Path.GetPathRoot(AppContext.BaseDirectory);
                if (string.IsNullOrEmpty(root))
                    return 0;
                return new DriveInfo(root).AvailableFreeSpace;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return 0;
            }
        }
    }
}
