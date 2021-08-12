namespace Frotz.Other;

public static class GraphicsFont
{
    // Each bit is one pixel of an 8x8 square.
    // Each byte is one row of the square.

    public static string GetLines(int id) => id switch
    {
        32 => "0000000000000000",
        33 => "000004067F060400",
        34 => "000010307F301000",
        35 => "8040201008040201",
        36 => "0102040810204080",
        37 => "0000000000000000",
        38 => "00000000FF000000",
        39 => "000000FF00000000",
        40 => "1010101010101010",
        41 => "0808080808080808",
        42 => "101010FF00000000",
        43 => "00000000FF101010",
        44 => "10101010F0101010",
        45 => "080808080F080808",
        46 => "08080808F8000000",
        47 => "000000F808080808",
        48 => "0000001F10101010",
        49 => "101010101F000000",
        50 => "08080808F8040201",
        51 => "010204F808080808",
        52 => "8040201F10101010",
        53 => "101010101F204080",
        54 => "FFFFFFFFFFFFFFFF",
        55 => "FFFFFFFFFF000000",
        56 => "000000FFFFFFFFFF",
        57 => "1F1F1F1F1F1F1F1F",
        58 => "F8F8F8F8F8F8F8F8",
        92 => "000004067F060400", // TODO: should be down arrow, rotate
        93 => "000010307F301000", // TODO: should be up arrow, rotate
        _  => "FF818181818181FF"
    };

    /// <summary>
    /// Due to many rendering issues with the Beyond Zork graphics font,
    /// this method attempts to approximate Beyond Zork map and percentage bar
    /// rendering using standard font characters. It is imperfect, but better
    /// than what this code originally did.
    /// </summary>
    public static char GetChar(int id) => id switch
    {
        32 => ' ',
        33 => '←',
        34 => '→',
        35 => '/',
        36 => '\\',
        37 => ' ',
        38 => '─',
        39 => '─',
        40 => '│',
        41 => '│',
        42 => '┴',
        43 => '┬',
        44 => '├',
        45 => '┤',
        46 => '└',
        47 => '┌',
        48 => '┐',
        49 => '┘',

        // should be diagonal "y" shapes
        50 => '└',
        51 => '┌',
        52 => '┐',
        53 => '┘',

        54 => '█',
        55 => '─',
        56 => '─',
        57 => '│',
        58 => '│',
        59 => '─',
        60 => '─',
        61 => '├',
        62 => '┤',
        63 => '└',
        66 => '┘',
        64 => '┌',
        65 => '┐',
        67 => '└',
        68 => '┌',
        69 => '┐',
        70 => '┘',
        79 => ' ',
        80 => '░',
        81 => '░',
        82 => '▒',
        83 => '▒',
        84 => '▒',
        85 => '▓',
        86 => '▓',
        87 => '█',
        88 => '│',
        89 => '│',
        92 => '↓',
        93 => '↑',
        _ => '□', // Unknown char
    };


}
