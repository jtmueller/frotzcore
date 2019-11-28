using System;

namespace Frotz.Screen
{
    public struct CharDisplayInfo : IEquatable<CharDisplayInfo>
    {
        public int Font { get; set; }
        public int Style { get; set; }
        public int BackgroundColor { get; set; }
        public int ForegroundColor { get; set; }

        public CharDisplayInfo(int font, int style, int backgroundColor, int foregrounColor)
        {
            Font = font;
            Style = style;
            BackgroundColor = backgroundColor;
            ForegroundColor = foregrounColor;
        }

        public override string ToString() 
            => $"Font: {Font} Style: {Style}: Fore: {ForegroundColor} Back: {BackgroundColor}";

        public override bool Equals(object? obj) => obj is CharDisplayInfo cdi && Equals(cdi);

        public bool Equals(CharDisplayInfo other)
        {
            return Font == other.Font
                && Style == other.Style
                && BackgroundColor == other.BackgroundColor
                && ForegroundColor == other.ForegroundColor;
        }

        public static bool operator ==(CharDisplayInfo x, CharDisplayInfo y) => x.Equals(y);
        public static bool operator !=(CharDisplayInfo x, CharDisplayInfo y) => !x.Equals(y);

        public override int GetHashCode() => HashCode.Combine(Font, Style, BackgroundColor, ForegroundColor);

        public bool AreSame(CharDisplayInfo fs) => Equals(fs);
        public static CharDisplayInfo Empty => new CharDisplayInfo(0, 0, 0, 0);

        public bool ImplementsStyle(int styleBit) => (Style & styleBit) > 0;
    }
}
