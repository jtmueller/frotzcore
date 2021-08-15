namespace Frotz.Screen;

public readonly record struct CharDisplayInfo(int Font, int Style, int BackgroundColor, int ForegroundColor)
{
    public bool ImplementsStyle(int styleBit) => (Style & styleBit) == styleBit;
}
