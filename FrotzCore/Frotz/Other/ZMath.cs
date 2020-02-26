using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Frotz.Other
{
    public static class ZMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MakeInt(char a, char b, char c, char d)
            => (uint)((a << 24) | (b << 16) | (c << 8) | d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MakeInt(Span<byte> bytes)
            => BinaryPrimitives.ReadUInt32BigEndian(bytes);

        internal static void ClearArray(Span<byte> bytes) => bytes.Clear();
    }
}
