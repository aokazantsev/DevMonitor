namespace DevMonitor.Setup
{
    internal sealed class GuidePage
    {
        public readonly string Title;
        public readonly string Text;
        public readonly string ImageName;

        public GuidePage(string title, string text, string imageName)
        {
            Title = title;
            Text = text;
            ImageName = imageName;
        }
    }
}
