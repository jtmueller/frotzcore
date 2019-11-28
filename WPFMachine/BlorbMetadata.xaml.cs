using Frotz;
using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace WPFMachine
{
    /// <summary>
    /// Interaction logic for BlorbMetadata.xaml
    /// </summary>
    public partial class BlorbMetadata : Window
    {
        private readonly Frotz.Blorb.Blorb _blorb;

        public BlorbMetadata(Frotz.Blorb.Blorb BlorbFile)
        {
            InitializeComponent();

            rtb.SizeChanged += new SizeChangedEventHandler(rtb_SizeChanged);
            imgCover.SizeChanged += new SizeChangedEventHandler(imgCover_SizeChanged);

            _blorb = BlorbFile;

            var xml = new XmlDocument();
            xml.LoadXml(_blorb.MetaData);

            int row = 0;

            XmlNodeList nodes;

            if (BlorbFile.Pictures.Count > 0)
            {
                nodes = xml.GetElementsByTagName("coverpicture");
                if (nodes.Count > 0)
                {
                    int id = Convert.ToInt32(nodes[0].InnerText);

                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    using var ms = OS.StreamManger.GetStream("BlorbMetadata", BlorbFile.Pictures[id].Image);
                    bi.StreamSource = ms;
                    bi.EndInit();
                    imgCover.Source = bi;
                }

            }

            nodes = xml.GetElementsByTagName("bibliographic");
            if (nodes.Count == 1)
            {
                foreach (XmlNode node in nodes[0].ChildNodes)
                {
                    if (node.Name == "description")
                    {
                        wbInfo.NavigateToString(node.InnerXml);
                    }
                    else
                    {
                        string text = "";
                        string key = node.Name;
                        switch (key)
                        {
                            case "title":
                                {
                                    text = "Title";
                                    Title = node.InnerText;
                                }
                                break;
                            case "author":
                                text = "Author"; break;
                            case "language":
                                text = "Language"; break;
                            case "headline":
                                text = "Subtitle"; break;
                            case "firstpublished":
                                text = "First Published"; break;
                            case "genre":
                                text = "Genre"; break;
                            case "group":
                                text = "Group"; break;
                            case "series":
                                text = "Series"; break;
                            case "seriesnumber":
                                text = "Series #"; break;
                        }

                        if (text == "Language") continue; // Temporary measure, since I don't want to see the language

                        var tr = new TableRow();
                        var tc = new TableCell(new Paragraph(new Run(text)));
                        tr.Cells.Add(tc);

                        var p = new Paragraph();
                        var r = new Run(node.InnerText);
                        p.TextAlignment = TextAlignment.Right;
                        p.Inlines.Add(r);

                        tc = new TableCell(p);
                        tr.Cells.Add(tc);

                        trg.Rows.Add(tr);

                        row++;
                    }
                }

                btnOk.Focus();
            }

            nodes = xml.GetElementsByTagName("contacts");
            if (nodes.Count > 0)
            {
                var n = nodes[0];
                if (n.FirstChild.Name == "url")
                {

                    var tr = new TableRow();
                    var tc = new TableCell(new Paragraph(new Run("More Info")));
                    tr.Cells.Add(tc);

                    var p = new Paragraph();
                    var h = new Hyperlink(new Run(n.FirstChild.InnerText))
                    {
                        Focusable = false,
                        Foreground = Brushes.Blue,
                        IsEnabled = true
                    };
                    p.TextAlignment = TextAlignment.Right;
                    h.MouseDown += new MouseButtonEventHandler(h_MouseDown);
                    h.NavigateUri = new Uri(n.FirstChild.InnerText);
                    h.ForceCursor = true;
                    h.Cursor = Cursors.Hand;
                    p.Inlines.Add(h);

                    tc = new TableCell(p);
                    tr.Cells.Add(tc);
                    trg.Rows.Add(tr);
                }
            }
        }

        private void h_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var hl = sender as Hyperlink;
            wbInfo.Navigate(hl.NavigateUri.ToString(), "_blank", null, null);
        }

        private void imgCover_SizeChanged(object sender, SizeChangedEventArgs e) => CheckSize();

        private void rtb_SizeChanged(object sender, SizeChangedEventArgs e) => CheckSize();

        private bool resized = false;
        private void CheckSize()
        {
            if (resized == false && rtb.ActualHeight > 0 && imgCover.ActualHeight > 0)
            {
                Height = rtb.ActualHeight + imgCover.ActualHeight + 50 + (ActualHeight - LayoutRoot.ActualHeight);
                resized = true;
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
