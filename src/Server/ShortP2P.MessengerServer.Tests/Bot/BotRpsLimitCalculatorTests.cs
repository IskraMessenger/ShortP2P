using ShortP2P.MessengerServer.UseCases.BotRateLimit;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotRpsLimitCalculatorTests
{
    [Theory]
    [InlineData(1, 1.0 / 3.0)]
    [InlineData(2, 0.5)]
    [InlineData(4, 2.0 / 3.0)]
    [InlineData(8, 1.0)]
    [InlineData(16, 4.0 / 3.0)]
    public void GetCpuCoefficient_MatchesLog2Formula(int cores, double expected)
    {
        Assert.Equal(expected, BotRpsLimitCalculator.GetCpuCoefficient(cores), precision: 5);
    }

    [Theory]
    [InlineData(0.25, 0.3)]
    [InlineData(0.5, 0.4)]
    [InlineData(2.0, 0.4 + (2.0 - 0.5) / 7.5)]
    [InlineData(8.0, 0.4 + (8.0 - 0.5) / 7.5)]
    [InlineData(16.0, 2.0)]
    [InlineData(32.0, 2.0)]
    public void GetRamCoefficient_MatchesSpec(double gigabytes, double expected)
    {
        Assert.Equal(expected, BotRpsLimitCalculator.GetRamCoefficient(gigabytes), precision: 5);
    }

    [Fact]
    public void GetBandwidthCoefficient_At1Mbps_UsesFloor()
    {
        Assert.Equal(
            BotRpsLimitCalculator.MinBandwidthCoefficient,
            BotRpsLimitCalculator.GetBandwidthCoefficient(1),
            precision: 5);
    }

    [Fact]
    public void GetBandwidthCoefficient_At10Mbps_IsOne()
    {
        Assert.Equal(1.0, BotRpsLimitCalculator.GetBandwidthCoefficient(10), precision: 5);
    }

    [Fact]
    public void GetBandwidthCoefficient_At100Mbps_IsTwo()
    {
        Assert.Equal(2.0, BotRpsLimitCalculator.GetBandwidthCoefficient(100), precision: 5);
    }

    [Fact]
    public void GetBandwidthCoefficient_CapsAtFour()
    {
        Assert.Equal(4.0, BotRpsLimitCalculator.GetBandwidthCoefficient(100_000), precision: 5);
    }

    [Fact]
    public void Compute_MultipliesAllCoefficients()
    {
        // 8 cores → 1, 8 GiB → 1.4, 10 Mbps → 1, ryzen_5 → 1
        var limit = BotRpsLimitCalculator.Compute(
            baseRps: 5,
            logicalCoreCount: 8,
            totalRamMegabytes: 8 * 1024,
            bandwidthMbps: 10,
            cpuType: "ryzen_5");

        Assert.Equal(5 * 1.0 * (0.4 + (8.0 - 0.5) / 7.5) * 1.0 * 1.0, limit, precision: 5);
    }

    [Fact]
    public void ResolvePenalty_UnknownType_DefaultsToOne()
    {
        Assert.Equal(1.0, BotCpuTypes.ResolvePenalty("unknown_cpu"));
        Assert.Equal(0.6, BotCpuTypes.ResolvePenalty("celeron"));
        Assert.Equal(0.75, BotCpuTypes.ResolvePenalty("pentium"));
        Assert.Equal(0.75, BotCpuTypes.ResolvePenalty("pentium_4"));
        Assert.Equal(1.25, BotCpuTypes.ResolvePenalty("RYZEN_9"));
    }
}
