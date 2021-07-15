/* redirect.c - Output redirection to Z-machine memory
 *	Copyright (c) 1995-1997 Stefan Jokisch
 *
 * This file is part of Frotz.
 *
 * Frotz is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * Frotz is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA
 */
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class Redirect
    {

        private const byte MAX_NESTING = 16;
        private static int depth = -1;

        private record struct RedirectStruct(zword XSize, zword Table, zword Width, zword Total);

        private static readonly RedirectStruct[] redirect = new RedirectStruct[MAX_NESTING];

        /*
         * memory_open
         *
         * Begin output redirection to the memory of the Z-machine.
         *
         */

        internal static void MemoryOpen(zword table, zword xsize, bool buffering)
        {
            if (++depth < MAX_NESTING)
            {
                if (!buffering)
                    xsize = 0xffff;
                if (buffering && (short)xsize <= 0)
                    xsize = Screen.GetMaxWidth((zword)(-(short)xsize));

                FastMem.StoreW(table, 0);

                redirect[depth] = new(xsize, table, 0, 0);

                Main.ostream_memory = true;
            }
            else
            {
                Err.RuntimeError(ErrorCodes.ERR_STR3_NESTING);
            }
        }/* memory_open */

        /*
         * memory_new_line
         *
         * Redirect a newline to the memory of the Z-machine.
         *
         */

        internal static void MemoryNewline()
        {
            zword addr;

            redirect[depth].Total += redirect[depth].Width;
            redirect[depth].Width = 0;

            addr = redirect[depth].Table;

            FastMem.LowWord(addr, out zword size);
            addr += 2;

            if (redirect[depth].XSize != 0xffff)
            {
                redirect[depth].Table = (zword)(addr + size);
                size = 0;
            }
            else
            {
                FastMem.StoreB((zword)(addr + (size++)), 13);
            }

            FastMem.StoreW(redirect[depth].Table, size);
        }/* memory_new_line */

        /*
         * memory_word
         *
         * Redirect a string of characters to the memory of the Z-machine.
         *
         */

        internal static void MemoryWord(ReadOnlySpan<zword> s)
        {
            zword addr;
            zword c;

            int pos = 0;

            if (Main.h_version == ZMachine.V6)
            {
                int width = OS.StringWidth(s);

                if (redirect[depth].XSize != 0xffff)
                {
                    if (redirect[depth].Width + width > redirect[depth].XSize)
                    {
                        if (s[pos] is ' ' or CharCodes.ZC_INDENT or CharCodes.ZC_GAP)
                            width = OS.StringWidth(s[++pos..]);

                        MemoryNewline();
                    }
                }

                redirect[depth].Width += (zword)width;
            }

            addr = redirect[depth].Table;

            FastMem.LowWord(addr, out zword size);
            addr += 2;

            while ((c = s[pos++]) != 0)
                FastMem.StoreB((zword)(addr + (size++)), Text.TranslateToZscii(c));

            FastMem.StoreW(redirect[depth].Table, size);

        }/* memory_word */

        /*
         * memory_close
         *
         * End of output redirection.
         *
         */

        internal static void MemoryClose()
        {
            if (depth >= 0)
            {
                if (redirect[depth].XSize != 0xffff)
                    MemoryNewline();

                if (Main.h_version == ZMachine.V6)
                {
                    Main.h_line_width = (redirect[depth].XSize != 0xffff) ?
                    redirect[depth].Total : redirect[depth].Width;

                    FastMem.SetWord(ZMachine.H_LINE_WIDTH, Main.h_line_width);
                }

                if (depth == 0)
                    Main.ostream_memory = false;

                depth--;
            }

        }/* memory_close */
    }
}