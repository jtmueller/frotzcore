using Frotz.Constants;
using Frotz.Other;
using System.Collections.Concurrent;
using System.Windows.Media;
using WPFMachine.Support;

namespace WPFMachine;

public class ZColorCheck
{
    private static readonly Color C64Blue = Color.FromRgb(0x42, 0x42, 0xE7);

    public int ColorCode { get; set; }
    public ColorType Type { get; set; }

    public ZColorCheck(int color, ColorType colorType)
    {
        ColorCode = color;
        Type = colorType;
    }

    public bool AreSameColor(ZColorCheck colorToCompare)
    {
        if (colorToCompare == null) return false;

        if (colorToCompare.ColorCode == 0 || ColorCode == 0 && Type == colorToCompare.Type) return true;

        return colorToCompare.ColorCode == ColorCode && colorToCompare.Type == Type;
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

    private static readonly ConcurrentDictionary<(int color, ColorType type), Color> s_colorCache = new();

    internal static Color ZColorToColor(int color, ColorType type) =>
        s_colorCache.GetOrAdd((color, type), ZColorToColor);

    private static Color ZColorToColor((int, ColorType) ct)
    {
        var (color, type) = ct;
        if (color is 0 or 1)
        {
            if (type == ColorType.Foreground) return CurrentForeColor;
            if (type == ColorType.Background) return CurrentBackColor;
        }

        return color switch
        {
            ZColor.BLACK_COLOUR => Colors.Black,
            ZColor.BLUE_COLOUR => C64Blue,
            ZColor.CYAN_COLOUR => Colors.Cyan,
            ZColor.DARKGREY_COLOUR => Colors.DarkGray,
            ZColor.GREEN_COLOUR => Colors.Green,
            // case ZColor.LIGHTGREY_COLOUR: // Light Grey & Grey both equal 10
            ZColor.GREY_COLOUR => Colors.Gray,
            ZColor.MAGENTA_COLOUR => Colors.Magenta,
            ZColor.MEDIUMGREY_COLOUR => Colors.DimGray,
            ZColor.RED_COLOUR => Colors.Red,
            ZColor.TRANSPARENT_COLOUR => Colors.Transparent,
            ZColor.WHITE_COLOUR => Colors.White,
            ZColor.YELLOW_COLOUR => Colors.Yellow,
            32 => Properties.Settings.Default.DefaultInputColor,
            _ => ParseColor(color),
        };

        static Color ParseColor(int color)
        {
            int new_color = TrueColorStuff.GetColor(color);
            var (r, g, b) = TrueColorStuff.GetRGB(new_color);
            return Color.FromRgb(r, g, b);
        }
    }
}
