using Frotz.Constants;
using System.Runtime.CompilerServices;
using zword = System.UInt16;

namespace Frotz.Other
{
    public static class TrueColorStuff
    {
        private const zword NON_STD_COLS = 238;
        private static readonly long[] s_colours;
        private static readonly long[] s_nonStdColours;
        private static zword s_nonStdIndex = 0;
        private static readonly long s_defaultFore = -1;
        private static readonly long s_defaultBack = -1;

        static TrueColorStuff()
        {
            s_colours = new long[11];
            s_nonStdColours = new long[NON_STD_COLS];

            // TODO Pass in the real default colors
            s_defaultFore = RGB(0xFF, 0xFF, 0xFF);
            s_defaultBack = RGB(0x00, 0x00, 0x80);

            s_colours[0] = RGB5ToTrue(0x0000); // black
            s_colours[1] = RGB5ToTrue(0x001D); // red
            s_colours[2] = RGB5ToTrue(0x0340); // green
            s_colours[3] = RGB5ToTrue(0x03BD); // yellow
            s_colours[4] = RGB5ToTrue(0x59A0); // blue
            s_colours[5] = RGB5ToTrue(0x7C1F); // magenta
            s_colours[6] = RGB5ToTrue(0x77A0); // cyan
            s_colours[7] = RGB5ToTrue(0x7FFF); // white
            s_colours[8] = RGB5ToTrue(0x5AD6); // light grey
            s_colours[9] = RGB5ToTrue(0x4631); // medium grey
            s_colours[10] = RGB5ToTrue(0x2D6B); // dark grey
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int RGB5ToTrue(zword five)
        {
            byte r = (byte)(five & 0x001F);
            byte g = (byte)((five & 0x03E0) >> 5);
            byte b = (byte)((five & 0x7C00) >> 10);
            return RGB(
                (byte)((r << 3) | (r >> 2)),
                (byte)((g << 3) | (g >> 2)),
                (byte)((b << 3) | (b >> 2)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int RGB(byte r, byte g, byte b) => r | g << 8 | b << 16;

        // Convert from a true colour to 5-bit RGB
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static zword TrueToRGB5(long colour)
        {
            int r = GetRValue(colour) >> 3;
            int g = GetGValue(colour) >> 3;
            int b = GetBValue(colour) >> 3;
            return (zword)(r | (g << 5) | (b << 10));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetRValue(long rgb) => LoByte(rgb);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetGValue(long rgb) => LoByte(rgb >> 8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetBValue(long rgb) => LoByte(rgb >> 16);

        public static (byte r, byte g, byte b) GetRGB(long rgb) => (LoByte(rgb), LoByte(rgb >> 8), LoByte(rgb >> 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte LoByte(long w) => (byte)(w & 0xff);

        // Get an index for a non-standard colour
        internal static zword GetColourIndex(long colour)
        {
            // Is this a standard colour?
            for (int i = 0; i < 11; i++)
            {
                if (s_colours[i] == colour)
                    return (zword)(i + ZColor.BLACK_COLOUR);
            }

            // Is this a default colour?
            if (s_defaultFore == colour)
                return 16;
            if (s_defaultBack == colour)
                return 17;

            // Is this colour already in the table?
            for (int i = 0; i < NON_STD_COLS; i++)
            {
                if (s_nonStdColours[i] == colour)
                    return (zword)(i + 18);
            }

            // Find a free colour index
            int index = -1;
            while (index == -1)
            {
                if (Generic.Screen.ColorInUse(
                    (zword)(s_nonStdIndex + 18)) == 0)
                {
                    s_nonStdColours[s_nonStdIndex] = colour;
                    index = s_nonStdIndex + 18;
                }

                s_nonStdIndex++;
                if (s_nonStdIndex >= NON_STD_COLS)
                    s_nonStdIndex = 0;
            }
            return (zword)index;
        }


        // Get a color
        public static long GetColor(int color)
        {
            // Standard colours
            if (color is >= ZColor.BLACK_COLOUR and <= ZColor.DARKGREY_COLOUR)
                return s_colours[color - ZColor.BLACK_COLOUR];

            // Default colours
            if (color == 16)
                return s_defaultFore;
            if (color == 17)
                return s_defaultBack;

            // Non standard colours
            if (color is >= 18 and < 256)
            {
                if (s_nonStdColours[color - 18] != 0xFFFFFFFF)
                    return s_nonStdColours[color - 18];
            }
            return s_colours[0];
        }
    }
}
