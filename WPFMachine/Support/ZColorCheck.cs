
using Frotz.Constants;
using Frotz.Other;
using System.Collections.Concurrent;
using System.Windows.Media;
using WPFMachine.Support;

namespace WPFMachine
{
    public class ZColorCheck
    {
        private static Color C64Blue = Color.FromRgb(66, 66, 231);

        public int ColorCode { get; set; }
        public ColorType Type { get; set; }


        public ZColorCheck(int color, ColorType colorType)
        {
            ColorCode = color;
            Type = colorType;
        }

        public bool AreSameColor(ZColorCheck ColorToCompare)
        {
            if (ColorToCompare == null) return false;

            if (ColorToCompare.ColorCode == 0 || ColorCode == 0 && Type == ColorToCompare.Type) return true;

            return ColorToCompare.ColorCode == ColorCode && ColorToCompare.Type == Type;
        }

        internal Brush ToBrush() => ZColorToBrush(ColorCode, Type);

        internal Color ToColor() => ZColorToColor(ColorCode, Type);

        static ZColorCheck()
        {
            ResetDefaults();
        }

        internal static void ResetDefaults()
        {
            CurrentForeColor = Properties.Settings.Default.DefaultForeColor;
            CurrentBackColor = Properties.Settings.Default.DefaultBackColor;
        }

        internal static void SetDefaults(int fore_color, int back_color)
        {
            if (fore_color > 1)
            {
                CurrentForeColor = ZColorToColor(fore_color, ColorType.Foreground);
            }

            if (back_color > 1)
            {
                CurrentBackColor = ZColorToColor(back_color, ColorType.Background);
            }
        }

        internal static Color CurrentForeColor { get; set; }
        internal static Color CurrentBackColor { get; set; }

        internal static Brush ZColorToBrush(int color, ColorType type) => new SolidColorBrush(ZColorToColor(color, type));

        private static readonly ConcurrentDictionary<(int color, ColorType type), Color> s_colorCache = new ConcurrentDictionary<(int, ColorType), Color>();

        internal static Color ZColorToColor(int color, ColorType type) =>
            s_colorCache.GetOrAdd((color, type), ZColorToColor);

        private static Color ZColorToColor((int, ColorType) ct)
        {
            var (color, type) = ct;
            if (color == 0 || color == 1)
            {
                if (type == ColorType.Foreground) return CurrentForeColor;
                if (type == ColorType.Background) return CurrentBackColor;
            }

            switch (color)
            {
                case ZColor.BLACK_COLOUR:
                    return Colors.Black;
                case ZColor.BLUE_COLOUR:
                    return C64Blue;
                case ZColor.CYAN_COLOUR:
                    return Colors.Cyan;
                case ZColor.DARKGREY_COLOUR:
                    return Colors.DarkGray;
                case ZColor.GREEN_COLOUR:
                    return Colors.Green;
                // case ZColor.LIGHTGREY_COLOUR: // Light Grey & Grey both equal 10
                case ZColor.GREY_COLOUR:
                    return Colors.Gray;
                case ZColor.MAGENTA_COLOUR:
                    return Colors.Magenta;
                case ZColor.MEDIUMGREY_COLOUR:
                    return Colors.DimGray;
                case ZColor.RED_COLOUR:
                    return Colors.Red;
                case ZColor.TRANSPARENT_COLOUR:
                    return Colors.Transparent;
                case ZColor.WHITE_COLOUR:
                    return Colors.White;
                case ZColor.YELLOW_COLOUR:
                    return Colors.Yellow;
                case 32:
                    return Properties.Settings.Default.DefaultInputColor;
            }

            long new_color = TrueColorStuff.GetColor(color);
            var (r, g, b) = TrueColorStuff.GetRGB(new_color);

            return Color.FromRgb(r, g, b);
        }
    }
}
