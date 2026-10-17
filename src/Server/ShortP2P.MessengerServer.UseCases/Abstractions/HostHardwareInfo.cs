namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public sealed record HostHardwareInfo(
    double MaxClockMhz,
    int LogicalCoreCount,
    double TotalRamMegabytes,
    string? CpuBrandName = null);
