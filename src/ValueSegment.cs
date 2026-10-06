namespace DevMonitor
{
    internal sealed class ValueSegment
    {
        public readonly string Text;
        public readonly Severity Severity;

        public ValueSegment(string text, Severity severity)
        {
            Text = text;
            Severity = severity;
        }
    }
}
