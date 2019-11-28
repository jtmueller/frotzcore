using System.Windows;
using System.Windows.Controls;

namespace WPFMachine.Options
{
    /// <summary>
    /// Interaction logic for GameDirectory.xaml
    /// </summary>
    public partial class GameDirectory : UserControl
    {
        public event RoutedEventHandler Click;

        public GameDirectory(string text)
        {
            InitializeComponent();
            lDir.Content = text;
        }

        private void bRemove_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, new RoutedEventArgs());
        }

        public string Directory => lDir.Content.ToString();
    }
}
