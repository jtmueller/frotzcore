using System.Windows;

namespace WPFMachine
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            tbAbout.Text = @"Original Frotz Code at http://sourceforge.frotz.net
Ported to .NET by Chris Szurgot (chris.szurgot@microsoft.com)
Ported to .NET Core by Joel Mueller (joel.mueller@spglobal.com)

More info to be added
";
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
    }
}
