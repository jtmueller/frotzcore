using System.Linq;

using System.Windows.Media;

namespace WPFMachine
{
    public class FontInfo
    {
        public FontInfo(string name, double size)
        {
            Name = name;
            Size = size;

            Family = new FontFamily(name);
            Typeface = new Typeface(name);
        }

        public FontInfo(string name, double size, FontFamily family)
        {
            Name = name;
            Size = size;

            Family = family;
            Typeface = family.GetTypefaces().First();

        }

        public string Name { get; private set; }
        public FontFamily Family { get; private set; }

        private double _size;
        public double Size
        {
            get => _size;
            set
            {
                _size = value;
                PointSize = Size * (96.0 / 72.0);
            }
        }


        public double PointSize { get; private set; }
        public Typeface Typeface { get; set; }
    }
}
