using Frotz;
using Frotz.Constants;
using Frotz.Screen;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WPFMachine.Absolute
{
    public class ScrollbackArea
    {
        private readonly DockPanel _dock;
        public DockPanel DP => _dock;

        public RichTextBox _RTB;
        private readonly FlowDocument _doc;
        private Paragraph _p;
        private Run _currentRun = null;
        private readonly UserControl _parent;

        public ScrollbackArea(UserControl Parent)
        {
            _dock = new DockPanel();

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            _dock.Children.Add(sp);

            sp.SetValue(DockPanel.DockProperty, Dock.Top);

            var bCopyText = new Button
            {
                Content = "Copy Text To Clipboard"
            };
            bCopyText.Click += new RoutedEventHandler(CopyText_Click);
            sp.Children.Add(bCopyText);

            var bSaveRtf = new Button
            {
                Content = "Save RTF"
            };
            bSaveRtf.Click += new RoutedEventHandler(SaveRtf_Click);
            sp.Children.Add(bSaveRtf);

            var bSaveText = new Button
            {
                Content = "Save Text"
            };
            bSaveText.Click += new RoutedEventHandler(SaveText_Click);
            sp.Children.Add(bSaveText);

            var sv = new ScrollViewer();
            _dock.Children.Add(sv);

            _RTB = new RichTextBox
            {
                IsReadOnly = true,
                IsReadOnlyCaretVisible = true
            };

            _doc = new FlowDocument();
            _RTB.Document = _doc;

            _parent = Parent;

            Reset();

            sv.Content = _RTB;
        }

        private void SaveFile(string filter, string Format)
        {
            var sfd = new SaveFileDialog
            {
                Filter = filter
            };

            if (sfd.ShowDialog() == false)
            {
                return;
            }

            string fileName = sfd.FileName;

            using var fs = new System.IO.FileStream(fileName, System.IO.FileMode.Create);

            var range = new TextRange(_RTB.Document.ContentStart, _RTB.Document.ContentEnd);
            range.Save(fs, Format, true);
        }

        private void SaveText_Click(object sender, RoutedEventArgs e) => SaveFile("Text (*.txt)|*.txt", System.Windows.DataFormats.Text);

        private void SaveRtf_Click(object sender, RoutedEventArgs e) => SaveFile("Rich Text Format (*.rtf)|*.rtf", System.Windows.DataFormats.Rtf);

        private void CopyText_Click(object sender, RoutedEventArgs e)
        {
            var range = new TextRange(_RTB.Document.ContentStart, _RTB.Document.ContentEnd);
            Clipboard.SetText(range.Text);
        }

        public void Reset()
        {
            _p = new Paragraph
            {
                FontFamily = new System.Windows.Media.FontFamily(Properties.Settings.Default.ProportionalFont)
            };

            double PointSize = Properties.Settings.Default.FontSize * (96.0 / 72.0);
            _p.FontSize = PointSize;

            _doc.Blocks.Clear();

            _doc.Blocks.Add(_p);

            _currentRun = null;

            currentStyle = -1;
        }

        private const string threeNewLines = "\r\n\r\n\r\n";
        private int currentStyle = -1;

        public void AddString(string text, CharDisplayInfo cdi)
        {
            if (string.IsNullOrEmpty(text)) return;
            _parent.Dispatcher.Invoke(() =>
            {
                if (text == "\r\n")
                {

                    if (_p.Inlines.LastInline is LineBreak && _p.Inlines.LastInline.PreviousInline is LineBreak)
                    {
                        return;
                    }
                    var lb = new LineBreak();
                    _p.Inlines.Add(lb);

                    _currentRun = null;
                    return;
                }


                if (currentStyle != cdi.Style)
                {
                    currentStyle = cdi.Style;
                    _currentRun = new Run();
                    _p.Inlines.Add(_currentRun);
                    if ((cdi.Style & ZStyles.BOLDFACE_STYLE) != 0)
                    {
                        _currentRun.FontWeight = FontWeights.Bold;
                    }
                    if ((cdi.Style & ZStyles.EMPHASIS_STYLE) != 0)
                    {
                        _currentRun.FontStyle = FontStyles.Italic;
                    }
                    if ((cdi.Style & ZStyles.REVERSE_STYLE) != 0)
                    {
                        _currentRun.Background = Brushes.Black;
                        _currentRun.Foreground = Brushes.White;
                    }
                    if ((cdi.Style & ZStyles.FIXED_WIDTH_STYLE) != 0)
                    {
                        _currentRun.FontFamily = new System.Windows.Media.FontFamily(Properties.Settings.Default.FixedWidthFont);
                    }
                }

                if (_currentRun == null)
                {
                    _currentRun = new Run(text);
                    _p.Inlines.Add(_currentRun);
                }
                else
                {
                    _currentRun.Text += text;

                    if (_currentRun.Text.EndsWith(threeNewLines))
                    {
                        var sb = new StringBuilder(_currentRun.Text);

                        while (sb[^6] == '\r' && sb[^5] == '\n' &&
                               sb[^4] == '\r' && sb[^3] == '\n' &&
                               sb[^2] == '\r' && sb[^1] == '\n')
                        {
                            sb.Remove(^2..);
                        }

                        _currentRun.Text = sb.ToString();

                        //                        
                    }
                }
                _RTB.CaretPosition = _RTB.CaretPosition.DocumentEnd;

            });
        }
    }
}
