using System.Diagnostics;

namespace Frotz.Other
{
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
        public static char GetChar(int id)
        {
            switch (id)
            {
                case 32: return ' ';
                case 33: return '←';
                case 34: return '→';
                case 35: return '/';
                case 36: return '\\';
                case 37: return ' ';
                case 38: return '─';
                case 39: return '─';
                case 40: return '│';
                case 41: return '│';
                case 42: return '┴';
                case 43: return '┬';
                case 44: return '├';
                case 45: return '┤';
                case 46: return '└';
                case 47: return '┌';
                case 48: return '┐';
                case 49: return '┘';

                // should be diagonal "y" shapes
                case 50: return '└';
                case 51: return '┌';
                case 52: return '┐';
                case 53: return '┘';

                case 54: return '█';
                case 55: return '─';
                case 56: return '─';
                case 57: return '│';
                case 58: return '│';
                case 59: return '─';
                case 60: return '─';
                case 61: return '├';
                case 62: return '┤';
                case 63: return '└';
                case 66: return '┘';

                case 64: return '┌';
                case 65: return '┐';
                case 67: return '└';
                case 68: return '┌';
                case 69: return '┐';
                case 70: return '┘';

                case 79: return ' ';
                case 80: return '░';
                case 81: return '░';
                case 82: return '▒';
                case 83: return '▒';
                case 84: return '▒';
                case 85: return '▓';
                case 86: return '▓';
                case 87: return '█';
                case 88: return '│';
                case 89: return '│';

                case 92: return '↓';
                case 93: return '↑';

                default:
                    Debug.WriteLine("Unknown char: {0}", id);
                    return '□';
            }
        }


    }
}