
using System.Windows.Documents;

namespace WPFMachine.Screen
{
    internal class ZBlankContainer : InlineUIContainer
    {
        internal ZBlankContainer(int width)
        {
            Width = width;
            Child = new System.Windows.Controls.Canvas
            {
                Width = width
            };
        }

        internal int Width { get; private set; }
    }
}
