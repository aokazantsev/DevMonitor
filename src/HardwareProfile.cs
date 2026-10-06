namespace DevMonitor
{
    internal sealed class HardwareProfile
    {
        public readonly HardwareKind Kind;
        public readonly string Name;
        public readonly TemperatureZones Zones;
        public readonly string Note;

        public HardwareProfile(HardwareKind kind, string name, TemperatureZones zones, string note)
        {
            Kind = kind;
            Name = name;
            Zones = zones;
            Note = note;
        }

        public bool UsesDriverLimits
        {
            get { return Zones == null; }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
