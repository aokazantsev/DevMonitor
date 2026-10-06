namespace DevMonitor
{
    internal sealed class RunningStat
    {
        private double sum;
        private int count;
        private float max;

        public int Count
        {
            get { return count; }
        }

        public float? Average
        {
            get { return count == 0 ? (float?)null : (float)(sum / count); }
        }

        public float? Max
        {
            get { return count == 0 ? (float?)null : max; }
        }

        public void Add(float? value)
        {
            if (!value.HasValue) return;
            sum += value.Value;
            if (count == 0 || value.Value > max) max = value.Value;
            count++;
        }

        public void Reset()
        {
            sum = 0;
            count = 0;
            max = 0;
        }
    }
}
