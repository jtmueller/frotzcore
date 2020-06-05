using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFMachine
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IZMachineScreen _screen;
        private Thread _zThread;
        private readonly List<string> _lastPlayedGames = new List<string>();
        private readonly bool _closeOnQuit = false;
        private string _storyFileName;
        private Frotz.Blorb.Blorb _blorbFile;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitializeComponent();

            Properties.Settings.Default.Upgrade();

            var b = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Black
            };

            // _screen = new Screen.TextControlScreen(this);
            _screen = new Absolute.AbsoluteScreen(this);
            pnlScreenPlaceholder.Children.Add(b);

            b.Child = (UIElement)_screen;
            Loaded += new RoutedEventHandler(MainWindow_Loaded);

            if (Properties.Settings.Default.LastPlayedGames != null)
            {
                string[] games = Properties.Settings.Default.LastPlayedGames.Split('|');
                _lastPlayedGames = new List<string>(games);
            }

            BuildMainMenu();

            SizeChanged += new SizeChangedEventHandler(MainWindow_SizeChanged);

            TextInput += new TextCompositionEventHandler(MainWindow_TextInput);
            PreviewKeyDown += new KeyEventHandler(MainWindow_PreviewKeyDown);

            statusBottom.Visibility = System.Windows.Visibility.Hidden;

            SetFrotzOptions();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) => MessageBox.Show("EX:" + e.ExceptionObject);

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // I can capture the arrow keys here
            if (!mnuInGame.IsFocused)
            {
                var c = e.Key switch
                {
                    Key.Tab   => '\t',
                    Key.Up    => (char)CharCodes.ZC_ARROW_UP,
                    Key.Down  => (char)CharCodes.ZC_ARROW_DOWN,
                    Key.Left  => (char)CharCodes.ZC_ARROW_LEFT,
                    Key.Right => (char)CharCodes.ZC_ARROW_RIGHT,
                    _ => '\0',
                };

                if (c != 0)
                {
                    _screen.AddInput(c);
                    e.Handled = true;

                }
            }
        }

        private void MainWindow_TextInput(object sender, TextCompositionEventArgs e)
        {
            if (_screen != null)
            {
                if (e.Text.Length > 0)
                {
                    _screen.AddInput(e.Text[0]);
                }

                if (e.SystemText.Length > 0)
                {
                    ushort newKey = ConvertAltText(e.SystemText);

                    if (newKey != '\0')
                    {
                        _screen.AddInput((char)newKey);
                    }
                }
            }
        }

        // I'd like to make this a return char
        private ushort ConvertAltText(string text)
        {
            return char.ToLowerInvariant(text[0]) switch
            {
                'h' => CharCodes.ZC_HKEY_HELP,
                'd' => CharCodes.ZC_HKEY_DEBUG,
                'p' => CharCodes.ZC_HKEY_PLAYBACK,
                'r' => CharCodes.ZC_HKEY_RECORD,

                's' => CharCodes.ZC_HKEY_SEED,
                'u' => CharCodes.ZC_HKEY_UNDO,
                'n' => CharCodes.ZC_HKEY_RESTART,
                'x' => CharCodes.ZC_HKEY_QUIT,

                _ => CharCodes.ZC_BAD
            };
        }

        private void BuildMainMenu()
        {
            miRecentGames.Items.Clear();
            miGames.Items.Clear();

            foreach (string s in _lastPlayedGames)
            {
                var mi = new MenuItem
                {
                    Header = s,
                    Tag = s
                };
                mi.Click += new RoutedEventHandler(MnuMru_Click);
                miRecentGames.Items.Add(mi);
            }

            SetupGameDirectories();
        }

        private void SetupGameDirectories()
        {
            string gameDirectories = Properties.Settings.Default.GameDirectoryList;

            miGames.Items.Clear();

            if (!string.IsNullOrWhiteSpace(gameDirectories))
            {
                string[] list = gameDirectories.Split(';');
                if (list.Length == 1)
                {
                    AddFilesInPath(list[0], miGames, true);

                    if (miGames.Items.Count == 1)
                    {
                        var mi = miGames.Items[0] as MenuItem;
                        var items = new List<MenuItem>();

                        foreach (MenuItem i in mi.Items)
                        {
                            items.Add(i);
                        }

                        foreach (var i in items)
                        {
                            mi.Items.Remove(i);
                            miGames.Items.Add(i);
                        }

                        miGames.Items.Remove(mi);
                    }

                    // DirectoryInfo di 
                }
                else
                {
                    foreach (string dir in list)
                    {
                        try
                        {
                            AddFilesInPath(dir, miGames, true);
                        }
                        catch (DirectoryNotFoundException) { }
                        catch (ArgumentException) { }
                    }
                }
                miGames.Visibility = Visibility.Visible;
            }
            else
            {
                miGames.Visibility = Visibility.Collapsed;
            }
        }

        private readonly HashSet<string> _validExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".z1", ".z2", ".z3", ".z4", ".z5", ".z6", ".z7", ".z8", ".zblorb", ".dat"
        };

        private void AddFilesInPath(string path, MenuItem parent, bool recurse = true)
            => AddFilesInPath(new DirectoryInfo(path), parent, recurse);

        private void AddFilesInPath(DirectoryInfo di, MenuItem parent, bool recurse = true)
        {
            var miRoot = new MenuItem
            {
                Header = di.Name
            };

            if (recurse)
            {
                foreach (var sub in di.EnumerateDirectories())
                {
                    AddFilesInPath(sub, miRoot, recurse);
                }
            }

            foreach (var fi in di.EnumerateFiles())
            {
                if (_validExtensions.Contains(fi.Extension))
                {
                    AddGameItem(fi, miRoot);
                }
            }

            if (miRoot.Items.Count > 0)
            {
                parent.Items.Add(miRoot);
            }
        }

        private void AddGameItem(FileInfo file, MenuItem parent)
        {
            var mi = new MenuItem
            {
                Header = file.Name,
                Tag = file.FullName
            };
            mi.Click += MnuMru_Click;

            parent.Items.Add(mi);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualHeight > 0 && ActualWidth > 0)
            {
                _screen.SetCharsAndLines();
                var (rows, cols) = _screen.Metrics;
                stsItemSize.Content = $"{rows}x{cols}";
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var args = Environment.GetCommandLineArgs().AsMemory(1);
            if (args.Length > 0)
            {
                // closeOnQuit = true;
                StartThread(args);
            }
        }

        private void StartThread(Memory<string> args)
        {
            _zThread = new Thread(ZMachineThread)
            {
                IsBackground = true
            };
            _zThread.Start(args);
        }

        public void ZMachineThread(object argsO)
        {
            var args = ((Memory<string>)argsO).Span;
            if (args.Length > 0 && args[0] == "last" && _lastPlayedGames.Count > 0)
            {
                args[0] = _lastPlayedGames[^1];
            }

            try
            {
                Dispatcher.Invoke(() =>
                {
                    mnuInGame.Visibility = Visibility.Visible;
                    mnuMain.Visibility = Visibility.Collapsed;
                    gameButtons.IsEnabled = true;
                    _screen.Focus();

                    miDebugInfo.Visibility = Properties.Settings.Default.ShowDebugMenu ? Visibility.Visible : Visibility.Collapsed;
                });

                Frotz.OS.SetScreen((IZScreen)_screen);

                ZColorCheck.ResetDefaults();

                _screen.GameSelected += new EventHandler<GameSelectedEventArgs>(Screen_GameSelected);

                Frotz.Generic.Main.MainFunc(args);

                //Dispatcher.Invoke(_screen.Reset);

                if (_closeOnQuit)
                {
                    Dispatcher.Invoke(Close);
                }
            }
            catch (ZMachineException)
            { // Noop
            }
            catch (Exception ex)
            {
                MessageBox.Show("EX:" + ex);
            }
            finally
            {
                _screen.GameSelected -= new EventHandler<GameSelectedEventArgs>(Screen_GameSelected);
                Dispatcher.Invoke(() =>
                {
                    BuildMainMenu();

                    mnuInGame.Visibility = Visibility.Collapsed;
                    mnuMain.Visibility = Visibility.Visible;
                    gameButtons.IsEnabled = false;

                    Title = "FrotzCore";
                });
            }
        }

        private void Screen_GameSelected(object sender, GameSelectedEventArgs e)
        {
            string s = e.StoryFileName;

            for (int i = 0; i < _lastPlayedGames.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_lastPlayedGames[i]) || string.Compare(_lastPlayedGames[i], s, true) == 0)
                {
                    _lastPlayedGames.RemoveAt(i--);
                }
            }

            _lastPlayedGames.Add(s);


            while (_lastPlayedGames.Count > Properties.Settings.Default.LastPlayedGamesCount)
            {
                _lastPlayedGames.RemoveAt(0);
            }

            Properties.Settings.Default.LastPlayedGames = string.Join("|", _lastPlayedGames);
            Properties.Settings.Default.Save();

            _storyFileName = e.StoryFileName;
            _blorbFile = e.BlorbFile;

            miGameInfo.IsEnabled = (_blorbFile != null);
        }

        private void SetFrotzOptions()
        {
            var settings = Properties.Settings.Default;

            Frotz.Generic.Main.option_context_lines = settings.FrotzContextLines;
            Frotz.Generic.Main.option_left_margin = settings.FrotzLeftMargin;
            Frotz.Generic.Main.option_right_margin = settings.FrotzRightMargin;
            Frotz.Generic.Main.option_script_cols = settings.FrotzScriptColumns;
            Frotz.Generic.Main.option_undo_slots = settings.FrotzUndoSlots;

            Frotz.Generic.Main.option_attribute_assignment = settings.FrotzAttrAssignment;
            Frotz.Generic.Main.option_attribute_testing = settings.FrotzAttrTesting;
            Frotz.Generic.Main.option_expand_abbreviations = settings.FrotzExpandAbbreviations;
            Frotz.Generic.Main.option_ignore_errors = settings.FrotzIgnoreErrors;
            Frotz.Generic.Main.option_object_locating = settings.FrotzObjLocating;
            Frotz.Generic.Main.option_object_movement = settings.FrotzObjMovement;
            Frotz.Generic.Main.option_piracy = settings.FrotzPiracy;

            Frotz.Generic.Main.option_save_quetzal = settings.FrotzSaveQuetzal;
            Frotz.Generic.Main.option_sound = settings.FrotzSound;

        }

        #region Menu Events
        private void MnuQuitGame_Click(object sender, RoutedEventArgs e)
        {
            if (_zThread != null)
            {
                Frotz.Generic.Main.AbortGameLoop = true;
            }
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e) => Close();

        private void MnuMru_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as MenuItem;
            string game = mi.Tag as string;

            //if (_zThread != null)
            //{
            //    // Should never get here since the menu isn't show while a game is in progress
            //    Frotz.Generic.Main.AbortGameLoop = true;
            //}

            StartThread(new string[] { game });

        }

        private void MiOptions_Click(object sender, RoutedEventArgs e)
        {
            var os = new OptionsScreen
            {
                Owner = this
            };
            os.ShowDialog();

            _screen.SetFontInfo();
            _screen.SetCharsAndLines();

            SetupGameDirectories();
            SetFrotzOptions();
        }

        private void MiStartNewStory_Click(object sender, RoutedEventArgs e)
        {
            if (_zThread != null)
            {
                Frotz.Generic.Main.AbortGameLoop = true;
            }
            StartThread(Array.Empty<string>());
        }

        private void MiExit_Click(object sender, RoutedEventArgs e) => Close();

        private void MiGameInfo_Click(object sender, RoutedEventArgs e)
        {
            var bm = new BlorbMetadata(_blorbFile)
            {
                Owner = this
            };
            bm.ShowDialog();
        }

        private void MiAbout_Click(object sender, RoutedEventArgs e)
        {
            var aw = new AboutWindow
            {
                Owner = this
            };
            aw.ShowDialog();
        }
        #endregion

        private void MiDebugInfo_Click(object sender, RoutedEventArgs e)
        {
            byte[] buffer;
            if (_blorbFile != null && _blorbFile.ZCode != null)
            {
                buffer = _blorbFile.ZCode;
            }
            else
            {
                using var fs = new FileStream(_storyFileName, FileMode.Open);
                buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
            }

            try
            {
                var info = ZTools.InfoDump.Main(buffer, Array.Empty<string>());

                var w = new Window();
                var tc = new TabControl();

                foreach (var val in info)
                {
                    CreateTextBox(tc, val.Header, val.Text);
                }

                string temp = ZTools.Txd.Main(buffer, Array.Empty<string>());
                string endOfCode = "[END OF CODE]";

                int index = temp.IndexOf(endOfCode, StringComparison.OrdinalIgnoreCase);


                if (index == -1)
                {
                    info.Add(new ZTools.InfoDump.ZToolInfo("TXD", temp));

                    CreateTextBox(tc, "TXD", temp);
                }
                else
                {
                    index += endOfCode.Length;

                    AddTabItem(tc, "TXD - Code", new Support.ZInfoTXD(temp.Substring(0, index), 0));
                    AddTabItem(tc, "TXD - Strings", new Support.ZInfoTXD(temp.Substring(index + 1), 1));
                }

                w.Content = tc;

                w.Show();
            }
            catch (ArgumentException ae)
            {
                MessageBox.Show("Exception\r\n" + ae);
            }
        }

        private void CreateTextBox(TabControl tc, string header, string text)
        {
            var tb = new TextBox
            {
                Text = text,
                FontFamily = new FontFamily("Consolas")
            };

            var sv = new ScrollViewer
            {
                Content = tb
            };

            AddTabItem(tc, header, sv);
        }

        private void AddTabItem(TabControl tc, string header, Control c)
        {
            var ti = new TabItem
            {
                Header = header,
                Content = c
            };
            tc.Items.Add(ti);
        }

        private void MiHistory_Click(object sender, RoutedEventArgs e)
        {
            var d = ((Absolute.AbsoluteScreen)_screen).Scrollback.DP;

            var w = new Window
            {
                Content = d,
                Owner = this
            };

#if !temp
            w.ShowDialog();
            w.Content = null;
#else
            w.Show();
#endif
        }

        private void BtnSaveGame_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(Frotz.Generic.GameControl.SaveGame);
        }

        private void BtnOpenSave_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(Frotz.Generic.GameControl.RestoreGame);
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(Frotz.Generic.GameControl.Undo);
        }
    }
}
