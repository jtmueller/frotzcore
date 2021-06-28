namespace Frotz.Other
{
    public static class CRC
    {
        /* Table of CRCs of all 8-bit messages. */
        private static readonly Lazy<ulong[]> CRC_TABLE = new(() =>
        {
            ulong[] crc_table = new ulong[256];
            for (int n = 0; n < 256; n++)
            {
                ulong c = (ulong)n;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) > 0 ? 0xedb8_8320L ^ (c >> 1) : c >> 1;
                }
                crc_table[n] = c;
            }
            return crc_table;
        });

        /* Update a running CRC with the bytes buf[0..len-1]--the CRC
           should be initialized to all 1's, and the transmitted value
           is the 1's complement of the final running CRC (see the
           crc() routine below)). */
        private static ulong UpdateCRC(ulong crc, Span<byte> buf)
        {
            ulong c = crc;
            var crc_table = CRC_TABLE.Value;
            for (int n = 0, len = buf.Length; n < len; n++)
            {
                c = crc_table[(c ^ buf[n]) & 0xff] ^ (c >> 8);
            }
            return c;
        }

        /* Return the CRC of the bytes buf[0..len-1]. */
        public static ulong Calculate(Span<byte> buf) => UpdateCRC(0xffff_ffffL, buf) ^ 0xffff_ffffL;
    }
}