
using System.Windows.Documents;

namespace WPFMachine.Screen
{
    internal class ZBlankContainer : InlineUIContainer
    {
        internal ZBlankContainer(int width)
        {
            Width = width;
            var c = new System.Windows.Controls.Canvas
            {
                Width = width
            };

            Child = c;
        }

        internal int Width { get; private set; }
    }
}
