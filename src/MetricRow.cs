namespace DevMonitor
{
    internal sealed class MetricRow
    {
        public readonly string Label;
        public readonly ValueSegment[] Segments;

        public MetricRow(string label, params ValueSegment[] segments)
        {
            Label = label;
            Segments = segments;
        }
    }
}
