using Frotz;
using Frotz.Blorb;
using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFMachine.Support;

namespace WPFMachine.Absolute
{
    /// <summary>
    /// Interaction logic for Screen2.xaml
    /// </summary>
    public partial class AbsoluteScreen : ScreenBase, IZScreen
    {
        private readonly StringBuilder _currentText = new();
        private int _activeWindow = -1;

        public ScrollbackArea Scrollback { get; }


        public AbsoluteScreen(Window parent) : base()
        {
            InitializeComponent();

            _parent = parent;

            Scrollback = new ScrollbackArea(this);

            _cursorCanvas = new Canvas
            {
                Background = ZColorCheck.ZColorToBrush(1, ColorType.Foreground),
                Visibility = Visibility.Visible
            };
            cnvsTop.Children.Add(_cursorCanvas);

            _sound = new FrotzSound();
            LayoutRoot.Children.Add(_sound);


            _substituion = new NumberSubstitution();

            SetFontInfo();

            _currentInfo = new CharDisplayInfo(1, 0, 1, 1);
            _bColor = 1;
            Background = ZColorCheck.ZColorToBrush(_bColor, ColorType.Background);

            MouseDown += AbsoluteScreen_MouseDown;
            MouseDoubleClick += AbsoluteScreen_MouseDoubleClick;
        }

        private void OnMouseMove(MouseButtonEventArgs e, ushort mouseEvent)
        {
            var p = e.GetPosition(this);
            OS.MouseMoved((ushort)p.X, (ushort)p.Y);
            AddInput((char)mouseEvent);
        }

        private void AbsoluteScreen_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OnMouseMove(e, CharCodes.ZC_DOUBLE_CLICK);

        private void AbsoluteScreen_MouseDown(object sender, MouseButtonEventArgs e) => OnMouseMove(e, CharCodes.ZC_SINGLE_CLICK);

        //private readonly Dictionary<char, string> _graphicsChars = new Dictionary<char, string>();

        public void DisplayChar(char c)
        {
            _currentText.Append(_currentInfo.Font == ZFont.GRAPHICS_FONT ? Frotz.Other.GraphicsFont.GetChar(c) : c);

    //        if (_currentInfo.Font == ZFont.GRAPHICS_FONT)
    //        {
    //            if (_cursorX <= _lastDrawn.X)
    //            {
    //                _cursorX += (int)_lastDrawn.Width;
    //            }

    //            Invoke(() =>
    //            {
    //                if (!_graphicsChars.TryGetValue(c, out string lines))
    //                {
    //                    var (width, height) = _metrics.FontSize;
    //                    string lineData = Frotz.Other.GraphicsFont.GetLines(c);
    //                    var sb = new StringBuilder(1000);
    //                    sb.AppendFormat(
    //@"<Image xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" Width=""{0}"" Height=""{0}"" Stretch=""None"">
    //    <Image.Source>
    //        <DrawingImage>
    //            <DrawingImage.Drawing>
    //                <GeometryDrawing Geometry=""", width);

    //                    for (int y = 0; y < 8; y++)
    //                    {
    //                        int flag = int.Parse(lineData.AsSpan(y * 2, 2), NumberStyles.HexNumber);
    //                        for (int x = 0; x < 8; x++)
    //                        {
    //                            int toggled = (flag >> x) & 1;
    //                            if (toggled == 1)
    //                            {
    //                                //sb.AppendFormat(" <Line X1=\"{0}\" Y1=\"{1}\" X2=\"{2}\" Y2=\"{3}\" Stroke=\"White\" StrokeThickness=\"1\" />\r\n",
    //                                //    j, i, j + 1, i + 1);
    //                                sb.AppendFormat("M {0},{1} L {2},{3} ", x, y, x + 1, y);
    //                            }
    //                        }
    //                    }

    //                    sb.AppendFormat(@""">
    //                    <GeometryDrawing.Pen>
    //                        <Pen Brush=""{0}"" Thickness=""1"" />
    //                    </GeometryDrawing.Pen>
    //                </GeometryDrawing>
    //            </DrawingImage.Drawing>
    //        </DrawingImage>
    //    </Image.Source>
    //</Image>", ZColorCheck.ZColorToColor(_currentInfo.ForegroundColor, ColorType.Foreground));

    //                    lines = sb.ToString();
    //                    _graphicsChars.Add(c, lines);
    //                }

    //                var img = (Image)System.Windows.Markup.XamlReader.Parse(lines);

    //                var cnvs = new Canvas();
    //                cnvs.Children.Add(img);

    //                img.SnapsToDevicePixels = true;
    //                // img.Stretch = Stretch.Uniform;

    //                cnvs.Top(_cursorY);
    //                cnvs.Left(_cursorX);
    //                cnvs.Right(_cursorX + _metrics.FontSize.Width);
    //                cnvs.Bottom(_cursorY + _metrics.FontSize.Height))

    //                _cursorX += _metrics.FontSize.Width;

    //                mainCanvas.Children.Add(cnvs);

    //            });
    //        }
    //        else
    //        {
    //            _currentText.Append(c);
    //        }
        }

        public void RefreshScreen() => FlushCurrentString(); // TODO Determine if anything else needs to be done here

        public void SetCursorPosition(int x, int y)
        {
            if (!_inInputMode)
            {
                FlushCurrentString();

                if (_activeWindow == 0 && y != _cursorY)
                {
                    Scrollback.AddString("\r\n", _currentInfo);
                }
                _cursorX = x;
                _cursorY = y;

                _lastDrawn = Rect.Empty;
            }

        }

        public void ScrollLines(int top, int height, int lines)
        {
            FlushCurrentString();

            Scrollback.AddString("\r\n", _currentInfo);

            Invoke(() =>
            {
                for (int i = 0; i < mainCanvas.Children.Count; i++)
                {
                    var c = mainCanvas.Children[i];
                    if (c is Image img)
                    {
                        double iTop = img.Top();
                        double iLeft = img.Left();

                        double iBottom = iTop + img.ActualHeight;
                        double iRight = iLeft + img.ActualWidth;

                        if (iTop >= top && iBottom <= top + height)
                        {
                            double newPos = iTop - lines;
                            if (newPos >= top)
                            {
                                img.Top(newPos);
                            }
                            else
                            {
                                mainCanvas.Children.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            });
        }

        private void FlushCurrentString()
        {
            if (_currentText.Length == 0 || _inInputMode) return;

            string text = _currentText.ToString();
            _currentText.Clear();

            if (_activeWindow == 0)
            {
                Scrollback.AddString(text, _currentInfo);
            }

            SendStringToScreen(text, _currentInfo);

        }

        private Rect _lastDrawn = Rect.Empty;

        private void SendStringToScreen(string text, CharDisplayInfo cdi)
        {
            Invoke(() =>
            {
                var myImage = new Image();

                double x = _cursorX;
                double y = _cursorY;

                if (_lastDrawn != Rect.Empty && _inInputMode == false)
                {
                    x = _lastDrawn.X + _lastDrawn.Width;
                }

                var fi = _regularFont;
                if (cdi.Font == 4)
                {
                    fi = _fixedFont;
                }

                var ft = BuildFormattedText(text, fi, cdi);

                Brush brush = Brushes.Transparent;

                if (cdi.ImplementsStyle(ZStyles.REVERSE_STYLE))
                {
                    brush = ZColorCheck.ZColorToBrush(cdi.ForegroundColor, ColorType.Foreground);
                }
                else
                {
                    if (_currentInfo.BackgroundColor != _bColor)
                    {
                        brush = ZColorCheck.ZColorToBrush(cdi.BackgroundColor, ColorType.Background);
                    }
                }

                var dv = new DrawingVisual();
                var dc = dv.RenderOpen();
                using (Utilities.Dispose(dc, x => x.Close()))
                {
                    dc.DrawRectangle(brush, null, new Rect(0, 0, ft.WidthIncludingTrailingWhitespace, charHeight));
                    dc.DrawText(ft, new Point(0, 0));
                }    

                var dpi = VisualTreeHelper.GetDpi(this);
                var bmp = new RenderTargetBitmap((int)dv.ContentBounds.Width, (int)charHeight, 
                    dpi.PixelsPerInchX / dpi.PixelsPerDip, dpi.PixelsPerInchY / dpi.PixelsPerDip, PixelFormats.Pbgra32);
                bmp.Render(dv);

                myImage.Source = bmp;

                mainCanvas.Children.Add(myImage);
                myImage.Top(y);
                myImage.Left(x);

                _lastDrawn = new Rect(x, y, (int)dv.ContentBounds.Width, charHeight);

                RemoveCoveredImages(myImage);
            });
        }

        public void SetTextStyle(int new_style)
        {
            if (new_style != _currentInfo.Style)
            {
                FlushCurrentString();
                _currentInfo.Style = new_style;
            }
        }

        public void SetFont(int font)
        {
            if (_currentInfo.Font != font)
            {
                FlushCurrentString();
                _currentInfo.Font = font;
            }
        }

        private void Invoke(Action a) => Dispatcher.Invoke(a);

        public void Clear()
        {
            Invoke(() =>
            {
                mainCanvas.Children.Clear();
                _bColor = _currentInfo.BackgroundColor;
                Background = ZColorCheck.ZColorToBrush(_currentInfo.BackgroundColor, ColorType.Background);
            });
        }

        public void ClearArea(int top, int left, int bottom, int right)
        {
            Scrollback.AddString("\r\n", _currentInfo);

            var r = new Rect(left, top, right - left, bottom - top);

            Invoke(() =>
            {
                for (int i = 0; i < mainCanvas.Children.Count; i++)
                {
                    var c = mainCanvas.Children[i];
                    if (c is Image img)
                    {
                        double iTop = img.Top();
                        double iLeft = img.Left();

                        double iBottom = iTop + img.ActualHeight;
                        double iRight = iLeft + img.ActualWidth;

                        var iRect = new Rect(iLeft, iTop, iRight - iLeft, iBottom - iTop);
                        var p = new Point(iLeft, iTop);

                        if (r.Contains(p)) // what is iRect for?
                        {
                            mainCanvas.Children.RemoveAt(i);
                            i--;
                        }
                    }
                }
            });
        }

        public void ScrollArea(int top, int bottom, int left, int right, int units)
        {
            FlushCurrentString();

            Scrollback.AddString("\r\n", _currentInfo);

            var r = new Rect(left, top, right - left, bottom - top);

            Invoke(() =>
            {
                for (int i = 0; i < mainCanvas.Children.Count; i++)
                {
                    var c = mainCanvas.Children[i];
                    if (c is Image img)
                    {
                        double iTop = img.Top();
                        double iLeft = img.Top();

                        double iBottom = iTop + img.ActualHeight;
                        double iRight = iLeft + img.ActualWidth;

                        var iRect = new Rect(iLeft, iTop, iRight - iLeft, iBottom - iTop);
                        var p = new Point(iLeft, iTop);

                        if (r.Contains(p)) // what is iRect for?
                        {
                            double newPos = iTop - units;
                            if (newPos >= top)
                            {
                                img.Top(newPos);
                            }
                            else
                            {
                                mainCanvas.Children.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            });
        }

        public void RemoveChars(int count)
        {
            Invoke(() =>
            {
                if (count == 1 && _inInputMode)
                {
                    if (_currentText.Length > 0)
                    {
                        char c = _currentText[^1];

                        double x = _cursorCanvas.Left();
                        x -= GetStringWidth(c.ToString(), _currentInfo);
                        _cursorCanvas.Left(x);

                        _currentText.Remove(^1..);
                        RemoveLastChild();
                        if (_currentText.Length > 0)
                        {
                            SendStringToScreen(_currentText.ToString(), _currentInfo);
                        }
                        else
                        {
                            mainCanvas.Children.Add(new Image());
                        }
                    }
                }
                else
                {
                    RemoveLastChild();
                }

                _lastDrawn = new Rect(_cursorX, _cursorY, 0, 0);
            });
        }

        public void GetColor(out int foreground, out int background)
        {
            foreground = _currentInfo.ForegroundColor;
            background = _currentInfo.BackgroundColor;
        }

        public void SetColor(int new_foreground, int new_background)
        {
            FlushCurrentString();

            //long tempfg = Frotz.Other.TrueColorStuff.GetColour(new_foreground);
            //long tempbg = Frotz.Other.TrueColorStuff.GetColour(new_background);

            _currentInfo.ForegroundColor = new_foreground;
            _currentInfo.BackgroundColor = new_background;
        }

        public ushort PeekColor() => (ushort)_currentInfo.BackgroundColor;

        public void SetInputMode(bool inputMode, bool cursorVisible)
        {
            _inInputMode = inputMode;
            if (_inInputMode == false)
            {
                for (int i = 0; i < 10; i++)
                {
                    // TODO Move back to the carat
                }

                Scrollback.AddString(_currentText.ToString(), _currentInfo);

                _currentText.Clear();
            }
            else
            {
                if (_cursorX == _lastDrawn.X)
                {
                    _cursorX += (int)_lastDrawn.Width;
                }
            }

            Invoke(() =>
            {
                mainCanvas.Children.Add(new Image());

                _cursorCanvas.Visibility = cursorVisible ? Visibility.Visible : Visibility.Hidden;

                _cursorCanvas.Top(_cursorY + charHeight - _cursorCanvas.MinHeight);
                _cursorCanvas.Left(_cursorX);
            });
        }

        public void SetInputColor() => _currentInfo.ForegroundColor = 32;

        public void AddInputChar(char c)
        {
            Invoke(() =>
            {
                if (mainCanvas.Children.Count > 0)
                    mainCanvas.Children.RemoveAt(^1);

                _currentText.Append(c);

                SendStringToScreen(_currentText.ToString(), _currentInfo);

                double x = _cursorCanvas.Left();
                x += GetStringWidth(c.ToString(), _currentInfo);
                _cursorCanvas.Left(x);
            });
        }

        public ZPoint GetCursorPosition() => new(_cursorX, _cursorY);

        private bool _inInputMode = false;

        private void RemoveLastChild()
        {
            if (mainCanvas.Children.Count > 0)
            {
                mainCanvas.Children.RemoveAt(^1);
            }
        }

        #region Copied from TextControlScreen

        private int _bColor; // Track the background color separately

        private readonly FrotzSound _sound;
        private readonly Canvas _cursorCanvas;
        private int _cursorX = 0;
        private int _cursorY = 0;

        public ScreenMetrics GetScreenMetrics()
        {
            Invoke(() =>
            {
                SetCharsAndLines();
            });

            return _metrics;
        }

        public bool GetFontData(int font, ref ushort height, ref ushort width)
        {
            switch (font)
            {
                case ZFont.TEXT_FONT:
                case ZFont.FIXED_WIDTH_FONT:
                case ZFont.GRAPHICS_FONT:
                    height = (ushort)_metrics.FontSize.Height;
                    width = (ushort)_metrics.FontSize.Width;
                    return true;
                case ZFont.PICTURE_FONT:
                    return false;
            }

            return false;
        }

#nullable enable
        public void StoryStarted(string storyFileName, Blorb? blorbFile)
        {
            Invoke(() =>
            {
                _parent.Title = $"FrotzCore - {blorbFile?.StoryName ?? OS.BlorbFile?.StoryName ?? Path.GetFileName(storyFileName)}";

                OnStoryStarted(new GameSelectedEventArgs(storyFileName, blorbFile));
                Scrollback.Reset();
            });
        }
#nullable disable

        public int GetStringWidth(string s, CharDisplayInfo font)
        {
            int f = font.Font;
            if (f == -1) f = _currentInfo.Font;
            var ft = f switch
            {
                ZFont.FIXED_WIDTH_FONT => BuildFormattedText(s, _fixedFont, _currentInfo),
                ZFont.GRAPHICS_FONT    => BuildFormattedText(s, _beyZorkFont.Value, _currentInfo),
                _                      => BuildFormattedText(s, _regularFont, _currentInfo),
            };
            return (int)ft.WidthIncludingTrailingWhitespace;
        }

        public string OpenExistingFile(string defaultName, string title, string filter)
        {
            string name = null;
            Dispatcher.Invoke(() =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = title,
                    Filter = CreateFilterList(filter),
                    DefaultExt = ".sav"
                };

                ofd.FileName = Path.GetFileName(defaultName);

                if (ofd.ShowDialog(_parent) == true)
                {
                    name = ofd.FileName;
                }
                _parent.Focus(); // HACK For some reason, it won't always pick up text input after the dialog, so this refocuses
            });
            return name;
        }

        public string OpenNewOrExistingFile(string defaultName, string title, string filter, string defaultExtension)
        {
            string name = null;
            Dispatcher.Invoke(() =>
            {
                var sfd = new Microsoft.Win32.SaveFileDialog();

                var fi = new System.IO.FileInfo(defaultName);
                sfd.FileName = fi.Name;

                // sfd.FileName = defaultName;

                sfd.Title = title;
                sfd.Filter = CreateFilterList(filter);
                sfd.DefaultExt = defaultExtension;

                if (sfd.ShowDialog(_parent) == true)
                {
                    name = sfd.FileName;
                }

                _parent.Focus(); // HACK For some reason, it won't always pick up text input after the dialog, so this refocuses
            });
            return name;
        }

        private static string CreateFilterList(params string[] types) 
            => string.Join("|", types) + "|All Files (*.*)|*.*";

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
            // TODO I don't know if this is ever hit?
            throw new NotImplementedException();
        }

        public void StopSample(int number)
        {
            Invoke(() =>
            {
                _sound.StopSound();
            });
        }

        private static Frotz.Other.PNGChunk PaletteChunk = null;

        public void DrawPicture(int picture, byte[] image, int y, int x)
        {
            Dispatcher.Invoke(() =>
            {
                // If the image would go beyond the actual bounds of the display, don't bother drawing it.
                if (x > ActualWidth || y > ActualHeight) return;

                byte[] buffer = image;

                if (OS.BlorbFile.AdaptivePalatte != null && OS.BlorbFile.AdaptivePalatte.Count > 0)
                {
                    try
                    {
                        // Had to use the adaptive palatte for some Infocom games
                        using var readMS = new MemoryStream(image);
                        var p = new Frotz.Other.PNG(readMS);

                        if (OS.BlorbFile.AdaptivePalatte.Contains(picture))
                        {
                            if (PaletteChunk == null) throw new ArgumentException("No last palette");
                            p.Chunks["PLTE"] = PaletteChunk;
                        }
                        else
                        {
                            PaletteChunk = p.Chunks["PLTE"];
                        }

                        using var writeMS = OS.StreamManger.GetStream("AbsoluteScreen.DrawPicture");
                        p.Save(writeMS);

                        buffer = writeMS.ToArray();
                    }
                    catch (ArgumentException)
                    {
                        // TODO It's bad form to not at least define the exception better
                    }
                }

                var img = new FrotzImage();
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = new MemoryStream(buffer);
                bi.EndInit();
                img.Source = bi;

                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);

                int newX = x;
                int newY = y;

                if (newY > short.MaxValue) newY -= ushort.MaxValue;
                if (newX > short.MaxValue) newX -= ushort.MaxValue;

                img.Top(newY);
                img.Left(newX);

                img.Width(bi.Height * scale);
                img.Height(bi.Width * scale);

                if (picture == 1)
                {
                    if (img.Source.Width > mainCanvas.ActualWidth || img.Source.Height > mainCanvas.ActualHeight)
                    {
                        // Picture one is generallythe title page, Resize the img to be the same size as the canvas, 
                        // and it will show correctly in the bounds
                        img.Width(mainCanvas.ActualWidth);
                        img.Height(mainCanvas.ActualHeight);
                    }
                }

                mainCanvas.Children.Add(img);

                // removeCoveredImages(img);
            });
        }

        private static Rect GetImageBounds(Image img)
        {
            double x = img.Left();
            double y = img.Top();
            double width = img.Width();
            double height = img.Height();

            if (double.IsNaN(width) && img.Source != null)
            {
                width = img.Source.Width;
                height = img.Source.Height;
            }

            return new Rect(x, y, width, height);
        }

        // Iterate through the images on the screen, and remove any that would be completely obscured by the new image
        // In additional to keeping the number of images on the screen down, this also allows text to be drawn on top
        // of other images (like Zork Zero status)
        private void RemoveCoveredImages(Image img)
        {
            var r = GetImageBounds(img);

            for (int i = 0; i < mainCanvas.Children.Count; i++)
            {
                var oldImg = mainCanvas.Children[i] as Image;
                if (img == oldImg) continue;
                if (oldImg is not null)
                {
                    var oldR = GetImageBounds(oldImg);

                    if (r.Contains(oldR) || r == oldR)
                    {
                        mainCanvas.Children.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

#nullable enable
        public (string FileName, byte[] FileData)? SelectGameFile()
        {
            string? fName = null;
            byte[]? buffer = null;
            Dispatcher.Invoke(() =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open a Z-Code file",
                    DefaultExt = ".dat",

                    Filter = CreateFilterList(
                        "Most IF Files (*.zblorb;*.dat;*.z?;*.blorb)|*.zblorb;*.dat;*.z?;*.blorb",
                        "Infocom Blorb File (*.zblorb)|*.zblorb",
                        "Infocom Games (*.dat)|*.dat",
                        "Z-Code Files (*.z?)|*.z?",
                        "Blorb File (*.blorb)|*.blorb")
                };

                if (ofd.ShowDialog(_parent) == true)
                {
                    fName = ofd.FileName;
                    using var s = ofd.OpenFile();
                    buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                }

            });

            if (fName != null && buffer != null)
            {
                return (fName, buffer);
            }

            return null;
        }
#nullable disable

        public void DisplayMessage(string message, string caption) => MessageBox.Show(message, caption);

        public void HandleFatalError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                // TODO I'd like this to reference the root window for modality
                MessageBox.Show(_parent, message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            });

            throw new ZMachineException(message);
        }
        #endregion

        protected override void AfterSetCharsAndLines()
        {
            _cursorCanvas.MinHeight = 2;
            _cursorCanvas.MinWidth = charWidth;
        }

        protected override void DoReset() => Clear();

        public void SetActiveWindow(int win)
        {
            _activeWindow = win;
            FlushCurrentString();
        }

        public void SetWindowSize(int win, int top, int left, int height, int width)
        {
        }

        public bool ShouldWrap() => true;
    }
}

