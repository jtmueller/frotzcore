using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFMachine.RTBSubclasses
{
    public class ZRun : Run
    {
        public ZRun(FontFamily Family)
            : this(Family, "")
        { }

        public ZRun(FontFamily Family, string Text)
            : base(Text)
        {
            FontStyle = FontStyles.Normal;
            FontWeight = FontWeights.Normal;
            FontFamily = Family;
        }

        private double? _width = null;
        public double Width
        {
            get
            {
                if (_width == null)
                {
                    _width = DetermineWidth();
                }
                return (double)_width;
            }
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
                    OnWidthChanged();
                    base.FontStyle = value;
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
                    OnWidthChanged();
                    base.FontWeight = value;
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
                    OnWidthChanged();
                    base.Text = value;
                }
            }
        }

        private double DetermineWidth()
        {
            var ns = new NumberSubstitution();
            var ft = new FormattedText(Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize, Foreground, ns, TextFormattingMode.Display, 1.0);

            return ft.Width;
        }
    }
}
