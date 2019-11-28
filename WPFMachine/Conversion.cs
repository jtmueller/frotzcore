using Frotz.Screen;
using System;
using System.Windows.Controls;

namespace WPFMachine
{
    internal static class Conversion
    {
        internal static int Tcw(this int w) => w == 1 ? 0 : w / Metrics.FontSize.Width;

        internal static int Tch(this int h) => h == 1 ? 0 : h / Metrics.FontSize.Height;

        // TODO I won't need this
        internal static int Tsw(this int w) => w == 1 ? 0 : w;

        internal static int Tsh(this int h) => h == 1 ? 0 : h;

        // TODO FIgure out how to make this easier to call
        internal static int Tch(this int h, int min) => Math.Max(min, h / Metrics.FontSize.Height);

        internal static ScreenMetrics Metrics { get; set; }

        internal static double Top(this Image img) => (double)img.GetValue(Canvas.TopProperty);

        internal static double Left(this Image img) => (double)img.GetValue(Canvas.LeftProperty);

        internal static double Right(this Image img) => (double)img.GetValue(Canvas.RightProperty);

        internal static double Bottom(this Image img) => (double)img.GetValue(Canvas.BottomProperty);
    }
}
