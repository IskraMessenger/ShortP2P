using ShortP2P.MessengerServer.UseCases.BotRateLimit;

namespace ShortP2P.MessengerServer.Tests.Bot;

public class BotCpuTypeResolverTests
{
    [Theory]
    [InlineData("Intel(R) Celeron(R) N5105", "celeron")]
    [InlineData("Intel(R) Pentium(R) Gold G6400", "pentium")]
    [InlineData("Intel(R) Pentium(R) 4 CPU 3.00GHz", "pentium_4")]
    [InlineData("AMD Ryzen 5 5600X 6-Core Processor", "ryzen_5")]
    [InlineData("AMD Ryzen 7 5800X", "ryzen_7")]
    [InlineData("AMD Ryzen 9 5950X 16-Core Processor", "ryzen_9")]
    [InlineData("Intel(R) Xeon(R) Platinum 8375C", "xeon_platinum")]
    [InlineData("Intel(R) Xeon(R) CPU E5-2680 v4", "xeon_e5")]
    [InlineData("ARMv8 Processor rev 1 (v8l)", "arm64")]
    [InlineData("aarch64", "arm64")]
    [InlineData("Apple M1", "arm64")]
    [InlineData("Some Unknown CPU", "pentium")]
    [InlineData(null, "pentium")]
    [InlineData("", "pentium")]
    [InlineData("   ", "pentium")]
    public void ResolveFromBrand_MapsKeywords(string? brand, string expected) =>
        Assert.Equal(expected, BotCpuTypeResolver.ResolveFromBrand(brand));

    [Fact]
    public void ResolveCpuType_Auto_UsesBrandMapping()
    {
        var type = BotCpuTypeResolver.ResolveCpuType("Auto", "AMD Ryzen 9 7950X");
        Assert.Equal("ryzen_9", type);
    }

    [Fact]
    public void ResolveCpuType_Auto_CaseInsensitive_FallsBackToPentiumWhenBrandUnclear()
    {
        Assert.Equal("pentium", BotCpuTypeResolver.ResolveCpuType("auto", null));
        Assert.Equal("pentium", BotCpuTypeResolver.ResolveCpuType("AUTO", "Mystery Chip 9000"));
    }

    [Fact]
    public void ResolveCpuType_Explicit_IgnoresBrand()
    {
        Assert.Equal("celeron", BotCpuTypeResolver.ResolveCpuType("celeron", "AMD Ryzen 9 5950X"));
    }

    [Fact]
    public void ResolveCoreCount_UsesDetectedWhenPositive()
    {
        Assert.Equal(8, BotCpuTypeResolver.ResolveCoreCount(8));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolveCoreCount_FallsBackToTwoWhenUnclear(int? cores) =>
        Assert.Equal(2, BotCpuTypeResolver.ResolveCoreCount(cores));
}
