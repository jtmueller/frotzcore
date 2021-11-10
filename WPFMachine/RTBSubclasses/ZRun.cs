using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFMachine.RTBSubclasses;

public class ZRun : Run
{
    public ZRun(FontFamily family) : this(family, "")
    { }

    public ZRun(FontFamily family, string text) : base(text)
    {
        FontStyle = FontStyles.Normal;
        FontWeight = FontWeights.Normal;
        FontFamily = family;
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

    internal event EventHandler WidthChanged;
    protected void OnWidthChanged()
    {
        _width = null;
        WidthChanged?.Invoke(this, EventArgs.Empty);
    }

    public new FontStyle FontStyle
    {
        get => base.FontStyle;
        set
        {
            if (base.FontStyle != value)
            {
                base.FontStyle = value;
                OnWidthChanged();

            }
        }
    }

    public new FontWeight FontWeight
    {
        get => base.FontWeight;
        set
        {
            if (base.FontWeight != value)
            {
                base.FontWeight = value;
                OnWidthChanged();
            }
        }
    }

    public new string Text
    {
        get => base.Text;
        set
        {
            if (base.Text != value)
            {
                base.Text = value;
                OnWidthChanged();
            }
        }
    }
}
