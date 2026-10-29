namespace ShortP2P.MessengerServer.UseCases.Blobs;

/// <summary>
/// Budget for a bot upload: <c>0.8 * freeBytes - other in-progress uploads</c>.
/// The file being accepted is not part of the other-uploads sum.
/// </summary>
public static class BotUploadDiskBudget
{
    public static bool Fits(long freeBytes, long otherUploadsBytes, long incomingBytes)
    {
        if (incomingBytes < 1)
            return false;
        if (freeBytes < 0)
            freeBytes = 0;
        if (otherUploadsBytes < 0)
            otherUploadsBytes = 0;

        var usable = (long)Math.Floor(freeBytes * 0.8) - otherUploadsBytes;
        return incomingBytes <= usable;
    }
}
