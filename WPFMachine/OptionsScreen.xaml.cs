using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WPFMachine.Support;

namespace WPFMachine
{
    /// <summary>
    /// Interaction logic for OptionsScreen.xaml
    /// </summary>
    public partial class OptionsScreen : Window, IComparer<FontFamily>
    {
        public OptionsScreen()
        {
            InitializeComponent();

            var settings = Properties.Settings.Default;

            var fixedWidthFonts = new List<FontFamily>();
            var otherWidthFonts = new List<FontFamily>();

            double maxFixedHeight = -1;
            double maxPropHeight = -1;

            FontFamily fixedWidthCurrent = null;
            FontFamily propWidthCurrent = null;

            int count = 0;

            double ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            foreach (var ff in Fonts.SystemFontFamilies)
            {
                count++;

                var ft = new FormattedText("i", System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface(ff.Source), 10, Brushes.Black, ppd);

                var s = new Size(ft.Width, ft.Height);

                ft = new FormattedText("w", System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface(ff.Source), 10, Brushes.Black, ppd);

                if (ft.Width == s.Width)
                {
                    fixedWidthFonts.Add(ff);

                    maxFixedHeight = Math.Max(maxFixedHeight, ft.Height);
                    if (ff.Source == settings.FixedWidthFont)
                    {
                        fixedWidthCurrent = ff;
                    }
                }

                otherWidthFonts.Add(ff);
                maxPropHeight = Math.Max(maxPropHeight, ft.Height);
                if (ff.Source == settings.ProportionalFont)
                {
                    propWidthCurrent = ff;
                }

            }

            fixedWidthFonts.Sort(this);
            otherWidthFonts.Sort(this);


            fddFixedWidth.ItemsSource = fixedWidthFonts;
            fddProportional.ItemsSource = otherWidthFonts;

            fddFixedWidth.SelectedItem = fixedWidthCurrent;
            fddProportional.SelectedItem = propWidthCurrent;

            tbFontSize.Text = settings.FontSize.ToString();

            ccForeColor.SelectedColor = settings.DefaultForeColor;
            ccBackColor.SelectedColor = settings.DefaultBackColor;
            ccInputColor.SelectedColor = settings.DefaultInputColor;

            tbLastPlayedCount.Text = settings.LastPlayedGamesCount.ToString();

            cbShowDebug.IsChecked = settings.ShowDebugMenu;

            string dirs = settings.GameDirectoryList;

            if (dirs != "")
            {
                foreach (var dir in dirs.Split(';'))
                {
                    AddDirectory(dir);
                }
            }

            tbLeftMargin.Text = settings.FrotzLeftMargin.ToString();
            tbRightMargin.Text = settings.FrotzRightMargin.ToString();
            tbContextLines.Text = settings.FrotzContextLines.ToString();
            tbUndoSlots.Text = settings.FrotzUndoSlots.ToString();
            tbScriptColumns.Text = settings.FrotzScriptColumns.ToString();

            cbPiracy.IsChecked = settings.FrotzPiracy;
            cbExpandAbbreviations.IsChecked = settings.FrotzExpandAbbreviations;
            cbIgnoreErrors.IsChecked = settings.FrotzIgnoreErrors;
            cbAttrAssignment.IsChecked = settings.FrotzAttrAssignment;
            cbAttrTesting.IsChecked = settings.FrotzAttrTesting;
            cbObjLocating.IsChecked = settings.FrotzObjLocating;
            cbObjMovement.IsChecked = settings.FrotzObjMovement;

            cbSaveQuetzal.IsChecked = settings.FrotzSaveQuetzal;
            cbSound.IsChecked = settings.FrotzSound;
        }

        private void AddDirectory(string dir)
        {
            var gd = new Options.GameDirectory(dir);
            gd.Click += new RoutedEventHandler(gd_Click);
            spGameList.Children.Add(gd);

            gdListRow.Height = new GridLength(spGameList.Children.Count * 30);
        }

        private void gd_Click(object sender, RoutedEventArgs e)
        {
            var gd = sender as Options.GameDirectory;
            spGameList.Children.Remove(gd);

            gdListRow.Height = new GridLength(spGameList.Children.Count * 30);
        }

        public int Compare(FontFamily x, FontFamily y) => string.Compare(x.Source, y.Source);

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            var settings = Properties.Settings.Default;

            if (fddFixedWidth.SelectedItem is FontFamily ff)
            {
                Properties.Settings.Default.FixedWidthFont = ff.Source;
            }

            if (fddProportional.SelectedItem is FontFamily fp)
            {
                Properties.Settings.Default.ProportionalFont = fp.Source;
            }

            Properties.Settings.Default.FontSize = Convert.ToInt32(tbFontSize.Text);

            Properties.Settings.Default.DefaultForeColor = ccForeColor.SelectedColor;
            Properties.Settings.Default.DefaultBackColor = ccBackColor.SelectedColor;
            Properties.Settings.Default.DefaultInputColor = ccInputColor.SelectedColor;

            Properties.Settings.Default.LastPlayedGamesCount = Convert.ToInt32(tbLastPlayedCount.Text);

            settings.ShowDebugMenu = cbShowDebug.IsChecked ?? false;


            var dirs = new List<string>();
            foreach (Options.GameDirectory gd in spGameList.Children)
            {
                dirs.Add(gd.Directory);
            }

            Properties.Settings.Default.GameDirectoryList = string.Join(";", dirs);

            settings.FrotzPiracy = cbPiracy.IsChecked ?? false;
            settings.FrotzExpandAbbreviations = cbExpandAbbreviations.IsChecked ?? false;
            settings.FrotzAttrAssignment = cbAttrAssignment.IsChecked ?? false;
            settings.FrotzAttrTesting = cbAttrTesting.IsChecked ?? false;
            settings.FrotzObjMovement = cbObjMovement.IsChecked ?? false;
            settings.FrotzObjLocating = cbObjLocating.IsChecked ?? false;

            settings.FrotzSaveQuetzal = cbSaveQuetzal.IsChecked ?? false;
            settings.FrotzSound = cbSound.IsChecked ?? false;

            settings.FrotzLeftMargin = Convert.ToUInt16(tbLeftMargin.Text);
            settings.FrotzRightMargin = Convert.ToUInt16(tbRightMargin.Text);
            settings.FrotzContextLines = Convert.ToUInt16(tbContextLines.Text);
            settings.FrotzUndoSlots = Convert.ToInt32(tbUndoSlots.Text);
            settings.FrotzScriptColumns = Convert.ToInt32(tbScriptColumns.Text);

            Properties.Settings.Default.Save();

            Close();
        }

        private void cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void DockPanel_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ok_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                cancel_Click(this, new RoutedEventArgs());
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ok_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                cancel_Click(this, new RoutedEventArgs());
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new BrowseForFolder();
            var hwnd = new WindowInteropHelper(this).Handle;
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string path = fbd.SelectFolder("Select a folder containing one or more game files.", docPath, hwnd);

            if (!string.IsNullOrWhiteSpace(path))
            {
                AddDirectory(path);
            }
        }
    }
}
