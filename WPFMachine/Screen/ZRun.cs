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

        internal ZRun(CharDisplayInfo displayInfo)
        {
            DisplayInfo = displayInfo;
        }

        private double? _width;

        public double DetermineWidth(double pixelsPerDip)
        {
            if (_width.HasValue)
                return _width.GetValueOrDefault();

            _width = DetermineWidth(Text, pixelsPerDip);
            return _width.GetValueOrDefault();
        }

        private double DetermineWidth(string text, double pixelsPerDip)
        {
            var ft = new FormattedText(text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize, Foreground, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip);

            return ft.Width;
        }
    }
}
