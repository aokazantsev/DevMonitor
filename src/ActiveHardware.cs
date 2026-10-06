using System.Collections.Generic;

namespace DevMonitor
{
    internal static class ActiveHardware
    {
        public static readonly TemperatureZones DefaultCpuZones = new TemperatureZones(85f, 95f);
        public static readonly TemperatureZones DefaultGpuZones = new TemperatureZones(83f, 87f);

        private static readonly HardwareProfile FallbackCpu =
            new HardwareProfile(HardwareKind.Cpu, "не выбран", DefaultCpuZones, "пороги по умолчанию");
        private static readonly HardwareProfile FallbackGpu =
            new HardwareProfile(HardwareKind.Gpu, "не выбрана", DefaultGpuZones, "пороги по умолчанию");

        public static readonly HardwareProfile Cpu;
        public static readonly HardwareProfile Gpu;

        static ActiveHardware()
        {
            List<HardwareProfile> catalog = HardwareCatalog.Load();
            AppSettings settings = AppSettings.Load();
            Cpu = Choose(catalog, HardwareKind.Cpu, settings.CpuProfileName, new[] { InstalledHardware.ProcessorName() }) ?? FallbackCpu;
            Gpu = WithResolvedZones(Choose(catalog, HardwareKind.Gpu, settings.GpuProfileName, InstalledHardware.DisplayAdapterNames()) ?? FallbackGpu);
        }

        private static HardwareProfile Choose(List<HardwareProfile> catalog, HardwareKind kind, string configuredName, IEnumerable<string> installedNames)
        {
            if (configuredName != null) return HardwareCatalog.Find(catalog, kind, configuredName);
            return HardwareMatcher.Detect(catalog, kind, installedNames);
        }

        private static HardwareProfile WithResolvedZones(HardwareProfile profile)
        {
            if (!profile.UsesDriverLimits) return profile;
            string description;
            TemperatureZones zones = DriverZones.Resolve(out description);
            return new HardwareProfile(profile.Kind, profile.Name, zones, description);
        }
    }
}
