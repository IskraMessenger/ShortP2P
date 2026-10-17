namespace ShortP2P.MessengerServer.UseCases.Abstractions;

public sealed record HostLoadInfo(
    double CpuUtilization,
    double AvailableRamFraction);
