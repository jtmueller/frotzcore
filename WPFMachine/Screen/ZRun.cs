using Frotz.Screen;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFMachine.Screen
{
    internal class ZRun : Run
    {
        internal CharDisplayInfo DisplayInfo { get; private set; }

        internal ZRun(CharDisplayInfo DisplayInfo)
        {
            this.DisplayInfo = DisplayInfo;
        }

        private double? _width = null;
        public double Width => _width ?? (double)(_width = DetermineWidth());

        internal double DetermineWidth() => DetermineWidth(Text);

        internal double DetermineWidth(string text)
        {
            var ns = new NumberSubstitution();
            var ft = new FormattedText(text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize, Foreground, ns, TextFormattingMode.Display, 1.0);

            return ft.Width;
        }
    }
}
