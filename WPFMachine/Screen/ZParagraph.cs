using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using WPFMachine.Support;

namespace WPFMachine.Screen
{
    internal class ZParagraph : Paragraph
    {
        private readonly ZTextControl _parent;
        private readonly StringBuilder _currentText = new StringBuilder();
        private int _x;
        private CharDisplayInfo _currentInfo;
        private CharDisplayInfo _defaultInfo;
        private CharDisplayInfo _fixedInfo;
        private TextFlushMode _flushMode = TextFlushMode.Normal;

        internal ZParagraph(ZTextControl Parent, CharDisplayInfo CurrentInfo)
        {
            _parent = Parent;
            _currentInfo = CurrentInfo;

            _defaultInfo = new CharDisplayInfo(ZFont.TEXT_FONT, ZStyles.NORMAL_STYLE, 1, 1);
            _fixedInfo = new CharDisplayInfo(ZFont.FIXED_WIDTH_FONT, ZStyles.NORMAL_STYLE, 1, 1);
        }

        internal double Top { get; set; }

        public double Width
        {
            get
            {
                double w = 0;
                foreach (var i in base.Inlines)
                {
                    if (i is ZRun)
                    {
                        w += ((ZRun)i).Width;
                    }
                    else if (i is ZBlankContainer)
                    {
                        w += ((ZBlankContainer)i).Width;
                    }
                }
                return w;
            }
        }

        internal void SetDisplayInfo(CharDisplayInfo CurrentInfo)
        {
            if (_currentText.Length > 0)
            {
                Dispatcher.Invoke(Flush);
            }
            _currentInfo = CurrentInfo;

        }

        internal void AddDisplayChar(char c) => _currentText.Append(c);

        internal void Flush()
        {
            if (_currentText.Length == 0) return;
            string text = _currentText.ToString();
            _currentText.Clear();
            int chars = _x.Tcw();

            // TODO This is an invalid state, but it still seems to get here... I need to figure out why
            //if (_flushMode == TextFlushMode.Absolute && _x == 1 && Width == 0)
            //{
            //    ySystem.Diagnostics.Debug.WriteLine("How did I get here?");
            //}

            // If _x is 1 and Width == 0, We don't need to be here...
            if (_flushMode == TextFlushMode.Absolute)
            {
                _flushMode = TextFlushMode.Normal;
                //                Inlines.Clear();
                if (Inlines.Count == 0)
                {
                    // Add an empty inline to allow for the absolute positioning
                    AddInline("", _defaultInfo);
                }

                if (Inlines.Count == 1 && FirstInline.DisplayInfo.AreSame(_currentInfo) &&
                    (FirstInline.DisplayInfo.Font == ZFont.FIXED_WIDTH_FONT ||
                    FirstInline.DisplayInfo.ImplementsStyle(ZStyles.FIXED_WIDTH_STYLE)))
                {
                    FirstInline.Text = ReplaceText(chars, FirstInline.Text, text);
                    return;
                }
                else
                {
                    if (Width == 0)
                    {
                        if (IsFixedWidth(_currentInfo))
                        {
                            SetAbsolute(text, _x, _currentInfo);

                            return;
                        }
                        else
                        {
#if !USEADORNER
                            if (_x > 1)
                            {
                                System.Diagnostics.Debug.WriteLine("Inserting a blank");
                                var zbc = new ZBlankContainer(_x);
                                Inlines.Add(zbc);
                            }

                            AddInline(text, _currentInfo);
                            return;
#else
                            setAbsolute(text, _x, _currentInfo);
                            return;
#endif
                        }
                    }
                    else
                    {
                        // TODO If the top == 1, it must be the status bar overwriting text... Make this work better
                        if (IsOnlyFixedFont() && IsFixedWidth(_currentInfo) || Top == 1)
                        {
                            // TODO Need to make some calculations here
                            string temp = CurrentText;
                            Inlines.Clear();

                            AddInline(ReplaceText(chars, temp, text), _fixedInfo);

                            return;
                        }
                        else
                        {
                            if (text.Trim() != "" || _currentInfo.ImplementsStyle(ZStyles.REVERSE_STYLE))
                            {
                                SetAbsolute(text, _x, _currentInfo);

                                return;
                            }
                            else
                            {
                                // System.Diagnostics.Debug.WriteLine("Do something about Absolute");

                                // Figure f = new Figure();
                                return;
                            }
                        }
                    }
                }
            }
            else if (_flushMode == TextFlushMode.Overwrite)
            {
                _flushMode = TextFlushMode.Normal;
                throw new Exception("In Overwrite mode");
            }

            if (LastInline != null && LastInline.DisplayInfo.AreSame(_currentInfo))
            {
                LastInline.Text += text;
            }
            else
            {
                AddInline(text, _currentInfo);
            }
        }

        private void SetAbsolute(string text, int _x, CharDisplayInfo _info) 
            => _parent._adorner.AddAbsolute(text, (int)Top, _x, _info);

        private string ReplaceText(int pos, string currentString, string newText)
        {
            string previous = currentString;
            if (previous.Length < pos + newText.Length)
            {
                previous = previous.PadRight(pos + newText.Length);
            }
            var sb = new StringBuilder(previous);
            sb.Remove(pos, newText.Length);
            sb.Insert(pos, newText);

            return sb.ToString();
        }

        private bool IsFixedWidth(CharDisplayInfo Info) 
            => Info.Font == ZFont.FIXED_WIDTH_FONT || Info.ImplementsStyle(ZStyles.FIXED_WIDTH_STYLE);

        private ZRun AddInline(string Text, CharDisplayInfo DisplayInfo)
        {
            var temp = CreateInline(Text, DisplayInfo);
            Inlines.Add(temp);
            return temp;
        }

        private ZRun CreateInline(string Text, CharDisplayInfo DisplayInfo)
        {
            // Add an empty inline to allow for the absolute positioning
            var temp = new ZRun(DisplayInfo)
            {
                Text = Text,
                FontFamily = _currentInfo.Font == 1 
                    ? _parent.RegularFont.Family : _parent.FixedFont.Family
            };

            ImplementRunStyle(temp);

            return temp;
        }

        private ZRun FirstInline => (ZRun)Inlines.FirstInline;

        private ZRun LastInline
        {
            get
            {
                var r = Inlines.LastInline as ZRun;
                return r;
            }
        }

        internal void SetCursorXPosition(int x)
        {
            Flush();
            _x = x;
            _flushMode = TextFlushMode.Absolute;
        }


        internal void ImplementRunStyle(ZRun run)
        {
            if (run == null) return;

            if (run.DisplayInfo.ImplementsStyle(ZStyles.BOLDFACE_STYLE))
            {
                FontWeight = FontWeights.Bold;
            }

            if (run.DisplayInfo.ImplementsStyle(ZStyles.EMPHASIS_STYLE))
            {
                run.FontStyle = FontStyles.Italic;
            }

            if (run.DisplayInfo.ImplementsStyle(ZStyles.FIXED_WIDTH_STYLE) || run.DisplayInfo.Font != ZFont.TEXT_FONT)
            {
                run.FontFamily = _parent.FixedFont.Family;
            }

            if (run.DisplayInfo.ImplementsStyle(ZStyles.REVERSE_STYLE))
            {
                run.Background = ZColorCheck.ZColorToBrush(run.DisplayInfo.ForegroundColor, ColorType.Foreground);
                run.Foreground = ZColorCheck.ZColorToBrush(run.DisplayInfo.BackgroundColor, ColorType.Background);
            }
            else
            {
                run.Foreground = ZColorCheck.ZColorToBrush(run.DisplayInfo.ForegroundColor, ColorType.Foreground);
                int color = run.DisplayInfo.BackgroundColor;
                if (color != _parent.BColor)
                {
                    run.Background = ZColorCheck.ZColorToBrush(color, ColorType.Background);
                }
            }
        }

        public string CurrentText
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var i in Inlines)
                {
                    if (i is Run r)
                    {
                        sb.Append(r.Text);
                    }
                }
                return sb.ToString();
            }
        }

        private bool IsOnlyFixedFont()
        {
            foreach (var i in Inlines)
            {
                if (i is ZRun r)
                {
                    if (!IsFixedWidth(r.DisplayInfo) && r.Text != "") return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private ZRun _inputRun = null;
        internal void StartInputMode()
        {
            _inputRun = new ZRun(_currentInfo);
            ImplementRunStyle(_inputRun);
            Inlines.Add(_inputRun);
        }

        internal void AddInputChar(char c) => _inputRun.Text += c;

        internal void RemoveInputChars(int count)
        {
            if (_inputRun != null)
            {
                RemoveCharsFromRun(_inputRun, count);
            }
            else
            {
                RemoveCharsFromRun(LastInline, count);
            }
        }


        internal void EndInputMode() => _inputRun = null;

        private void RemoveCharsFromRun(ZRun run, int count) => run.Text = run.Text.Remove(run.Text.Length - count);
    }

    internal enum TextFlushMode
    {
        Normal = 0,
        Overwrite = 1,
        Absolute = 2
    }

    internal class CharsInLine
    {
        private readonly List<CharInfo> _chars = new List<CharInfo>();

        internal CharsInLine()
        {
        }
    }
}

