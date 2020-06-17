using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frotz.Other
{
    public static class ZMath
    {
        public static uint MakeInt(ReadOnlySpan<char> chars)
        {
            Debug.Assert(chars.Length == 4, "Must be 4 characters.");
            Span<byte> bytes = stackalloc byte[4];
            Encoding.UTF8.GetBytes(chars, bytes);
            return BinaryPrimitives.ReadUInt32BigEndian(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint MakeInt(Span<byte> bytes)
            => BinaryPrimitives.ReadUInt32BigEndian(bytes);

        internal static void ClearArray(Span<byte> bytes) => bytes.Clear();
    }
}
