using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ShortP2P.MessengerServer.UseCases.Abstractions;

namespace ShortP2P.MessengerServer.Api.HostPowers;

public sealed class OsHostHardwareInfoProvider : IHostHardwareInfoProvider
{
    public Task<HostHardwareInfo?> TryGetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var cores = Math.Max(1, Environment.ProcessorCount);
            double mhz;
            double ramMb;
            string? cpuBrand;

            if (OperatingSystem.IsWindows())
            {
                TryReadWindowsCpuInfo(out mhz, out cpuBrand);
                ramMb = TryReadWindowsTotalRamMb() ?? 0;
            }
            else if (OperatingSystem.IsLinux())
            {
                mhz = TryReadLinuxMaxMhz() ?? 0;
                ramMb = TryReadLinuxTotalRamMb() ?? 0;
                cpuBrand = TryReadLinuxCpuBrand();
            }
            else if (OperatingSystem.IsMacOS())
            {
                mhz = MacOsHostMetrics.TryReadMaxClockMhz() ?? 0;
                ramMb = MacOsHostMetrics.TryReadTotalRamMb() ?? 0;
                cpuBrand = MacOsHostMetrics.TryReadCpuBrandString();
            }
            else
            {
                return Task.FromResult<HostHardwareInfo?>(null);
            }

            if (mhz <= 0 || ramMb <= 0)
                return Task.FromResult<HostHardwareInfo?>(null);

            cpuBrand = NormalizeCpuBrand(cpuBrand);
            return Task.FromResult<HostHardwareInfo?>(new HostHardwareInfo(mhz, cores, ramMb, cpuBrand));
        }
        catch
        {
            return Task.FromResult<HostHardwareInfo?>(null);
        }
    }

    private static string? NormalizeCpuBrand(string? cpuBrand) =>
        !string.IsNullOrWhiteSpace(cpuBrand)
            ? cpuBrand.Trim()
            // When brand string is missing, still allow Auto mapping for ARM hosts.
            : RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.Arm
                ? "aarch64"
                : null;

    /// <summary>
    /// Reads clock and friendly CPU name from the existing CentralProcessor key in one open
    /// (same source already used for TotalPower MHz). Prefer this over WMI / System.Management.
    /// Falls back to PROCESSOR_IDENTIFIER when ProcessorNameString is absent.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void TryReadWindowsCpuInfo(out double mhz, out string? cpuBrand)
    {
        mhz = 0;
        cpuBrand = null;
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key is null)
            {
                cpuBrand = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                return;
            }

            mhz = key.GetValue("~MHz") switch
            {
                int i => i,
                long l => l,
                _ => 0
            };
            cpuBrand = key.GetValue("ProcessorNameString") as string
                       ?? Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        }
        catch
        {
            cpuBrand = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
        }
    }

    [SupportedOSPlatform("windows")]
    private static double? TryReadWindowsTotalRamMb()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status))
            return null;
        return status.TotalPhys / (1024.0 * 1024.0);
    }

    private static double? TryReadLinuxMaxMhz()
    {
        try
        {
            const string maxPath = "/sys/devices/system/cpu/cpu0/cpufreq/cpuinfo_max_freq";
            if (File.Exists(maxPath))
            {
                // kHz
                var text = File.ReadAllText(maxPath).Trim();
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var khz) && khz > 0)
                    return khz / 1000.0;
            }

            if (!File.Exists("/proc/cpuinfo"))
                return null;

            foreach (var line in File.ReadLines("/proc/cpuinfo"))
            {
                if (!line.StartsWith("cpu MHz", StringComparison.OrdinalIgnoreCase) &&
                    !line.StartsWith("CPU MHz", StringComparison.OrdinalIgnoreCase))
                    continue;
                var idx = line.IndexOf(':');
                if (idx < 0)
                    continue;
                if (double.TryParse(line[(idx + 1)..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var mhz))
                    return mhz;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? TryReadLinuxCpuBrand()
    {
        try
        {
            if (!File.Exists("/proc/cpuinfo"))
                return null;

            string? modelName = null;
            string? hardware = null;
            string? processor = null;

            foreach (var line in File.ReadLines("/proc/cpuinfo"))
            {
                var idx = line.IndexOf(':');
                if (idx < 0)
                    continue;

                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                if (value.Length == 0)
                    continue;

                if (key.Equals("model name", StringComparison.OrdinalIgnoreCase))
                {
                    modelName = value;
                    break;
                }

                if (hardware is null && key.Equals("Hardware", StringComparison.OrdinalIgnoreCase))
                    hardware = value;
                else if (processor is null && key.Equals("Processor", StringComparison.OrdinalIgnoreCase))
                    processor = value;
            }

            return modelName ?? hardware ?? processor;
        }
        catch
        {
            return null;
        }
    }

    private static double? TryReadLinuxTotalRamMb()
    {
        try
        {
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (!line.StartsWith("MemTotal:", StringComparison.Ordinal))
                    continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var kb))
                    return kb / 1024.0;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
}
