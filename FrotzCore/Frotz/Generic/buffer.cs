/* buffer.c - Text buffering and word wrapping
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
namespace Frotz.Generic;

internal static class Buffer
{
    internal static MemoryOwner<zword> buffer_var = MemoryOwner<zword>.Empty;
    internal static int bufpos = 0;
    internal static bool locked = false;

    internal static zword prev_c = 0;

    /*
     * init_buffer
     *
     * Initialize buffer variables.
     *
     */

    internal static void InitBuffer()
    {
        buffer_var?.Dispose();
        buffer_var = MemoryOwner<zword>.Allocate(General.TEXT_BUFFER_SIZE);
        bufpos = 0;
        prev_c = 0;
        locked = false;
    }

    ///*
    // * flush_buffer
    // *
    // * Copy the contents of the text buffer to the output streams.
    // *
    // */

    internal static void FlushBuffer()
    {
        /* Make sure we stop when flush_buffer is called from flush_buffer.
           Note that this is difficult to avoid as we might print a newline
           during flush_buffer, which might cause a newline interrupt, that
           might execute any arbitrary opcode, which might flush the buffer. */

        if (locked || bufpos == 0)
            return;

        /* Send the buffer to the output streams */

        buffer_var.Span[bufpos] = '\0';
        locked = true;
        Stream.StreamWord(buffer_var.Span);
        locked = false;

        /* Reset the buffer */

        bufpos = 0;
        prev_c = 0;

    }/* flush_buffer */

    /*
     * print_char
     *
     * High level output function.
     *
     */

    private static bool PrintCharFlag = false;
    internal static void PrintChar(zword c)
    {
        if (Main.message || Main.ostream_memory || Main.enable_buffering)
        {
            if (!PrintCharFlag)
            {
                /* Characters 0 and ZC_RETURN are special cases */

                if (c == CharCodes.ZC_RETURN) { NewLine(); return; }
                if (c == 0)
                    return;

                /* Flush the buffer before a whitespace or after a hyphen */

                if ((c is ' ' or CharCodes.ZC_INDENT or CharCodes.ZC_GAP) || (prev_c == '-' && c != '-'))
                    FlushBuffer();

                /* Set the flag if this is part one of a style or font change */

                if (c is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                    PrintCharFlag = true;

                /* Remember the current character code */

                prev_c = c;
            }
            else
            {
                PrintCharFlag = false;
            }

            /* Insert the character into the buffer */

            buffer_var.Span[bufpos++] = c;

            if (bufpos == General.TEXT_BUFFER_SIZE)
                Err.RuntimeError(ErrorCodes.ERR_TEXT_BUF_OVF);
        }
        else
        {
            Stream.StreamChar(c);
        }
    }/* print_char */

    /*
     * new_line
     *
     * High level newline function.
     *
     */

    internal static void NewLine()
    {

        FlushBuffer();
        Stream.NewLine();

    }/* new_line */
}
