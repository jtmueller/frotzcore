using Frotz;
using Frotz.Blorb;
using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFMachine.Support;

namespace WPFMachine.Screen
{
    /// <summary>
    /// Interaction logic for WPFScreen.xaml
    /// </summary>
    public partial class TextControlScreen : UserControl, IZScreen, IZMachineScreen
    {
        private readonly Window _parent;

        public TextControlScreen(Window Parent)
        {
            InitializeComponent();

            _parent = Parent;
            Margin = new Thickness(0);

            _parent = Parent;

            _cursorCanvas = new Canvas
            {
                Background = ZColorCheck.ZColorToBrush(1, ColorType.Foreground),
                Visibility = Visibility.Hidden
            };
            cnvsTop.Children.Add(_cursorCanvas);

            _sound = new FrotzSound();
            LayoutRoot.Children.Add(_sound);

            fColor = 1;
            bColor = 1;

            Background = ZColorCheck.ZColorToBrush(1, ColorType.Background);

            _substituion = new NumberSubstitution();

            SetFontInfo();

            Unloaded += (s, e) =>
            {
                _regularLines?.Dispose();
                _fixedWidthLines?.Dispose();
            };
        }

        public void AddInput(char key) => OnKeyPressed(key);

        public event EventHandler<ZKeyPressEventArgs> KeyPressed;
        protected void OnKeyPressed(char key) => KeyPressed?.Invoke(this, new ZKeyPressEventArgs(key));

        public event EventHandler<GameSelectedEventArgs> GameSelected;

        private readonly NumberSubstitution _substituion;
        private ScreenLines _regularLines = null;
        private ScreenLines _fixedWidthLines = null;
        private readonly FrotzSound _sound;
        private readonly int cursorHeight = 2;
        private readonly Canvas _cursorCanvas;
        private double charHeight = 1;
        private double charWidth = 1;
        private Size _actualCharSize = Size.Empty;
        private int _lines = 0;
        private int _chars = 0; /* in fixed font */

        public ScreenMetrics Metrics { get; private set; }

        private int _x = 0;
        private int _y = 0;
        private int _cursorX = 0;
        private int _cursorY = 0;
        private int fColor;
        private int bColor;
        private int scale = 1;
        private FontInfo _regularFont;
        private FontInfo _fixedFont;

        // TODO It might be easier to just grab the h/w in the funciton
        // TODO Find a way to hook this to an event
        public void SetCharsAndLines()
        {
            double height = ActualHeight;
            double width = ActualWidth;

            var fixedFt = BuildFormattedText("A", _fixedFont, true, null, null);
            var propFt = BuildFormattedText("A", _regularFont, true, null, null);

            //double w = fixedFt.Width;
            //double h = fixedFt.Height;

            charHeight = Math.Max(fixedFt.Height, propFt.Height);
            charWidth = fixedFt.Width;

            // Account for the margin of the Rich Text Box
            // TODO Find a way to determine what this should be, or to remove the margin
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
                    // scale = 2; // Ok, so the rest of things are at the right scale, but we've pulled back the images to 1x

                    screenWidth = OS.BlorbFile.StandardSize.Width * scale;
                    screenHeight = OS.BlorbFile.StandardSize.Height * scale;
                }
            }
            else
            {
                scale = 1;
            }

            _actualCharSize = new Size(propFt.Width, propFt.Height);

            _chars = Convert.ToInt32(Math.Floor(screenWidth / charWidth)); // Determine chars based only on fixed width chars since proportional fonts are accounted for as they are written
            _lines = Convert.ToInt32(Math.Floor(screenHeight / charHeight)); // Use the largest character height

            Metrics = new ScreenMetrics(
                new ZSize(charHeight, charWidth),// new ZSize(h, w),
                new ZSize(_lines * charHeight, _chars * charWidth), // The ZMachine wouldn't take screenHeight as round it down, so this takes care of that
                _lines, _chars, scale);

            Conversion.Metrics = Metrics;

            _regularLines = new ScreenLines(Metrics.Rows, Metrics.Columns);
            _fixedWidthLines = new ScreenLines(Metrics.Rows, Metrics.Columns);

            _cursorCanvas.MinHeight = 2;
            _cursorCanvas.MinWidth = charWidth;

            ztc.SetMetrics(Metrics);
        }

        public void SetFontInfo()
        {
            // TODO Should see if this can be moved into the ZTextControl
            ztc.SetFontInfo();

            int font_size = Properties.Settings.Default.FontSize;

            _regularFont = new FontInfo(Properties.Settings.Default.ProportionalFont, font_size);
            _fixedFont = new FontInfo(Properties.Settings.Default.FixedWidthFont, font_size);
        }

        public void HandleFatalError(string Message)
        {
            Dispatcher.Invoke(() =>
            {
                // TODO I'd like this to reference the root window for modality
                MessageBox.Show(_parent, Message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            });

            throw new ZMachineException(Message);
        }

        public ScreenMetrics GetScreenMetrics()
        {
            Dispatcher.Invoke(() =>
            {
                SetCharsAndLines();
            });

            return Metrics;
        }

        private int _lastX;
        private int _lastY;

        public void DisplayChar(char c)
        {
            Dispatcher.Invoke(() =>
            {
                ztc.AddDisplayChar(c);
            });

            if (_inInputMode)
            {
                _lastX = _x;
                _lastY = _y;
            }
        }

        public void SetCursorPosition(int x, int y)
        {
            int prevY = _cursorY;

            _x = x.Tcw();
            _y = y.Tch();

            _cursorX = x;
            _cursorY = y;

            ztc.SetCursorPosition(x, y);
        }

        public void ScrollLines(int top, int height, int numlines) => ztc.ScrollLines(top, height, numlines);

        public void ScrollArea(int top, int bottom, int left, int right, int units) => throw new Exception("Need to handle ScrollArea");// TODO If I scroll an area, need to move the graphics along with it

        protected override void OnRender(System.Windows.Media.DrawingContext dc)
        {
            _cursorCanvas.SetValue(Canvas.TopProperty, _cursorY + (charHeight - cursorHeight));
            _cursorCanvas.SetValue(Canvas.LeftProperty, (double)_cursorX);
        }

        private FormattedText BuildFormattedText(string text, FontInfo font, bool useDisplayMode,
            List<FontChanges> changes, DrawingContext dc)
        {
            var tfm = TextFormattingMode.Display;
            var ft = new FormattedText(text,
                   CultureInfo.CurrentCulture,
                   FlowDirection.LeftToRight,
                   font.Typeface,
                   font.PointSize,
                   ZColorCheck.ZColorToBrush(fColor, ColorType.Foreground),
                   _substituion, tfm, 1.0);

            if (changes != null)
            {
                foreach (var fc in changes)
                {
                    SetStyle(fc.FontAndStyle, fc, ft, dc);
                }
            }

            return ft;
        }

        public void SetStyle(CharDisplayInfo fs, FontChanges fc, FormattedText ft, DrawingContext dc)
        {
            int startPos = fc.Offset + fc.StartCol;

            if ((fs.Style & ZStyles.BOLDFACE_STYLE) > 0)
            {
                ft.SetFontWeight(FontWeights.Bold, startPos, fc.Count);
            }

            int rectColor = -1;
            var type = ColorType.Foreground;

            if ((fs.Style & ZStyles.REVERSE_STYLE) > 0)
            {
                ft.SetFontWeight(FontWeights.Bold, startPos, fc.Count);
                ft.SetForegroundBrush(ZColorCheck.ZColorToBrush(fs.BackgroundColor, ColorType.Background), startPos, fc.Count);

                rectColor = fs.ForegroundColor;
            }
            else
            {
                ft.SetForegroundBrush(ZColorCheck.ZColorToBrush(fs.ForegroundColor, ColorType.Foreground), startPos, fc.Count);
                if (fs.BackgroundColor > 1 && fs.BackgroundColor != bColor)
                {
                    rectColor = fs.BackgroundColor;
                    type = ColorType.Background;
                }
            }

            if ((fs.Style & ZStyles.EMPHASIS_STYLE) > 0)
            {
                ft.SetFontStyle(FontStyles.Italic, startPos, fc.Count);
            }

            if ((fs.Style & ZStyles.FIXED_WIDTH_STYLE) > 0)
            {
                ft.SetFontFamily(_fixedFont.Family, startPos, fc.Count);
            }

            if (dc != null && rectColor != -1)
            {
                var b = ZColorCheck.ZColorToBrush(rectColor, type);

                dc.DrawRectangle(b, null,
                    new Rect(fc.StartCol * charWidth, fc.Line * charHeight,
                        fc.Count * charWidth, charHeight));
            }
        }


        public void RefreshScreen()
        {
            Dispatcher.Invoke(() =>
            {
                InvalidateVisual();

                ztc.Flush();
                ztc.Refresh();
            });
        }

        public void SetTextStyle(int new_style) => ztc.SetTextStyle(new_style);

        public void Clear()
        {
            ztc.Clear();

            Dispatcher.Invoke(() =>
            {
                Background = ZColorCheck.ZColorToBrush(bColor, ColorType.Background);

                for (int i = 0; i < cnvsTop.Children.Count; i++)
                {
                    {
                        if (cnvsTop.Children[i] is Image img)
                        {
                            cnvsTop.Children.RemoveAt(i--);
                        }
                    }

                }
            });
        }

        public void ClearArea(int top, int left, int bottom, int right)
        {
            if (top == 1 && left == 1 && bottom == Metrics.WindowSize.Height && right == Metrics.WindowSize.Width)
            {
                Clear();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Clear area:" + top + ":" + left + ":" + bottom + ":" + right);
            }
        }

        public string OpenExistingFile(string defaultName, string Title, string Filter)
        {
            string name = null;
            Dispatcher.Invoke(() =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = Title,
                    Filter = CreateFilterList(Filter),
                    DefaultExt = ".sav",
                    FileName = defaultName
                };
                if (ofd.ShowDialog(_parent) == true)
                {
                    name = ofd.FileName;
                }
                _parent.Focus(); // HACK For some reason, it won't always pick up text input after the dialog, so this refocuses
            });
            return name;
        }

        public string OpenNewOrExistingFile(string defaultName, string Title, string Filter, string DefaultExtension)
        {
            string name = null;
            Dispatcher.Invoke(() =>
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Choose save game name",
                    FileName = defaultName,

                    Filter = CreateFilterList("Save Files (*.sav)|*.sav"),
                    DefaultExt = ".sav"
                };

                if (sfd.ShowDialog(_parent) == true)
                {
                    name = sfd.FileName;
                }

                _parent.Focus(); // HACK For some reason, it won't always pick up text input after the dialog, so this refocuses
            });
            return name;
        }


        public ZSize GetImageInfo(Span<byte> image)
        {
            using var ms = OS.StreamManger.GetStream("GetImageInfo", image);
            using var img = System.Drawing.Image.FromStream(ms);
            return new ZSize(img.Height * scale, img.Width * scale);
        }

        public void DrawPicture(int picture, byte[] image, int y, int x)
        {
            Dispatcher.Invoke(() =>
            {
                var img = new Image();
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                using var ms = OS.StreamManger.GetStream("DrawPicture", image);
                bi.StreamSource = ms;
                bi.EndInit();
                img.Source = bi;
                cnvsTop.Children.Add(img);

                int newX = x;
                int newY = y;
                // TODO Ok, so when calculating the position of the graphics, it's causing a wrap
                // TODO Find out why, and fix it...

                //
                if (newY > short.MaxValue) newY -= ushort.MaxValue;
                if (newX > short.MaxValue) newX -= ushort.MaxValue;

                img.SetValue(Canvas.TopProperty, (double)newY);
                img.SetValue(Canvas.LeftProperty, (double)newX);
            });
        }

        public void SetFont(int font) => ztc.SetFont(font);

        public void DisplayMessage(string Message, string Caption) => MessageBox.Show(Message, Caption);

        public int GetStringWidth(string s, CharDisplayInfo Font)
        {
            FormattedText ft;
            if (Font.Font == ZFont.FIXED_WIDTH_FONT)
            {
                ft = BuildFormattedText(s, _fixedFont, true, null, null);
            }
            else
            {
                ft = BuildFormattedText(s, _regularFont, true, null, null);
            }

            return (int)ft.WidthIncludingTrailingWhitespace;
        }

        public bool GetFontData(int font, ref ushort height, ref ushort width)
        {
            switch (font)
            {
                case ZFont.TEXT_FONT:
                case ZFont.FIXED_WIDTH_FONT:
                case ZFont.GRAPHICS_FONT:
                    height = (ushort)Metrics.FontSize.Height;
                    width = (ushort)Metrics.FontSize.Width;
                    return true;
                case ZFont.PICTURE_FONT:
                    return false;
            }

            return false;
        }

        public void SetColor(int new_foreground, int new_background)
        {
            fColor = new_foreground;
            bColor = new_background;

            // ZColorCheck.setDefaults(new_foreground, new_background);
            ztc.SetColor(new_foreground, new_background);
        }

        public void RemoveChars(int count)
        {

            Dispatcher.Invoke(() =>
            {
                if (_inInputMode)
                {
                    ztc.RemoveInputChars(count);
                }
                else
                {
                    ztc.RemoveInputChars(count);
                    // HandleFatalError("Need to handle case where RemoveChars is called outside of input mode");
                }
            });
        }

        public void AddInputChar(char c)
        {
            Dispatcher.Invoke(() =>
            {
                ztc.AddInputChar(c);
            });
        }

        public ushort PeekColor()
        {
            var f = _regularLines.GetFontAndStyle(_x, _y);
            return (ushort)f.BackgroundColor;
        }


        public void GetColor(out int foreground, out int background)
        {
            foreground = fColor;
            background = bColor;
        }

        public void PrepareSample(int number)
        {
            if (OS.BlorbFile != null)
            {
                _sound.LoadSound(OS.BlorbFile.Sounds[number]);
            }
        }

        public void StartSample(int number, int volume, int repeats, ushort eos)
        {
            Dispatcher.Invoke(() =>
            {
                _sound.LoadSound(OS.BlorbFile.Sounds[number]);
                _sound.PlaySound();
            });
        }

        public void FinishWithSample(int number)
        {
        }

        public void StopSample(int number)
        {
        }

        public void SetInputColor()
        {
            // 32 means use the default input color
            SetColor(32, bColor);
        }

        private string CreateFilterList(params string[] types)
        {
            var temp = new List<string>(types)
            {
                "All Files (*.*)|*.*"
            };

            return string.Join('|', temp.ToArray());
        }

        private bool _inInputMode = false;
        public void SetInputMode(bool inputMode, bool cursorVisible)
        {
            if (_inInputMode != inputMode)
            {
                _inInputMode = inputMode;

                Dispatcher.Invoke(() =>
                {
                    if (_inInputMode == true)
                    {
                        int x = ztc.StartInputMode();
                        if (_cursorX <= 1 && x > -1)
                        {
                            _cursorX = x + 2; // Move the cursor over 2 pixels to account for margin
                            _cursorCanvas.SetValue(Canvas.LeftProperty, (double)_cursorX);
                        }
                    }
                    else
                    {
                        ztc.EndInputMode();
                    }

                    _cursorCanvas.Visibility = cursorVisible ? Visibility.Visible : Visibility.Hidden;
                });
            }
        }

#nullable enable
        public void StoryStarted(string storyFileName, Blorb? blorbFile)
        {
            Dispatcher.Invoke(() =>
            {
                _parent.Title = OS.BlorbFile != null
                    ? $"FrotzCore - {OS.BlorbFile.StoryName}"
                    : $"FrotzCore - {storyFileName}";

                GameSelected?.Invoke(this, new GameSelectedEventArgs(storyFileName, blorbFile));
            });
        }
#nullable disable

        public ZPoint GetCursorPosition()
        {
            ZPoint p = default;
            Dispatcher.Invoke(() =>
            {
                p = new ZPoint(_cursorX, _cursorY);
            });
            return p;
        }

        public new void Focus() => base.Focus();

#nullable enable
        public (string FileName, byte[] FileData)? SelectGameFile()
        {
            byte[]? buffer = null;
            string? fName = null;
            Dispatcher.Invoke(() =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open a Z-Code file",
                    DefaultExt = ".dat",

                    Filter = CreateFilterList(
                     "Infocom Blorb File (*.zblorb)|*.zblorb",
                     "Infocom Games (*.dat)|*.dat",
                     "Z-Code Files (*.z?)|*.z?",
                     "Blorb File (*.blorb)|*.blorb")
                };


                if (ofd.ShowDialog(_parent) == true)
                {
                    fName = ofd.FileName;
                    var s = ofd.OpenFile();
                    buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Close();
                }

            });
            
            if (fName != null && buffer != null)
            {
                return (fName, buffer);
            }
            return null;
        }
#nullable disable

        public void Reset() => Clear();


        public void SetActiveWindow(int win)
        {
        }

        public void SetWindowSize(int win, int top, int left, int height, int width)
        {
        }

        void IZMachineScreen.AddInput(char InputKeyPressed) => throw new NotImplementedException();

        void IZMachineScreen.SetCharsAndLines() => throw new NotImplementedException();

        ScreenMetrics IZMachineScreen.Metrics => throw new NotImplementedException();

        void IZMachineScreen.SetFontInfo() => throw new NotImplementedException();

        void IZMachineScreen.Focus() => throw new NotImplementedException();

        event EventHandler<GameSelectedEventArgs> IZMachineScreen.GameSelected
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public bool ShouldWrap() => true;

    }
}
