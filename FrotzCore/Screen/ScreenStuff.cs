namespace Frotz.Screen;

using System.Text;

public sealed class ZKeyPressEventArgs(char kp) : EventArgs
{
    public char KeyPressed { get; private set; } = kp;
}

public readonly record struct ZPoint(int X, int Y)
{
    public static implicit operator ZPoint(ValueTuple<int, int> pair) => new(pair.Item1, pair.Item2);
}

public readonly record struct ZSize(int Height, int Width)
{
    public ZSize(double height, double width) : this(Convert.ToInt32(height), Convert.ToInt32(width)) { }

    public static implicit operator ZSize(ValueTuple<int, int> pair)
        => new(pair.Item1, pair.Item2);

    public static implicit operator ZSize(ValueTuple<double, double> pair)
        => new(pair.Item1, pair.Item2);

    public static readonly ZSize Empty = new(0, 0);
}

public readonly record struct ScreenMetrics(ZSize FontSize, ZSize WindowSize, int Rows, int Columns, int Scale)
{
    public (int Rows, int Columnns) Dimensions => (Rows, Columns);
}

public class FontChanges(int startCol, int count, CharDisplayInfo FandS)
{
    private readonly StringBuilder _sb = new(count);

    public int Offset { get; set; }
    public int StartCol { get; private set; } = startCol;
    public int Count => _sb.Length;
    public int Style => FontAndStyle.Style;
    public int Font => FontAndStyle.Font;
    public string Text => _sb.ToString();
    public int Line { get; set; }

    public CharDisplayInfo FontAndStyle { get; set; } = FandS;


    internal void AddChar(char c) => _sb.Append(c);
}
