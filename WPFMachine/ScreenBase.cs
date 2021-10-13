using Frotz;
using Frotz.Constants;
using Frotz.Screen;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFMachine.Support;

namespace WPFMachine;

public abstract class ScreenBase : UserControl, IZMachineScreen
{
    protected ScreenBase()
    {
        _dpi = new(() => VisualTreeHelper.GetDpi(this));
    }

    #region ZMachineScreen Members

    public void AddInput(char InputKeyPressed) => OnKeyPressed(InputKeyPressed);

    protected void OnKeyPressed(char key) => KeyPressed?.Invoke(this, new ZKeyPressEventArgs(key));

    public event EventHandler<ZKeyPressEventArgs> KeyPressed;

    protected int scale = 1;

    protected double charWidth = 1; // TODO This should probably be an int
    protected double charHeight = 1; // TODO Same here

    protected ScreenMetrics _metrics;

    public ScreenMetrics Metrics => _metrics;

    private readonly Lazy<DpiScale> _dpi;

    protected FontInfo _regularFont;
    protected FontInfo _fixedFont;
    protected Lazy<FontInfo> _beyZorkFont;

    protected CharDisplayInfo _currentInfo;

    protected Window _parent;

    protected Size ActualCharSize = Size.Empty;
    protected int lines = 0;
    protected int chars = 0; /* in fixed font */

    protected ScreenLines _regularLines = null; // TODO Are these even used now?
    protected ScreenLines _fixedWidthLines = null;

    protected NumberSubstitution _substituion = new();

    public void SetCharsAndLines()
    {
        double height = ActualHeight;
        double width = ActualWidth;

        var fixedFt = BuildFormattedText("A", _fixedFont, _currentInfo);
        var propFt = BuildFormattedText("A", _regularFont, _currentInfo);

        charHeight = Math.Max(fixedFt.Height, propFt.Height);
        charWidth = fixedFt.Width;

        double screenWidth = width - 20;
        double screenHeight = height - 20;

        if (OS.BlorbFile != null)
        {
            var standard = OS.BlorbFile.StandardSize;
            if (standard.Height > 0 && standard.Width > 0)
            {
                int maxW = (int)Math.Floor(width / OS.BlorbFile.StandardSize.Width);
                int maxH = (int)Math.Floor(height / OS.BlorbFile.StandardSize.Height);

                scale = Math.Min(maxW, maxH);

                screenWidth = OS.BlorbFile.StandardSize.Width * scale;
                screenHeight = OS.BlorbFile.StandardSize.Height * scale;

                double heightDiff = _parent.ActualHeight - ActualHeight;
                double widthDiff = _parent.ActualWidth - ActualWidth;

                _parent.Height = screenHeight + heightDiff;
                _parent.Width = screenWidth + widthDiff;
            }
            else
            {
                scale = 1;
            }
        }
        else
        {
            scale = 1;
        }

        ActualCharSize = new Size(propFt.Width, propFt.Height);

        chars = Convert.ToInt32(Math.Floor(screenWidth / charWidth)); // Determine chars based only on fixed width chars since proportional fonts are accounted for as they are written
        lines = Convert.ToInt32(Math.Floor(screenHeight / charHeight)); // Use the largest character height

        _metrics = new ScreenMetrics(
            new ZSize(charHeight, charWidth),// new ZSize(h, w),
            new ZSize(lines * charHeight, chars * charWidth), // The ZMachine wouldn't take screenHeight as round it down, so this takes care of that
            lines, chars, scale);

        _regularLines = new ScreenLines(_metrics.Rows, _metrics.Columns);
        _fixedWidthLines = new ScreenLines(_metrics.Rows, _metrics.Columns);

        AfterSetCharsAndLines();
    }

    protected abstract void AfterSetCharsAndLines();

    protected FormattedText BuildFormattedText(string text, FontInfo font, CharDisplayInfo cdi)
    {
        var ft = new FormattedText(text, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, font.Typeface, font.PointSize,
            ZColorCheck.ZColorToBrush(cdi.ForegroundColor, ColorType.Foreground),
            _substituion, TextFormattingMode.Ideal, _dpi.Value.PixelsPerDip);

        SetStyle(cdi, ft);

        return ft;
    }

    public void SetStyle(CharDisplayInfo fs, FormattedText ft)
    {
        if ((fs.Style & ZStyles.BOLDFACE_STYLE) > 0)
        {
            ft.SetFontWeight(FontWeights.Bold);
        }

        if ((fs.Style & ZStyles.REVERSE_STYLE) > 0)
        {
            ft.SetFontWeight(FontWeights.Bold);
            ft.SetForegroundBrush(ZColorCheck.ZColorToBrush(fs.BackgroundColor, ColorType.Background));
        }
        else
        {
            ft.SetForegroundBrush(ZColorCheck.ZColorToBrush(fs.ForegroundColor, ColorType.Foreground));
        }

        if ((fs.Style & ZStyles.EMPHASIS_STYLE) > 0)
        {
            ft.SetFontStyle(FontStyles.Italic);
        }

        if ((fs.Style & ZStyles.FIXED_WIDTH_STYLE) > 0)
        {
            ft.SetFontFamily(_fixedFont.Family);
        }
    }

    public void SetFontInfo()
    {
        int font_size = Properties.Settings.Default.FontSize;

        _regularFont = new FontInfo(Properties.Settings.Default.ProportionalFont, font_size);
        _fixedFont = new FontInfo(Properties.Settings.Default.FixedWidthFont, font_size);
        _beyZorkFont = new Lazy<FontInfo>(() => new FontInfo("BEYZORK", font_size, new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/beyzork.fon")));
    }

    public new void Focus() => base.Focus();

    public event EventHandler<GameSelectedEventArgs> GameSelected;

    public void Reset() => DoReset();

    protected abstract void DoReset();

    protected void OnStoryStarted(GameSelectedEventArgs e) => GameSelected?.Invoke(this, e);

    public ZSize GetImageInfo(byte[] image)
    {
        using var ms = new MemoryStream(image);
        using var img = System.Drawing.Image.FromStream(ms);
        return new ZSize(img.Height * scale, img.Width * scale);
    }

    // TODO This does the same thing (blurry) and doesn't retain the transparent
    public static byte[] ScaleImage(int scale, byte[] image)
    {
        using var imgMs = new MemoryStream(image);
        using var img = System.Drawing.Image.FromStream(imgMs);
        using var bmp = new System.Drawing.Bitmap(img.Width * scale, img.Height * scale);
        using var graphics = System.Drawing.Graphics.FromImage(bmp);
        graphics.DrawImage(img, 0, 0, img.Width * scale, img.Height * scale);
        using var ms = OS.StreamManger.GetStream("ScreenBase.ScaleImage");

        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

        return ms.ToArray();
    }

    #endregion
}
