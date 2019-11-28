using Microsoft.IO;
using System;
using System.IO;
using System.Text;

namespace Frotz
{
    public static class Extensions
    {
        public static int IndexOf(this StringBuilder sb, char searchChar)
        {
            for (int i = 0, len = sb.Length; i < len; i++)
            {
                if (sb[i] == searchChar)
                    return i;
            }

            return -1;
        }

        public static MemoryStream GetStream(this RecyclableMemoryStreamManager manager, string tag, ReadOnlySpan<byte> bytes)
        {
            var stream = manager.GetStream(tag, bytes.Length);
            stream.Write(bytes);
            stream.Position = 0L;
            return stream;
        }
    }
}
