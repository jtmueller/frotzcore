/* stream.c - IO stream implementation
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

using zword = System.UInt16;

internal static class Stream
{
    /*
     * scrollback_char
     *
     * Write a single character to the scrollback buffer.
     *
     */

    internal static void ScrollBackChar(zword c)
    {

        if (c == CharCodes.ZC_INDENT) { ScrollBackChar(' '); ScrollBackChar(' '); ScrollBackChar(' '); return; }
        if (c == CharCodes.ZC_GAP) { ScrollBackChar(' '); ScrollBackChar(' '); return; }

        OS.ScrollbackChar(c);
    }/* scrollback_char */

    /*
     * scrollback_word
     *
     * Write a string to the scrollback buffer.
     *
     */

    internal static void ScrollBackWord(ReadOnlySpan<zword> s)
    {
        for (int i = 0; i < s.Length && s[i] != 0; i++)
        {
            if (s[i] is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                i++;
            else
                ScrollBackChar(s[i]);
        }
    }/* scrollback_word */

    /*
     * scrollback_write_input
     *
     * Send an input line to the scrollback buffer.
     *
     */

    internal static void ScrollBackWriteInput(ReadOnlySpan<zword> buf, zword key)
    {
        int i;

        for (i = 0; i < buf.Length && buf[i] != 0; i++)
            ScrollBackChar(buf[i]);

        if (key == CharCodes.ZC_RETURN)
            ScrollBackChar('\n');
    }/* scrollback_write_input */

    /*
     * scrollback_erase_input
     *
     * Remove an input line from the scrollback buffer.
     *
     */

    internal static void ScrollbackEraseInput(ReadOnlySpan<zword> buf)
    {
        int width;
        int i;

        for (i = 0, width = 0; i < buf.Length && buf[i] != 0; i++)
            width++;

        OS.ScrollbackErase(width);
    }/* scrollback_erase_input */

    /*
     * stream_mssg_on
     *
     * Start printing a "debugging" message.
     *
     */

    internal static void StreamMssgOn()
    {

        Buffer.FlushBuffer();

        if (Main.ostream_screen)
            Screen.ScreenMssgOn();
        if (Main.ostream_script && Main.enable_scripting)
            Files.ScriptMssgOn();

        Main.message = true;

    }/* stream_mssg_on */

    /*
     * stream_mssg_off
     *
     * Stop printing a "debugging" message.
     *
     */

    internal static void StreamMssgOff()
    {

        Buffer.FlushBuffer();

        if (Main.ostream_screen)
            Screen.ScreenMssgOff();
        if (Main.ostream_script && Main.enable_scripting)
            Files.ScriptMssgOff();

        Main.message = false;

    }/* stream_mssg_off */

    /*
     * z_output_stream, open or close an output stream.
     *
     *	zargs[0] = stream to open (positive) or close (negative)
     *	zargs[1] = address to redirect output to (stream 3 only)
     *	zargs[2] = width of redirected output (stream 3 only, optional)
     *
     */

    internal static void ZOutputStream()
    {
        Buffer.FlushBuffer();

        switch ((short)Process.zargs[0])
        {
            case 1:
                Main.ostream_screen = true;
                break;
            case -1:
                Main.ostream_screen = false;
                break;
            case 2:
                if (!Main.ostream_script)
                    Files.ScriptOpen();
                break;
            case -2:
                if (Main.ostream_script)
                    Files.ScriptClose();
                break;
            case 3:
                Redirect.MemoryOpen(Process.zargs[1], Process.zargs[2], Process.zargc >= 3);
                break;
            case -3:
                Redirect.MemoryClose();
                break;
            case 4:
                if (!Main.ostream_record)
                    Files.RecordOpen();
                break;
            case -4:
                if (Main.ostream_record)
                    Files.RecordClose();
                break;
        }

    }/* z_output_stream */

    /*
     * stream_char
     *
     * Send a single character to the output stream.
     *
     */

    internal static void StreamChar(zword c)
    {
        if (Main.ostream_screen)
            Screen.ScreenChar(c);
        if (Main.ostream_script && Main.enable_scripting)
            Files.ScriptChar(c);
        if (Main.enable_scripting)
            ScrollBackChar(c);

    }/* stream_char */

    /*
     * stream_word
     *
     * Send a string of characters to the output streams.
     *
     */

    internal static void StreamWord(ReadOnlySpan<zword> s)
    {
        if (Main.ostream_memory && !Main.message)
        {
            Redirect.MemoryWord(s);
        }
        else
        {

            if (Main.ostream_screen)
                Screen.ScreenWord(s);
            if (Main.ostream_script && Main.enable_scripting)
                Files.ScriptWord(s);
            if (Main.enable_scripting)
                Stream.ScrollBackWord(s);
        }
    }/* stream_word */

    /*
     * stream_new_line
     *
     * Send a newline to the output streams.
     *
     */

    internal static void NewLine()
    {

        if (Main.ostream_memory && !Main.message)
        {
            Redirect.MemoryNewline();
        }
        else
        {
            if (Main.ostream_screen)
                Screen.ScreenNewline();
            if (Main.ostream_script && Main.enable_scripting)
                Files.ScriptNewLine();
            if (Main.enable_scripting)
                OS.ScrollbackChar('\n');
        }
    }/* stream_new_line */

    /*
     * z_input_stream, select an input stream.
     *
     *	zargs[0] = input stream to be selected
     *
     */

    internal static void ZIputStream()
    {

        Buffer.FlushBuffer();

        if (Process.zargs[0] == 0 && Main.istream_replay)
            Files.ReplayClose();
        if (Process.zargs[0] == 1 && !Main.istream_replay)
            Files.ReplayOpen();

    }/* z_input_stream */

    /*
     * stream_read_key
     *
     * Read a single keystroke from the current input stream.
     *
     */

    internal static zword StreamReadKey(zword timeout, zword routine, bool hot_keys)
    {
        zword key = CharCodes.ZC_BAD;
        Buffer.FlushBuffer();

    /* Read key from current input stream */

    continue_input:

        do
        {

            key = Main.istream_replay ? Files.ReplayReadKey() : Screen.ConsoleReadKey(timeout);

        } while (key == CharCodes.ZC_BAD);

        /* Verify mouse clicks */

        if (key is CharCodes.ZC_SINGLE_CLICK or CharCodes.ZC_DOUBLE_CLICK)
        {
            if (!Screen.ValidateClick())
                goto continue_input;
        }

        /* Copy key to the command file */

        if (Main.ostream_record && !Main.istream_replay)
            Files.RecordWriteKey(key);

        /* Handle timeouts */

        if (key == CharCodes.ZC_TIME_OUT)
        {
            if (Process.DirectCall(routine) == 0)
                goto continue_input;
        }

        /* Handle hot keys */

        if (hot_keys && key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
        {

            if (Main.h_version == ZMachine.V4 && key == CharCodes.ZC_HKEY_UNDO)
                goto continue_input;
            if (!Hotkey.HandleHotkey(key))
                goto continue_input;

        }

        /* Return key */
        return key;
    }/* stream_read_key */

    /*
     * stream_read_input
     *
     * Read a line of input from the current input stream.
     *
     */

    internal static zword StreamReadInput(int max, Span<zword> buf, zword timeout, zword routine, bool hot_keys, bool no_scripting)
    {
        zword key = CharCodes.ZC_BAD;
        bool no_scrollback = no_scripting;

        if (Main.h_version == ZMachine.V6 && Main.StoryId == Story.UNKNOWN && !Main.ostream_script)
            no_scrollback = false;

        Buffer.FlushBuffer();

        /* Remove initial input from the transscript file or from the screen */

        if (Main.ostream_script && Main.enable_scripting && !no_scripting)
            Files.ScriptEraseInput(buf);
        if (Main.enable_scripting && !no_scrollback)
            Stream.ScrollbackEraseInput(buf);
        if (Main.istream_replay)
            Screen.ScreenEraseInput(buf);

        /* Read input line from current input stream */

        continue_input:

        do
        {
            key = Main.istream_replay
                ? Files.ReplayReadInput(buf)
                : Screen.ConsoleReadInput(max, buf, timeout, key != CharCodes.ZC_BAD);

        } while (key == CharCodes.ZC_BAD);

        /* Verify mouse clicks */

        if (key is CharCodes.ZC_SINGLE_CLICK or CharCodes.ZC_DOUBLE_CLICK)
        {
            if (!Screen.ValidateClick())
                goto continue_input;
        }

        /* Copy input line to the command file */

        if (Main.ostream_record && !Main.istream_replay)
            Files.RecordWriteInput(buf, key);

        /* Handle timeouts */

        if (key == CharCodes.ZC_TIME_OUT)
        {
            if (Process.DirectCall(routine) == 0)
                goto continue_input;
        }

        /* Handle hot keys */

        if (hot_keys && key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
        {
            if (!Hotkey.HandleHotkey(key))
                goto continue_input;

            return CharCodes.ZC_BAD;
        }

        /* Copy input line to transscript file or to the screen */

        if (Main.ostream_script && Main.enable_scripting && !no_scripting)
            Files.ScriptWriteInput(buf, key);
        if (Main.enable_scripting && !no_scrollback)
            ScrollBackWriteInput(buf, key);
        if (Main.istream_replay)
            Screen.ScreenWriteInput(buf, key);

        /* Return terminating key */

        return key;

    }/* stream_read_input */
}
