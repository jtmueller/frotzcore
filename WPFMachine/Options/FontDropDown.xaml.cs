using System.Windows.Controls;

namespace WPFMachine.Options
{
    /// <summary>
    /// Interaction logic for FontDropDown.xaml
    /// </summary>
    public partial class FontDropDown : UserControl
    {
        public FontDropDown()
        {
            InitializeComponent();
        }

        public System.Collections.IEnumerable ItemsSource {
            get => fontComboFast.ItemsSource;
            set => fontComboFast.ItemsSource = value;
        }

        public object SelectedItem {
            get => fontComboFast.SelectedItem;
            set => fontComboFast.SelectedItem = value;
        }
    }
}
