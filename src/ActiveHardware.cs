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

        public static HardwareProfile Cpu { get; private set; }
        public static HardwareProfile Gpu { get; private set; }

        private static HardwareProfile selectedGpu;

        public static bool IsManualChoice { get; private set; }

        public static bool IsCpuInCatalog
        {
            get { return Cpu != FallbackCpu; }
        }

        public static bool IsGpuInCatalog
        {
            get { return selectedGpu != FallbackGpu; }
        }

        public static void Select(AppSettings settings)
        {
            List<HardwareProfile> catalog = HardwareCatalog.Load();
            Cpu = Choose(catalog, HardwareKind.Cpu, settings.CpuProfileName, new[] { InstalledHardware.ProcessorName() }) ?? FallbackCpu;
            selectedGpu = Choose(catalog, HardwareKind.Gpu, settings.GpuProfileName, InstalledHardware.DisplayAdapterNames()) ?? FallbackGpu;
            Gpu = WithResolvedZones(selectedGpu);
            IsManualChoice = settings.CpuProfileName != null || settings.GpuProfileName != null;
            AppLog.Append("hardware: cpu=" + Cpu.Name + " (" + (settings.CpuProfileName == null ? "detected" : "from settings") + ")"
                + ", gpu=" + Gpu.Name + " (" + (settings.GpuProfileName == null ? "detected" : "from settings") + ")");
        }

        public static void RefreshGpuZones()
        {
            if (!selectedGpu.UsesDriverLimits) return;
            Gpu = WithResolvedZones(selectedGpu);
            AppLog.Append("gpu zones refreshed: " + Gpu.Note);
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
