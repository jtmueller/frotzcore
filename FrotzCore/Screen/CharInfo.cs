namespace Frotz.Screen;

public struct CharInfo : IEquatable<CharInfo>
{
    public CharDisplayInfo DisplayInfo { get; }
    public readonly char Character { get; }

    public CharInfo(char character, CharDisplayInfo displayInfo)
    {
        Character = character;
        DisplayInfo = displayInfo;
    }

    public bool Equals(CharInfo other)
        => Character == other.Character && DisplayInfo == other.DisplayInfo;

    public override bool Equals(object? obj) => obj is CharInfo ci && Equals(ci);

    public override int GetHashCode() => HashCode.Combine(Character, DisplayInfo);

    public static bool operator ==(CharInfo x, CharInfo y) => x.Equals(y);
    public static bool operator !=(CharInfo x, CharInfo y) => !x.Equals(y);
}
