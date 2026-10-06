namespace DevMonitor
{
    internal sealed class TemperatureZones
    {
        public readonly float Warning;
        public readonly float Critical;

        public TemperatureZones(float warning, float critical)
        {
            Warning = warning;
            Critical = critical;
        }
    }
}
