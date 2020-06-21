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

        #region Image Extensions

        internal static double Top(this Image img) => (double)img.GetValue(Canvas.TopProperty);

        internal static void Top(this Image img, double value) => img.SetValue(Canvas.TopProperty, value);

        internal static double Left(this Image img) => (double)img.GetValue(Canvas.LeftProperty);

        internal static void Left(this Image img, double value) => img.SetValue(Canvas.LeftProperty, value);

        internal static double Right(this Image img) => (double)img.GetValue(Canvas.RightProperty);

        internal static void Right(this Image img, double value) => img.SetValue(Canvas.RightProperty, value);

        internal static double Bottom(this Image img) => (double)img.GetValue(Canvas.BottomProperty);

        internal static void Bottom(this Image img, double value) => img.SetValue(Canvas.BottomProperty, value);

        internal static double Width(this Image img) => (double)img.GetValue(Canvas.WidthProperty);

        internal static void Width(this Image img, double value) => img.SetValue(Canvas.WidthProperty, value);

        internal static double Height(this Image img) => (double)img.GetValue(Canvas.HeightProperty);

        internal static void Height(this Image img, double value) => img.SetValue(Canvas.WidthProperty, value);

        #endregion

        #region Canvas Extensions

        internal static double Top(this Canvas canvas) => (double)canvas.GetValue(Canvas.TopProperty);

        internal static void Top(this Canvas canvas, double value) => canvas.SetValue(Canvas.TopProperty, value);

        internal static double Left(this Canvas canvas) => (double)canvas.GetValue(Canvas.LeftProperty);

        internal static void Left(this Canvas canvas, double value) => canvas.SetValue(Canvas.LeftProperty, value);

        internal static double Right(this Canvas canvas) => (double)canvas.GetValue(Canvas.RightProperty);

        internal static void Right(this Canvas canvas, double value) => canvas.SetValue(Canvas.RightProperty, value);

        internal static double Bottom(this Canvas canvas) => (double)canvas.GetValue(Canvas.BottomProperty);

        internal static void Bottom(this Canvas canvas, double value) => canvas.SetValue(Canvas.BottomProperty, value);

        internal static double Width(this Canvas canvas) => (double)canvas.GetValue(Canvas.WidthProperty);

        internal static void Width(this Canvas canvas, double value) => canvas.SetValue(Canvas.WidthProperty, value);

        internal static double Height(this Canvas canvas) => (double)canvas.GetValue(Canvas.HeightProperty);

        internal static void Height(this Canvas canvas, double value) => canvas.SetValue(Canvas.WidthProperty, value);

        #endregion
    }
}
