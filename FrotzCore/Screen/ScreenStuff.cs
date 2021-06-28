using System.Text;

namespace Frotz.Screen
{
    public sealed class ZKeyPressEventArgs : EventArgs
    {
        public char KeyPressed { get; private set; }

        public ZKeyPressEventArgs(char KeyPressed)
        {
            this.KeyPressed = KeyPressed;
        }
    }

    public readonly struct ZPoint : IEquatable<ZPoint>
    {
        public readonly int X;
        public readonly int Y;

        public ZPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        public static implicit operator ZPoint(ValueTuple<int, int> pair) => new(pair.Item1, pair.Item2);

        public ZPoint WithX(int x) => new(x, Y);
        public ZPoint WithY(int y) => new(X, y);
        public bool Equals(ZPoint other) => other.X == X && other.Y == Y;
        public override bool Equals(object? obj) => obj is ZPoint p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(ZPoint left, ZPoint right) => left.Equals(right);

        public static bool operator !=(ZPoint left, ZPoint right) => !left.Equals(right);
    }

    public readonly struct ZSize : IEquatable<ZSize>
    {
        public readonly int Height;
        public readonly int Width;

        public ZSize(int height, int width)
        {
            Height = height;
            Width = width;
        }

        public ZSize(double height, double width)
        {
            Height = Convert.ToInt32(Math.Ceiling(height));
            Width = Convert.ToInt32(Math.Ceiling(width));
        }

        public void Deconstruct(out int height, out int width)
        {
            height = Height;
            width = Width;
        }

        public bool Equals(ZSize other) => other.Height == Height && other.Width == Width;
        public override bool Equals(object? obj) => obj is ZSize s && Equals(s);
        public override int GetHashCode() => HashCode.Combine(Height, Width);

        public static bool operator ==(ZSize left, ZSize right) => left.Equals(right);

        public static bool operator !=(ZSize left, ZSize right) => !left.Equals(right);

        public static implicit operator ZSize(ValueTuple<int, int> pair)
            => new(pair.Item1, pair.Item2);

        public static implicit operator ZSize(ValueTuple<double, double> pair)
            => new(pair.Item1, pair.Item2);

        public static readonly ZSize Empty = new(0, 0);
    }

    public readonly struct ScreenMetrics : IEquatable<ScreenMetrics>
    {
        public readonly ZSize FontSize;
        public readonly ZSize WindowSize;
        public readonly int Rows;
        public readonly int Columns;
        public readonly int Scale;

        public ScreenMetrics(ZSize fontSize, ZSize windowSize, int rows, int columns, int scale)
        {
            FontSize = fontSize;
            WindowSize = windowSize;
            Rows = rows;
            Columns = columns;
            Scale = scale;
        }

        public void Deconstruct(out int rows, out int cols)
        {
            rows = Rows;
            cols = Columns;
        }

        public bool Equals(ScreenMetrics other)
        {
            return other.Rows == Rows
                && other.Columns == Columns
                && other.Scale == Scale
                && other.FontSize == FontSize
                && other.WindowSize == WindowSize;
        }

        public override bool Equals(object? obj) => obj is ScreenMetrics m && Equals(m);
        public override int GetHashCode() => HashCode.Combine(Rows, Columns, Scale, FontSize, WindowSize);

        public static bool operator ==(ScreenMetrics left, ScreenMetrics right) => left.Equals(right);

        public static bool operator !=(ScreenMetrics left, ScreenMetrics right) => !left.Equals(right);
    }

    public class FontChanges
    {
        public int Offset { get; set; }
        public int StartCol { get; private set; }
        public int Count => _sb.Length;
        public int Style => FontAndStyle.Style;
        public int Font => FontAndStyle.Font;
        public string Text => _sb.ToString();
        public int Line { get; set; }

        public CharDisplayInfo FontAndStyle { get; set; }

        private readonly StringBuilder _sb;

        internal void AddChar(char c) => _sb.Append(c);

        public FontChanges(int startCol, int count, CharDisplayInfo FandS)
        {
            StartCol = startCol;
            FontAndStyle = FandS;
            _sb = new(count);
        }
    }
}
