using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFMachine.Screen
{
    // TODO This may be so I can overlay different text
    internal class OverlayAdorner : Adorner
    {
        private readonly List<AbsoluteText> _text = new();

        internal int FontHeight { get; set; }

        internal FontInfo RegularFont { get; set; }
        internal FontInfo FixedWidthFont { get; set; }


        // Be sure to call the base class constructor.
        public OverlayAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            FontHeight = 0;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            lock (_text)
            {
                if (RegularFont == null || FixedWidthFont == null) return;
                foreach (var at in _text)
                {

                    var f = RegularFont;
                    if (at.DisplayInfo.Font == ZFont.FIXED_WIDTH_FONT || at.DisplayInfo.ImplementsStyle(ZStyles.FIXED_WIDTH_STYLE))
                    {
                        f = FixedWidthFont;
                    }
                    var b = ZColorCheck.ZColorToBrush(at.DisplayInfo.ForegroundColor, Support.ColorType.Foreground);
                    var ft = new FormattedText(at.Text,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, f.Typeface, f.PointSize, b,
                        new NumberSubstitution(), TextFormattingMode.Display, VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    if (at.DisplayInfo.ImplementsStyle(ZStyles.REVERSE_STYLE))
                    {
                        drawingContext.DrawRectangle(b, null, new Rect(at.X + 2, at.Y + 2, ft.WidthIncludingTrailingWhitespace, Math.Max(ft.Height, FontHeight)));
                        ft.SetForegroundBrush(ZColorCheck.ZColorToBrush(at.DisplayInfo.BackgroundColor, Support.ColorType.Background));
                    }
                    drawingContext.DrawText(ft, new Point(at.X + 2, at.Y + 2));
                    // Note: Offsetting positions by 2 to get everything to line up correctly
                }
            }
        }

        // TODO Maybe just make this take the AbsoluteText object
        internal void AddAbsolute(string text, int y, int x, CharDisplayInfo displayInfo)
        {
            lock (_text)
            {
                _text.Add(new AbsoluteText(text, y, x, displayInfo));
            }
            Refresh();
        }

        internal void ScrollLines(int top, int height, int numlines)
        {
            lock (_text)
            {
                for (int i = 0; i < _text.Count; i++)
                {
                    var at = _text[i];
                    if (at.Y >= top && at.Y < top + height)
                    {
                        at.Y -= numlines;
                        if (at.Y < top)
                        {
                            _text.Remove(at);
                            i--;
                        }
                    }
                }
            }
            Refresh();
        }

        internal void Clear()
        {
            lock (_text)
            {
                _text.Clear();
            }
            Refresh();
        }

        private void Refresh()
        {
            Dispatcher.Invoke(() =>
            {
                InvalidateVisual();
            });
        }

        internal class AbsoluteText
        {
            internal string Text { get; set; }
            internal int X { get; set; }
            internal int Y { get; set; }
            internal CharDisplayInfo DisplayInfo { get; set; }

            internal AbsoluteText(string text, int y, int x, CharDisplayInfo displayInfo)
            {
                Text = text;
                X = x;
                Y = y;
                DisplayInfo = displayInfo;
            }

        }
    }
}

