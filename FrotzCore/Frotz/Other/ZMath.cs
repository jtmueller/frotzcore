using System;
using System.Runtime.CompilerServices;

namespace Frotz.Other
{
    using zlong = System.UInt32;

    public static class ZMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static zlong MakeInt(char a, char b, char c, char d)
            => (zlong)((a << 24) | (b << 16) | (c << 8) | d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static zlong MakeInt(byte a, byte b, byte c, byte d) 
            => (zlong)((a << 24) | (b << 16) | (c << 8) | d);

        internal static void ClearArray(byte[] a) => Array.Clear(a, 0, a.Length);
    }
}
