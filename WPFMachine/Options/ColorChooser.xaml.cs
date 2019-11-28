using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFMachine.Options
{
    /// <summary>
    /// Interaction logic for ColorChooser.xaml
    /// </summary>
    public partial class ColorChooser : UserControl
    {
        public ColorChooser()
        {
            InitializeComponent();
        }

        private Color _selectedColor;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                tbColor.Background = new SolidColorBrush(value);
                _selectedColor = value;
            }
        }

        private void tbColor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var cpd = new Microsoft.Samples.CustomControls.ColorPickerDialog
            {
                StartingColor = _selectedColor
            };

            if (cpd.ShowDialog() == true)
            {
                SelectedColor = cpd.SelectedColor;
            }
        }
    }
}
