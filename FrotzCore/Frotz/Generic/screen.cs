/* screen.c - Generic screen manipulation
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

using Frotz.Other;

namespace Frotz.Generic;

internal static class Screen
{
    private const int current_window = 100;

    private readonly record struct StoryInfo(Story story_id, int pic, int pic1, int pic2);

    private static readonly StoryInfo[] mapper = new StoryInfo[] {
        new(Story.ZORK_ZERO, 5, 497, 498),
        new(Story.ZORK_ZERO, 6, 501, 502),
        new(Story.ZORK_ZERO, 7, 499, 500),
        new(Story.ZORK_ZERO, 8, 503, 504),
        new(Story.ARTHUR, 54, 170, 171),
        new(Story.SHOGUN, 50, 61,62),
        new(Story.UNKNOWN, 0,0,0)
    };

    private static zword font_height = 1;
    private static zword font_width = 1;
    private static bool input_redraw = false;
    private static bool more_prompts = true;
    private static bool discarding = false;
    private static bool cursor = true;
    private static int input_window = 0;


    internal static ZWindow[] wp = new ZWindow[8];

    internal static ZWindow cwp { set; get; } = new ZWindow();

    /*
     * winarg0
     *
     * Return the window number in zargs[0]. In V6 only, -3 refers to the
     * current window.
     *
     */

    internal static zword WinArg0()
    {

        if (Main.h_version == ZMachine.V6 && (short)Process.zargs[0] == -3)
            return Main.cwin;

        if (Process.zargs[0] >= ((Main.h_version == ZMachine.V6) ? 8 : 2))
            Err.RuntimeError(ErrorCodes.ERR_ILL_WIN);

        return Process.zargs[0];

    }/* winarg0 */

    /*
     * winarg2
     *
     * Return the (optional) window number in zargs[2]. -3 refers to the
     * current window. This optional window number was only used by some
     * V6 opcodes: set_cursor, set_margins, set_colour.
     *
     */

    internal static zword WinArg2()
    {

        if (Process.zargc < 3 || (short)Process.zargs[2] == -3)
            return Main.cwin;

        if (Process.zargs[2] >= 8)
            Err.RuntimeError(ErrorCodes.ERR_ILL_WIN);

        return Process.zargs[2];

    }/* winarg2 */

    /*
     * update_cursor
     *
     * Move the hardware cursor to make it match the window properties.
     *
     */

    internal static void UpdateCursor()
    {
        OS.SetCursor(
            cwp.YPos + cwp.y_cursor - 1,
            cwp.XPos + cwp.x_cursor - 1
        );
    }/* update_cursor */

    /*
     * reset_cursor
     *
     * Reset the cursor of a given window to its initial position.
     *
     */

    private static void ResetCursor(zword win)
    {
        int lines = 0;

        if (Main.h_version <= ZMachine.V4 && win == 0)
            lines = wp[0].YSize / FastMem.Hi(wp[0].font_size) - 1;

        wp[win].y_cursor = (zword)(FastMem.Hi(wp[0].font_size) * lines + 1);
        wp[win].x_cursor = (zword)(wp[win].left + 1);

        if (win == Main.cwin)
            UpdateCursor();

    }/* reset_cursor */

    /*
     * amiga_screen_model
     *
     * Check if the Amiga screen model should be used, required for
     * some Infocom games.
     *
     */

    internal static bool AmigaScreenModel()
    {
        if (Main.h_interpreter_number == ZMachine.INTERP_AMIGA)
        {
            switch (Main.StoryId)
            {
                case Story.BEYOND_ZORK:
                case Story.ZORK_ZERO:
                case Story.SHOGUN:
                case Story.ARTHUR:
                case Story.JOURNEY:
                    return true;
            }
        }

        return false;

    }/* amiga_screen_model */


    /*
     * set_more_prompts
     *
     * Turn more prompts on/off.
     *
     */

    internal static void SetMorePrompts(bool flag)
    {

        if (flag && !more_prompts)
            cwp.line_count = 0;

        more_prompts = flag;

    }/* set_more_prompts */

    /*
     * units_left
     *
     * Return the #screen units from the cursor to the end of the line.
     *
     */

    internal static int UnitsLeft()
    {
        if (OS.WrapWindow(CwpIndex()) == 0) return 999;

        return cwp.XSize - cwp.right - cwp.x_cursor + 1;

    }/* units_left */

    /*
     * get_max_width
     *
     * Return maximum width of a line in the given window. This is used in
     * connection with the extended output stream #3 call in V6.
     *
     */

    internal static zword GetMaxWidth(zword win)
    {
        if (Main.h_version == ZMachine.V6)
        {

            if (win >= 8)
                Err.RuntimeError(ErrorCodes.ERR_ILL_WIN);

            return (zword)(wp[win].XSize - wp[win].left - wp[win].right);

        }
        else
        {
            return 0xffff;
        }
    }/* get_max_width */

    /*
     * countdown
     *
     * Decrement the newline counter. Call the newline interrupt when the
     * counter hits zero. This is a helper function for screen_new_line.
     *
     */
    internal static void Countdown()
    {
        if (cwp.nl_countdown != 0)
        {
            if (--cwp.nl_countdown == 0)
                Process.DirectCall(cwp.nl_routine);
        }
    }/* countdown */

    /*
     * screen_new_line
     *
     * Print a newline to the screen.
     *
     */

    internal static void ScreenNewline()
    {
        if (discarding) return;

        if (OS.WrapWindow(CwpIndex()) == 0)
            OS.DisplayChar('\n');

        /* Handle newline interrupts at the start (for most cases) */

        if (Main.h_interpreter_number != ZMachine.INTERP_MSDOS || Main.StoryId != Story.ZORK_ZERO || Main.h_release != 393)
            Countdown();

        /* Check whether the last input line gets destroyed */

        if (input_window == Main.cwin)
            input_redraw = true;

        /* If the cursor has not reached the bottom line, then move it to
           the next line; otherwise scroll the window or reset the cursor
           to the top left. */

        cwp.x_cursor = (zword)((cwp.left + 1));

        if (cwp.y_cursor + 2 * font_height - 1 > cwp.YSize)
        {
            if (Main.enable_scrolling)
            {

                zword y = cwp.YPos;
                zword x = cwp.XPos;

                OS.ScrollArea(y, x,
                    y + cwp.YSize - 1,
                    x + cwp.XSize - 1,
                    font_height);

            }
            else
            {
                cwp.y_cursor = 1;
            }
        }
        else
        {
            cwp.y_cursor += font_height;
        }

        UpdateCursor();

        /* See if we need to print a more prompt (unless the game has set
           the line counter to -999 in order to suppress more prompts). */

        if (Main.enable_scrolling && (short)cwp.line_count != -999)
        {
            zword above = (zword)((cwp.y_cursor - 1) / font_height);
            zword below = (zword)((cwp.YSize - cwp.y_cursor + 1) / font_height);

            cwp.line_count++;

            if ((short)cwp.line_count >= (short)above + below - 1)
            {

                if (more_prompts)
                    OS.MorePrompt();

                cwp.line_count = Main.option_context_lines;

            }
        }

        /* Handle newline interrupts at the end for Zork Zero under DOS */

        if (Main.h_interpreter_number == ZMachine.INTERP_MSDOS && Main.StoryId == Story.ZORK_ZERO && Main.h_release == 393)
            Countdown();

    }/* screen_new_line */

    /*
     * screen_char
     *
     * Display a single character on the screen.
     *
     */

    internal static void ScreenChar(zword c)
    {
        int width;

        if (discarding) return;

        if (c == CharCodes.ZC_INDENT && cwp.x_cursor != cwp.left + 1)
            c = ' ';

        if (UnitsLeft() < (width = OS.CharWidth(c)))
        {
            if (!Main.enable_wrapping)
            {
                cwp.x_cursor = (zword)(cwp.XSize - cwp.right);
                return;
            }

            ScreenNewline();

        }

        OS.DisplayChar(c); cwp.x_cursor += (zword)width;

    }/* screen_char */

    /*
     * screen_word
     *
     * Display a string of characters on the screen. If the word doesn't fit
     * then use wrapping or clipping depending on the current setting of the
     * enable_wrapping flag.
     *
     */

    internal static void ScreenWord(ReadOnlySpan<zword> buf)
    {
        int width;
        int pos = 0;

        if (discarding) return;

        if (buf[pos] == CharCodes.ZC_INDENT && cwp.x_cursor != cwp.left + 1)
            ScreenChar(buf[pos++]);

        if (UnitsLeft() < (width = OS.StringWidth(buf)))
        {

            if (!Main.enable_wrapping)
            {
                zword c;

                while (pos++ < buf.Length && (c = buf[pos]) != 0)
                {
                    if (c is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                    {
                        int arg = buf[pos++];

                        if (c == CharCodes.ZC_NEW_FONT)
                            OS.SetFont(arg);
                        if (c == CharCodes.ZC_NEW_STYLE)
                            OS.SetTextStyle(arg);
                    }
                    else
                    {
                        ScreenChar(c);
                    }
                }

                return;

            }

            if (buf[pos] is ' ' or CharCodes.ZC_INDENT or CharCodes.ZC_GAP)
                width = OS.StringWidth(buf.Slice(++pos));

            ScreenNewline();
        }

        OS.DisplayString(buf.Slice(pos));
        cwp.x_cursor += (zword)width;
    }/* screen_word */

    /*
     * screen_write_input
     *
     * Display an input line on the screen. This is required during playback.
     *
     */

    internal static void ScreenWriteInput(ReadOnlySpan<zword> buf, zword key)
    {
        int width;

        if (UnitsLeft() < (width = OS.StringWidth(buf)))
            ScreenNewline();

        OS.DisplayString(buf); cwp.x_cursor += (zword)width;

        if (key == CharCodes.ZC_RETURN)
            ScreenNewline();

    }/* screen_write_input */

    /*
     * screen_erase_input
     *
     * Remove an input line that has already been printed from the screen
     * as if it was deleted by the player. This could be necessary during
     * playback.
     *
     */

    internal static void ScreenEraseInput(ReadOnlySpan<zword> buf)
    {
        if (buf[0] != 0)
        {

            int width = OS.StringWidth(buf);

            zword y;
            zword x;

            cwp.x_cursor -= (zword)width;

            y = (zword)(cwp.YPos + cwp.y_cursor - 1);
            x = (zword)(cwp.XPos + cwp.x_cursor - 1);

            OS.EraseArea(y, x, y + font_height - 1, x + width - 1, -1);
            OS.SetCursor(y, x);

        }

    }/* screen_erase_input */

    /*
     * console_read_input
     *
     * Read an input line from the keyboard and return the terminating key.
     *
     */

    internal static zword ConsoleReadInput(int max, Span<zword> buf, zword timeout, bool continued)
    {
        zword key;
        int i;

        /* Make sure there is some space for input */

        if (Main.cwin == 0 && UnitsLeft() + OS.StringWidth(buf) < 10 * font_width)
            ScreenNewline();

        /* Make sure the input line is visible */

        if (continued && input_redraw)
            ScreenWriteInput(buf, zword.MaxValue); // TODO second value was -1, interestingly enough

        input_window = Main.cwin;
        input_redraw = false;

        /* Get input line from IO interface */

        cwp.x_cursor -= (zword)OS.StringWidth(buf);
        key = OS.ReadLine(max, buf, timeout, UnitsLeft(), continued);
        cwp.x_cursor += (zword)OS.StringWidth(buf);

        if (key != CharCodes.ZC_TIME_OUT)
        {
            for (i = 0; i < 8; i++)
                wp[i].line_count = 0;
        }

        /* Add a newline if the input was terminated normally */

        if (key == CharCodes.ZC_RETURN)
            ScreenNewline();

        return key;

    }/* console_read_input */

    /*
     * console_read_key
     *
     * Read a single keystroke and return it.
     *
     */

    internal static zword ConsoleReadKey(zword timeout)
    {
        zword key;
        int i;

        key = OS.ReadKey(timeout, cursor);

        if (key != CharCodes.ZC_TIME_OUT)
        {
            for (i = 0; i < 8; i++)
                wp[i].line_count = 0;
        }

        return key;

    }/* console_read_key */

    /*
     * update_attributes
     *
     * Set the three enable_*** variables to make them match the attributes
     * of the current window.
     *
     */

    internal static void UpdateAttributes()
    {
        zword attr = cwp.attribute;

        Main.enable_wrapping = (attr & 1) > 0;
        Main.enable_scrolling = (attr & 2) > 0;
        Main.enable_scripting = (attr & 4) > 0;
        Main.enable_buffering = (attr & 8) > 0;

        /* Some story files forget to select wrapping for printing hints */

        if (Main.StoryId == Story.ZORK_ZERO && Main.h_release == 366)
        {
            if (Main.cwin == 0)
                Main.enable_wrapping = true;
        }

        if (Main.StoryId == Story.SHOGUN && Main.h_release <= 295)
        {
            if (Main.cwin == 0)
                Main.enable_wrapping = true;
        }
    }/* update_attributes */

    /*
     * refresh_text_style
     *
     * Set the right text style. This can be necessary when the fixed font
     * flag is changed, or when a new window is selected, or when the game
     * uses the set_text_style opcode.
     *
     */

    internal static void RefreshTextStyle()
    {
        zword style;

        if (Main.h_version != ZMachine.V6)
        {

            style = wp[0].style;

            if (Main.cwin != 0 || (Main.h_flags & ZMachine.FIXED_FONT_FLAG) > 0)
                style |= ZStyles.FIXED_WIDTH_STYLE;

        }
        else
        {
            style = cwp.style;
        }

        if (!Main.ostream_memory && Main.ostream_screen && Main.enable_buffering)
        {

            Buffer.PrintChar(CharCodes.ZC_NEW_STYLE);
            Buffer.PrintChar(style);

        }
        else
        {
            OS.SetTextStyle(style);
        }
    }/* refresh_text_style */

    /*
     * set_window
     *
     * Set the current window. In V6 every window has its own set of window
     * properties such as colours, text style, cursor position and size.
     *
     */

    private static void SetWindow(zword win)
    {
        Buffer.FlushBuffer();

        Main.cwin = win; cwp = wp[win];

        UpdateAttributes();

        if (Main.h_version == ZMachine.V6)
        {
            OS.SetColor(FastMem.Lo(cwp.colour), FastMem.Hi(cwp.colour));

            if (OS.FontData(cwp.font, ref font_height, ref font_width))
                OS.SetFont(cwp.font);

            OS.SetTextStyle(cwp.style);

        }
        else
        {
            RefreshTextStyle();
        }

        if (Main.h_version != ZMachine.V6 && win != 0)
        {
            wp[win].y_cursor = 1;
            wp[win].x_cursor = 1;
        }

        UpdateCursor();

        OS.SetActiveWindow(win);

    }/* set_window */

    /*
     * erase_window
     *
     * Erase a window to background colour.
     *
     */

    internal static void EraseWindow(zword win)
    {
        zword y = wp[win].YPos;
        zword x = wp[win].XPos;

        if (Main.h_version == ZMachine.V6 && win != Main.cwin && !Screen.AmigaScreenModel())
            OS.SetColor(FastMem.Lo(wp[win].colour), FastMem.Hi(wp[win].colour));

        if (FastMem.Hi(wp[win].colour) != ZColor.TRANSPARENT_COLOUR)
        {

            OS.EraseArea(y, x,
                y + wp[win].YSize - 1,
                x + wp[win].XSize - 1,
                win);

        }

        if (Main.h_version == ZMachine.V6 && win != Main.cwin && !AmigaScreenModel())
            OS.SetColor(FastMem.Lo(cwp.colour), FastMem.Hi(cwp.colour));

        ResetCursor(win);

        wp[win].line_count = 0;

    }/* erase_window */

    /*
     * split_window
     *
     * Divide the screen into upper (1) and lower (0) windows. In V3 the upper
     * window appears below the status line.
     *
     */

    internal static void SplitWindow(zword height)
    {
        zword stat_height = 0;

        Buffer.FlushBuffer();

        /* Calculate height of status line and upper window */

        if (Main.h_version != ZMachine.V6)
            height *= FastMem.Hi(wp[1].font_size);

        if (Main.h_version <= ZMachine.V3)
        {
            stat_height = FastMem.Hi(wp[7].font_size);
            wp[7].YSize = stat_height;
            OS.SetWindowSize(7, wp[7]);
        }
        else
        {
            wp[7].YSize = 0;
        }

        /* Cursor of upper window mustn't be swallowed by the lower window */

        wp[1].y_cursor += (zword)(wp[1].YPos - 1 - stat_height);

        wp[1].YPos = (zword)(1 + stat_height);
        wp[1].YSize = height;

        if ((short)wp[1].y_cursor > (short)wp[1].YSize)
            ResetCursor(1);

        /* Cursor of lower window mustn't be swallowed by the upper window */

        wp[0].y_cursor += (zword)(wp[0].YPos - 1 - stat_height - height);

        wp[0].YPos = (zword)(1 + stat_height + height);
        wp[0].YSize = (zword)(Main.h_screen_height - stat_height - height);

        if ((short)wp[0].y_cursor < 1)
            ResetCursor(0);

        /* Erase the upper window in V3 only */

        if (Main.h_version == ZMachine.V3 && height != 0)
            EraseWindow(1);

        OS.SetWindowSize(0, wp[0]);
        OS.SetWindowSize(1, wp[1]);

    }/* split_window */

    /*
     * erase_screen
     *
     * Erase the entire screen to background colour.
     *
     */

    private static void EraseScreen(zword win)
    {
        int i;

        if (FastMem.Hi(cwp.colour) != ZColor.TRANSPARENT_COLOUR)
            OS.EraseArea(1, 1, Main.h_screen_height, Main.h_screen_width, -2);

        if (win == zword.MaxValue)
        {
            SplitWindow(0);
            SetWindow(0);
            ResetCursor(0);
        }

        for (i = 0; i < 8; i++)
            wp[i].line_count = 0;

    }/* erase_screen */

    ///*
    // * resize_screen
    // *
    // * Try to adapt the window properties to a new screen size.
    // *
    // */

    //void resize_screen (void)
    //{

    //    if (h_version != V6) {

    //    int h = wp[0].y_pos + wp[0].y_size;

    //    wp[0].x_size = h_screen_width;
    //    wp[1].x_size = h_screen_width;
    //    wp[7].x_size = h_screen_width;

    //    wp[0].y_size = h_screen_height - wp[1].y_size;
    //    if (h_version <= V3)
    //        wp[0].y_size -= hi (wp[7].font_size);

    //    if (os_font_data (TEXT_FONT, &font_height, &font_width)) {

    //        int i;
    //        for (i = 0; i < 8; i++)
    //        wp[i].font_size = (font_height << 8) | font_width;
    //    }

    //    if (cwin == 0) {

    //        int lines = wp[0].y_cursor + font_height - wp[0].y_size - 1;

    //        if (lines > 0) {

    //        if (lines % font_height != 0)
    //            lines += font_height;
    //        lines /= font_height;

    //        if (wp[0].y_cursor > (font_height * lines)) {

    //            os_scroll_area (wp[0].y_pos,
    //                    wp[0].x_pos,
    //                    h - 1,
    //                    wp[0].x_pos + wp[0].x_size - 1,
    //                    font_height * lines);
    //            wp[0].y_cursor -= (font_height * lines);
    //            update_cursor ();
    //        }
    //        }
    //    }

    //    os_window_height (0, wp[0].y_size);

    //    }

    //}/* resize_screen */

    /*
     * restart_screen
     *
     * Prepare the screen for a new game.
     *
     */
    internal static void RestartScreen()
    {
        /* Use default settings */

        OS.SetColor(Main.h_default_foreground, Main.h_default_background);

        if (OS.FontData(ZFont.TEXT_FONT, ref font_height, ref font_width))
            OS.SetFont(ZFont.TEXT_FONT);

        OS.SetTextStyle(0);

        cursor = true;

        /* Initialise window properties */

        //mwin = 1;

        for (int i = 0; i < 8; i++)
        {
            wp[i] = new ZWindow
            {
                YPos = 1,
                XPos = 1,
                YSize = 0,
                XSize = 0,
                y_cursor = 1,
                x_cursor = 1,
                left = 0,
                right = 0,
                nl_routine = 0,
                nl_countdown = 0,
                style = 0,
                colour = (ushort)((Main.h_default_background << 8) | Main.h_default_foreground),
                font = ZFont.TEXT_FONT,
                font_size = (ushort)((font_height << 8) | font_width),
                attribute = 8,
                true_fore = Main.hx_fore_colour,
                true_back = Main.hx_back_colour,

                index = i
            };
        }

        cwp = wp[0];

        /* Prepare lower/upper windows and status line */

        wp[0].attribute = 15;

        wp[0].left = Main.option_left_margin;
        wp[0].right = Main.option_right_margin;

        wp[0].XSize = Main.h_screen_width;
        wp[1].XSize = Main.h_screen_width;

        if (Main.h_version <= ZMachine.V3)
            wp[7].XSize = Main.h_screen_width;

        OS.RestartGame(ZMachine.RESTART_WPROP_SET);
        /* Clear the screen, unsplit it and select window 0 */

        Screen.EraseScreen(ushort.MaxValue);
    }/* restart_screen */

    /*
     * validate_click
     *
     * Return false if the last mouse click occured outside the current
     * mouse window; otherwise write the mouse arrow coordinates to the
     * memory of the header extension table and return true.
     *
     */
    internal static bool ValidateClick()
    {

        if (Main.mwin >= 0)
        {

            if (Main.MouseY < wp[Main.mwin].YPos || Main.MouseY >= wp[Main.mwin].YPos + wp[Main.mwin].YSize)
                return false;
            if (Main.MouseX < wp[Main.mwin].XPos || Main.MouseX >= wp[Main.mwin].XPos + wp[Main.mwin].XSize)
                return false;

        }
        else
        {

            if (Main.MouseY < 1 || Main.MouseY > Main.h_screen_height)
                return false;
            if (Main.MouseX < 1 || Main.MouseX > Main.h_screen_width)
                return false;
        }

        Main.hx_mouse_y = Main.MouseY;
        Main.hx_mouse_x = Main.MouseX;

        if (Main.h_version != ZMachine.V6)
        {
            Main.hx_mouse_y = (ushort)((Main.hx_mouse_y - 1) / Main.h_font_height + 1);
            Main.hx_mouse_x = (ushort)((Main.hx_mouse_x - 1) / Main.h_font_width + 1);
        }

        FastMem.SetHeaderExtension(ZMachine.HX_MOUSE_Y, Main.hx_mouse_y);
        FastMem.SetHeaderExtension(ZMachine.HX_MOUSE_X, Main.hx_mouse_x);

        return true;
    }/* validate_click */

    /*
     * screen_mssg_on
     *
     * Start printing a so-called debugging message. The contents of the
     * message are passed to the message stream, a Frotz specific output
     * stream with maximum priority.
     *
     */
    internal static void ScreenMssgOn()
    {

        if (Main.cwin == 0)
        {       /* messages in window 0 only */

            OS.SetTextStyle(0);

            if (cwp.x_cursor != cwp.left + 1)
                ScreenNewline();

            ScreenChar(CharCodes.ZC_INDENT);

        }
        else
        {
            discarding = true;    /* discard messages in other windows */
        }
    }/* screen_mssg_on */

    /*
     * screen_mssg_off
     *
     * Stop printing a "debugging" message.
     *
     */

    internal static void ScreenMssgOff()
    {
        if (Main.cwin == 0)
        {       /* messages in window 0 only */

            ScreenNewline();

            RefreshTextStyle();

        }
        else
        {
            discarding = false;   /* message has been discarded */
        }
    }/* screen_mssg_off */

    /*
     * z_buffer_mode, turn text buffering on/off.
     *
     *	zargs[0] = new text buffering flag (0 or 1)
     *
     */

    internal static void ZBufferMode()
    {
        /* Infocom's V6 games rarely use the buffer_mode opcode. If they do
           then only to print text immediately, without any delay. This was
           used to give the player some sign of life while the game was
           spending much time on parsing a complicated input line. (To turn
           off word wrapping, V6 games use the window_style opcode instead.)
           Today we can afford to ignore buffer_mode in V6. */

        if (Main.h_version != ZMachine.V6)
        {
            Buffer.FlushBuffer();

            zword temp = 8; // TODO No idea what this math will be like //'

            wp[0].attribute &= (zword)(~temp);

            if (Process.zargs[0] != 0)
                wp[0].attribute |= 8;

            UpdateAttributes();

        }

    }/* z_buffer_mode */

    /*
     * z_draw_picture, draw a picture.
     *
     *	zargs[0] = number of picture to draw
     *	zargs[1] = y-coordinate of top left corner
     *	zargs[2] = x-coordinate of top left corner
     *
     */

    internal static void ZDrawPicture()
    {
        zword pic = Process.zargs[0];

        zword y = Process.zargs[1];
        zword x = Process.zargs[2];

        int i;

        Buffer.FlushBuffer();

        if (y == 0)         /* use cursor line if y-coordinate is 0 */
            y = cwp.y_cursor;
        if (x == 0)         /* use cursor column if x-coordinate is 0 */
            x = cwp.x_cursor;

        y += (zword)(cwp.YPos - 1);
        x += (zword)(cwp.XPos - 1);

        /* The following is necessary to make Amiga and Macintosh story
           files work with MCGA graphics files.  Some screen-filling
           pictures of the original Amiga release like the borders of
           Zork Zero were split into several MCGA pictures (left, right
           and top borders).  We pretend this has not happened. */

        for (i = 0; mapper[i].story_id != Story.UNKNOWN; i++)
        {
            if (Main.StoryId == mapper[i].story_id && pic == mapper[i].pic)
            {
                int delta = 0;

                OS.PictureData(pic, out int height1, out int width1);
                OS.PictureData(mapper[i].pic2, out _, out int width2);

                if (Main.StoryId == Story.ARTHUR && pic == 54)
                    delta = Main.h_screen_width / 160;

                OS.DrawPicture(mapper[i].pic1, y + height1, x + delta);
                OS.DrawPicture(mapper[i].pic2, y + height1, x + width1 - width2 - delta);
            }
        }

        OS.DrawPicture(pic, y, x);

        if (Main.StoryId == Story.SHOGUN)
        {
            if (pic == 3)
            {
                OS.PictureData(59, out _, out _);
            }
        }
    }/* z_draw_picture */

    /*
     * z_erase_line, erase the line starting at the cursor position.
     *
     *	zargs[0] = 1 + #units to erase (1 clears to the end of the line)
     *
     */
    internal static void ZEraseLine()
    {
        // TODO This has never been hit...
        zword pixels = Process.zargs[0];
        zword y, x;

        Buffer.FlushBuffer();

        /* Do nothing if the background is transparent */

        if (FastMem.Hi(cwp.colour) == ZColor.TRANSPARENT_COLOUR)
            return;

        /* Clipping at the right margin of the current window */

        if (--pixels == 0 || pixels > UnitsLeft())
            pixels = (zword)UnitsLeft();

        /* Erase from cursor position */

        y = (zword)(cwp.YPos + cwp.y_cursor - 1);
        x = (zword)(cwp.XPos + cwp.x_cursor - 1);

        OS.EraseArea(y, x, y + font_height - 1, x + pixels - 1, -1);

    }/* z_erase_line */

    /*
     * z_erase_picture, erase a picture with background colour.
     *
     *	zargs[0] = number of picture to erase
     *	zargs[1] = y-coordinate of top left corner (optional)
     *	zargs[2] = x-coordinate of top left corner (optional)
     *
     */

    internal static void ZErasePicture()
    {

        zword y = Process.zargs[1];
        zword x = Process.zargs[2];

        Buffer.FlushBuffer();

        /* Do nothing if the background is transparent */

        if (FastMem.Hi(cwp.colour) == ZColor.TRANSPARENT_COLOUR)
            return;

        if (y == 0)     /* use cursor line if y-coordinate is 0 */
            y = cwp.y_cursor;
        if (x == 0)     /* use cursor column if x-coordinate is 0 */
            x = cwp.x_cursor;

        OS.PictureData(Process.zargs[0], out int height, out int width);

        y += (zword)(cwp.YPos - 1);
        x += (zword)(cwp.XPos - 1);

        OS.EraseArea(y, x, y + height - 1, x + width - 1, -1);

    }/* z_erase_picture */

    /*
     * z_erase_window, erase a window or the screen to background colour.
     *
     *	zargs[0] = window (-3 current, -2 screen, -1 screen & unsplit)
     *
     */

    internal static void ZEraseWindow()
    {

        Buffer.FlushBuffer();

        if ((short)Process.zargs[0] is -1 or -2)
            EraseScreen(Process.zargs[0]);
        else
            EraseWindow(WinArg0());

    }/* z_erase_window */

    /*
     * z_get_cursor, write the cursor coordinates into a table.
     *
     *	zargs[0] = address to write information to
     *
     */

    internal static void ZGetCursor()
    {
        zword y, x;

        Buffer.FlushBuffer();

        y = cwp.y_cursor;
        x = cwp.x_cursor;

        if (Main.h_version != ZMachine.V6)
        {   /* convert to grid positions */
            y = (zword)((y - 1) / Main.h_font_height + 1);
            x = (zword)((x - 1) / Main.h_font_width + 1);
        }

        FastMem.StoreW((zword)(Process.zargs[0] + 0), y);
        FastMem.StoreW((zword)(Process.zargs[0] + 2), x);

    }/* z_get_cursor */

    /*
     * z_get_wind_prop, store the value of a window property.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = number of window property to be stored
     *
     */

    internal static void ZGetWindProp()
    {
        Buffer.FlushBuffer();

        if (Process.zargs[1] < 16)
        {
            // Process.store(((zword*)(wp + winarg0()))[Process.zargs[1]]);
            // This is a nasty, nasty piece of code
            Process.Store(wp[WinArg0()][Process.zargs[1]]);
            // Process.store((wp[winarg0()].union[Process.zargs[1]]

        }
        else if (Process.zargs[1] == 16)
        {
            Process.Store(OS.ToTrueColor(FastMem.Lo(wp[WinArg0()].colour)));
        }
        else if (Process.zargs[1] == 17)
        {

            zword bg = FastMem.Hi(wp[WinArg0()].colour);

            if (bg == ZColor.TRANSPARENT_COLOUR)
            {
                unchecked
                {
                    Process.Store((zword)(-4));
                }
            }
            else
            {
                Process.Store(OS.ToTrueColor(bg));
            }
        }
        else
        {
            Err.RuntimeError(ErrorCodes.ERR_ILL_WIN_PROP);
        }
    }/* z_get_wind_prop */

    /*
     * z_mouse_window, select a window as mouse window.
     *
     *	zargs[0] = window number (-3 is the current) or -1 for the screen
     *
     */

    internal static void ZMouseWindow() => Main.mwin = ((short)Process.zargs[0] == -1) ? -1 : Screen.WinArg0();/* z_mouse_window */

    /*
     * z_move_window, place a window on the screen.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = y-coordinate
     *	zargs[2] = x-coordinate
     *
     */

    internal static void ZMoveWindow()
    {
        zword win = WinArg0();

        Buffer.FlushBuffer();

        wp[win].YPos = Process.zargs[1];
        wp[win].XPos = Process.zargs[2];

        if (win == Main.cwin)
            UpdateCursor();

    }/* z_move_window */

    /*
     * z_picture_data, get information on a picture or the graphics file.
     *
     *	zargs[0] = number of picture or 0 for the graphics file
     *	zargs[1] = address to write information to
     *
     */

    internal static void ZPictureData()
    {
        zword pic = Process.zargs[0];
        zword table = Process.zargs[1];

        int i;

        bool avail = OS.PictureData(pic, out int height, out int width);

        for (i = 0; mapper[i].story_id != Story.UNKNOWN; i++)
        {
            if (Main.StoryId == mapper[i].story_id)
            {

                if (pic == mapper[i].pic)
                {
                    avail &= OS.PictureData(mapper[i].pic1, out _, out _);
                    avail &= OS.PictureData(mapper[i].pic2, out int height2, out _);

                    height += height2;

                }
                else if (pic == mapper[i].pic1 || pic == mapper[i].pic2)
                {
                    avail = false;
                }
            }
        }

        FastMem.StoreW((zword)(table + 0), (zword)(height));
        FastMem.StoreW((zword)(table + 2), (zword)(width));

        Process.Branch(avail);

    }/* z_picture_data */

    /*
     * z_picture_table, prepare a group of pictures for faster display.
     *
     *	zargs[0] = address of table holding the picture numbers
     *
     */

    internal static void ZPictureTable()
    {
        /* This opcode is used by Shogun and Zork Zero when the player
           encounters built-in games such as Peggleboz. Nowadays it is
           not very helpful to hold the picture data in memory because
           even a small disk cache avoids re-loading of data. */

    }/* z_picture_table */

    /*
     * z_buffer_screen, set the screen buffering mode.
     *
     *	zargs[0] = mode
     *
     */

    internal static void ZBufferScreen()
    {
        // TODO This wants to cast the zword to a negative... I'd like to know why
        unchecked
        {
            Process.Store((zword)OS.BufferScreen((Process.zargs[0] == (zword)(-1)) ? -1 : Process.zargs[0]));
        }

    }/* z_buffer_screen */

    /*
     * z_print_table, print ASCII text in a rectangular area.
     *
     *	zargs[0] = address of text to be printed
     *	zargs[1] = width of rectangular area
     *	zargs[2] = height of rectangular area (optional)
     *	zargs[3] = number of char's to skip between lines (optional)
     *
     */
    internal static void ZPrintTable()
    {
        zword addr = Process.zargs[0];
        zword x;
        int i, j;

        Buffer.FlushBuffer();

        /* Supply default arguments */

        if (Process.zargc < 3)
            Process.zargs[2] = 1;
        if (Process.zargc < 4)
            Process.zargs[3] = 0;

        /* Write text in width x height rectangle */

        x = cwp.x_cursor;

        for (i = 0; i < Process.zargs[2]; i++)
        {

            if (i != 0)
            {

                Buffer.FlushBuffer();

                cwp.y_cursor += font_height;
                cwp.x_cursor = x;

                UpdateCursor();

            }

            for (j = 0; j < Process.zargs[1]; j++)
            {


                FastMem.LowByte(addr, out zbyte c);
                addr++;

                Buffer.PrintChar(c);

            }

            addr += Process.zargs[3];

        }

    }/* z_print_table */

    /*
     * z_put_wind_prop, set the value of a window property.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = number of window property to set
     *	zargs[2] = value to set window property to
     *
     */

    internal static void ZPutWindProp()
    {

        Buffer.FlushBuffer();

        if (Process.zargs[1] >= 16)
            Err.RuntimeError(ErrorCodes.ERR_ILL_WIN_PROP);

        // ((zword *) (wp + winarg0 ())) [zargs[1]] = zargs[2];

        // This is still a wicked evil piece of codee
        wp[WinArg0()][Process.zargs[1]] = Process.zargs[2];


    }/* z_put_wind_prop */

    /*
     * z_scroll_window, scroll a window up or down.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = #screen units to scroll up (positive) or down (negative)
     *
     */

    internal static void ZScrollWindow()
    {
        zword win = WinArg0();
        zword y, x;

        Buffer.FlushBuffer();

        /* Use the correct set of colours when scrolling the window */

        if (win != Main.cwin && !AmigaScreenModel())
            OS.SetColor(FastMem.Lo(wp[win].colour), FastMem.Hi(wp[win].colour));

        y = wp[win].YPos;
        x = wp[win].XPos;

        OS.ScrollArea(y, x,
            y + wp[win].YSize - 1,
            x + wp[win].XSize - 1,
            (short)Process.zargs[1]);

        if (win != Main.cwin && !AmigaScreenModel())
            OS.SetColor(FastMem.Lo(cwp.colour), FastMem.Hi(cwp.colour));

    }/* z_scroll_window */

    /*
     * z_set_colour, set the foreground and background colours.
     *
     *	zargs[0] = foreground colour
     *	zargs[1] = background colour
     *	zargs[2] = window (-3 is the current one, optional)
     *
     */

    internal static void ZSetColor()
    {
        zword win = (zword)((Main.h_version == ZMachine.V6) ? WinArg2() : 0);

        zword fg = Process.zargs[0];
        zword bg = Process.zargs[1];

        Buffer.FlushBuffer();

        if ((short)fg == -1)    /* colour -1 is the colour at the cursor */
            fg = OS.PeekColor();
        if ((short)bg == -1)
            bg = OS.PeekColor();

        if (fg == 0)        /* colour 0 means keep current colour */
            fg = FastMem.Lo(wp[win].colour);
        if (bg == 0)
            bg = FastMem.Hi(wp[win].colour);

        if (fg == 1)        /* colour 1 is the system default colour */
            fg = Main.h_default_foreground;
        if (bg == 1)
            bg = Main.h_default_background;

        if (fg == ZColor.TRANSPARENT_COLOUR)
            fg = FastMem.Lo(wp[win].colour);
        if (bg == ZColor.TRANSPARENT_COLOUR && ((Main.hx_flags & ZMachine.TRANSPARENT_FLAG) == 0))
            bg = FastMem.Hi(wp[win].colour);

        if (Main.h_version == ZMachine.V6 && AmigaScreenModel())
        {
            /* Changing colours of window 0 affects the entire screen */

            if (win == 0)
            {

                int i;

                for (i = 1; i < 8; i++)
                {

                    zword bg2 = FastMem.Hi(wp[i].colour);
                    zword fg2 = FastMem.Lo(wp[i].colour);

                    if (bg2 < 16)
                        bg2 = (bg2 == FastMem.Lo(wp[0].colour)) ? fg : bg;
                    if (fg2 < 16)
                        fg2 = (fg2 == FastMem.Lo(wp[0].colour)) ? fg : bg;

                    wp[i].colour = (zword)((bg2 << 8) | fg2);

                }

            }
        }

        wp[win].colour = (zword)((bg << 8) | fg);

        if (win == Main.cwin || Main.h_version != ZMachine.V6)
            OS.SetColor(fg, bg);

    }/* z_set_colour */

    /*
     * z_set_true_colour, set the foreground and background colours
     * to specific RGB colour values.
     *
     *	zargs[0] = foreground colour
     *	zargs[1] = background colour
     *	zargs[2] = window (-3 is the current one, optional)
     *
     */

    internal static void ZSetTrueColor()
    {
        zword win = (zword)((Main.h_version == ZMachine.V6) ? WinArg2() : 0);

        zword true_fg = Process.zargs[0];
        zword true_bg = Process.zargs[1];
        Buffer.FlushBuffer();

        ushort fg = ((short)true_fg) switch
        {
            /* colour -1 is the system default colour */
            -1 => Main.h_default_foreground,
            /* colour -2 means keep current colour */
            -2 => FastMem.Lo(wp[win].colour),
            /* colour -3 is the colour at the cursor */
            -3 => OS.PeekColor(),
            -4 => FastMem.Lo(wp[win].colour),
            _ => OS.FromTrueColor(true_fg),
        };

        ushort bg = ((short)true_bg) switch
        {
            /* colour -1 is the system default colour */
            -1 => Main.h_default_background,
            /* colour -2 means keep current colour */
            -2 => FastMem.Hi(wp[win].colour),
            /* colour -3 is the colour at the cursor */
            -3 => OS.PeekColor(),
            /* colour -4 means transparent */
            -4 => (Main.hx_flags & ZMachine.TRANSPARENT_FLAG) > 0
                    ? ZColor.TRANSPARENT_COLOUR : FastMem.Hi(wp[win].colour),
            _ => OS.FromTrueColor(true_bg),
        };
        wp[win].colour = (zword)((bg << 8) | fg);

        if (win == Main.cwin || Main.h_version != ZMachine.V6)
            OS.SetColor(fg, bg);

    }

    /*
     * z_set_font, set the font for text output and store the previous font.
     *
     * 	zargs[0] = number of font or 0 to keep current font
     *	zargs[1] = window (-3 is the current one, optional)
     *
     */

    internal static void ZSetFont()
    {
        zword font = Process.zargs[0];
        zword win = 0;

        if (Main.h_version == ZMachine.V6)
        {

            if (Process.zargc < 2 || (short)Process.zargs[1] == -3)
                win = Main.cwin;
            else if (Process.zargs[1] >= 8)
                Err.RuntimeError(ErrorCodes.ERR_ILL_WIN);
            else
                win = Process.zargs[1];

        }

        if (font != 0)
        {

            if (OS.FontData(font, ref font_height, ref font_width))
            {

                Process.Store(wp[win].font);

                wp[win].font = font;
                wp[win].font_size = (zword)((font_height << 8) | font_width);

                if ((Main.h_version != ZMachine.V6) || (win == Main.cwin))
                {

                    if (!Main.ostream_memory && Main.ostream_screen && Main.enable_buffering)
                    {

                        Buffer.PrintChar(CharCodes.ZC_NEW_FONT);
                        Buffer.PrintChar(font);

                    }
                    else
                    {
                        OS.SetFont(font);
                    }
                }
            }
            else
            {
                Process.Store(0);
            }
        }
        else
        {
            Process.Store(wp[win].font);
        }
    }/* z_set_font */

    /*
     * z_set_cursor, set the cursor position or turn the cursor on/off.
     *
     *	zargs[0] = y-coordinate or -2/-1 for cursor on/off
     *	zargs[1] = x-coordinate
     *	zargs[2] = window (-3 is the current one, optional)
     *
     */

    internal static void ZSetCursor()
    {
        zword win = (zword)((Main.h_version == ZMachine.V6) ? WinArg2() : 1);

        zword y = Process.zargs[0];
        zword x = Process.zargs[1];

        Buffer.FlushBuffer();

        /* Supply default arguments */

        if (Process.zargc < 3)
            Process.zargs[2] = current_window;

        /* Handle cursor on/off */

        if ((short)y < 0)
        {

            if ((short)y == -2)
                cursor = true;
            if ((short)y == -1)
                cursor = false;

            return;

        }

        /* Convert grid positions to screen units if this is not V6 */

        if (Main.h_version != ZMachine.V6)
        {

            if (Main.cwin == 0)
                return;

            y = (zword)((y - 1) * Main.h_font_height + 1);
            x = (zword)((x - 1) * Main.h_font_width + 1);

        }

        /* Protect the margins */

        if (y == 0)         /* use cursor line if y-coordinate is 0 */
            y = wp[win].y_cursor;
        if (x == 0)         /* use cursor column if x-coordinate is 0 */
            x = wp[win].x_cursor;
        if (x <= wp[win].left || x > wp[win].XSize - wp[win].right)
            x = (zword)(wp[win].left + 1);

        /* Move the cursor */

        wp[win].y_cursor = y;
        wp[win].x_cursor = x;

        if (win == Main.cwin)
            UpdateCursor();

    }/* z_set_cursor */

    /*
     * z_set_margins, set the left and right margins of a window.
     *
     *	zargs[0] = left margin in pixels
     *	zargs[1] = right margin in pixels
     *	zargs[2] = window (-3 is the current one, optional)
     *
     */

    internal static void ZSetMargins()
    {
        zword win = WinArg2();

        Buffer.FlushBuffer();

        wp[win].left = Process.zargs[0];
        wp[win].right = Process.zargs[1];

        /* Protect the margins */

        if (wp[win].x_cursor <= Process.zargs[0] || wp[win].x_cursor > wp[win].XSize - Process.zargs[1])
        {
            wp[win].x_cursor = (zword)(Process.zargs[0] + 1);

            if (win == Main.cwin)
                UpdateCursor();
        }

    }/* z_set_margins */

    /*
     * z_set_text_style, set the style for text output.
     *
     * 	zargs[0] = style flags to set or 0 to reset text style
     *
     */

    internal static void ZSetTextStyle()
    {
        zword win = (zword)((Main.h_version == ZMachine.V6) ? Main.cwin : 0);
        zword style = Process.zargs[0];

        wp[win].style |= style;

        if (style == 0)
            wp[win].style = 0;

        RefreshTextStyle();

    }/* z_set_text_style */

    /*
     * z_set_window, select the current window.
     *
     *	zargs[0] = window to be selected (-3 is the current one)
     *
     */

    internal static void ZSetWindow() => SetWindow(WinArg0());/* z_set_window */

    /*
     * pad_status_line
     *
     * Pad the status line with spaces up to the given position.
     *
     */

    private static void PadStatusLine(int column)
    {
        int spaces;

        Buffer.FlushBuffer();

        spaces = UnitsLeft() / OS.CharWidth(' ') - column;

        /* while (spaces--) */
        /* Justin Wesley's fix for narrow displays (Agenda PDA) */
        while (spaces-- > 0)
            ScreenChar(' ');

    }/* pad_status_line */

    /*
     * z_show_status, display the status line for V1 to V3 games.
     *
     *	no zargs used
     *
     */
    internal static void ZShowStatus()
    {
        zword addr;

        bool brief = false;

        /* One V5 game (Wishbringer Solid Gold) contains this opcode by
           accident, so just return if the version number does not fit */

        if (Main.h_version >= ZMachine.V4)
            return;

        /* Read all relevant global variables from the memory of the
           Z-machine into local variables */

        addr = Main.h_globals;
        FastMem.LowWord(addr, out zword global0);
        addr += 2;
        FastMem.LowWord(addr, out zword global1);
        addr += 2;
        FastMem.LowWord(addr, out zword global2);

        /* Frotz uses window 7 for the status line. Don't forget to select
           reverse and fixed width text style */

        SetWindow(7);

        Buffer.PrintChar(CharCodes.ZC_NEW_STYLE);
        Buffer.PrintChar(ZStyles.REVERSE_STYLE | ZStyles.FIXED_WIDTH_STYLE);

        /* If the screen width is below 55 characters then we have to use
           the brief status line format */

        if (Main.h_screen_cols < 55)
            brief = true;

        /* Print the object description for the global variable 0 */

        Buffer.PrintChar(' ');
        Text.PrintObject(global0);

        /* A header flag tells us whether we have to display the current
           time or the score/moves information */

        if ((Main.h_config & ZMachine.CONFIG_TIME) > 0)
        {   /* print hours and minutes */

            zword hours = (zword)((global1 + 11) % 12 + 1);

            PadStatusLine(brief ? 15 : 20);

            Text.PrintString("Time: ");

            if (hours < 10)
                Buffer.PrintChar(' ');
            Text.PrintNum(hours);

            Buffer.PrintChar(':');

            if (global2 < 10)
                Buffer.PrintChar('0');
            Text.PrintNum(global2);

            Buffer.PrintChar(' ');

            Buffer.PrintChar((global1 >= 12) ? 'p' : 'a');
            Buffer.PrintChar('m');

        }
        else
        {               /* print score and moves */

            PadStatusLine(brief ? 15 : 30);

            Text.PrintString(brief ? "S: " : "Score: ");
            Text.PrintNum(global1);

            PadStatusLine(brief ? 8 : 14);

            Text.PrintString(brief ? "M: " : "Moves: ");
            Text.PrintNum(global2);

        }

        /* Pad the end of the status line with spaces */

        PadStatusLine(0);

        /* Return to the lower window */

        SetWindow(0);

    }/* z_show_status */

    /*
     * z_split_window, split the screen into an upper (1) and lower (0) window.
     *
     *	zargs[0] = height of upper window in screen units (V6) or #lines
     *
     */

    internal static void ZSplitWindow() => SplitWindow(Process.zargs[0]);/* z_split_window */

    /*
     * z_window_size, change the width and height of a window.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = new height in screen units
     *	zargs[2] = new width in screen units
     *
     */

    internal static void ZWindowSize()
    {
        zword win = Screen.WinArg0();

        Buffer.FlushBuffer();

        wp[win].YSize = Process.zargs[1];
        wp[win].XSize = Process.zargs[2];

        /* Keep the cursor within the window */

        if (wp[win].y_cursor > Process.zargs[1] || wp[win].x_cursor > Process.zargs[2])
            ResetCursor(win);

        OS.SetWindowSize(win, wp[win]);

    }/* z_window_size */

    /*
     * z_window_style, set / clear / toggle window attributes.
     *
     *	zargs[0] = window (-3 is the current one)
     *	zargs[1] = window attribute flags
     *	zargs[2] = operation to perform (optional, defaults to 0)
     *
     */

    internal static void ZWindowStyle()
    {
        zword win = WinArg0();
        zword flags = Process.zargs[1];

        Buffer.FlushBuffer();

        /* Supply default arguments */

        if (Process.zargc < 3)
            Process.zargs[2] = 0;

        /* Set window style */

        switch (Process.zargs[2])
        {
            case 0: wp[win].attribute = flags; break;
            case 1: wp[win].attribute |= flags; break;
            case 2: wp[win].attribute &= (zword)(~flags); break;
            case 3: wp[win].attribute ^= flags; break;
        }

        if (Main.cwin == win)
            UpdateAttributes();

    }/* z_window_style */

    ///*
    // * get_window_colours
    // *
    // * Get the colours for a given window.
    // *
    // */

    //void get_window_colours (zword win, zbyte* fore, zbyte* back)
    //{

    //    *fore = lo (wp[win].colour);
    //    *back = hi (wp[win].colour);

    //}/* get_window_colours */

    /*
     * get_window_font
     *
     * Get the font for a given window.
     *
     */
    internal static zword GetWindowFont(zword win)
    {
        zword font = wp[win].font;

        if (font == ZFont.TEXT_FONT)
        {
            if (Main.h_version != ZMachine.V6)
            {

                if ((win != 0 || (Main.h_flags & ZMachine.FIXED_FONT_FLAG) > 0))

                    font = ZFont.FIXED_WIDTH_FONT;

            }
            else
            {

                if ((wp[win].style & ZStyles.FIXED_WIDTH_STYLE) > 0)

                    font = ZFont.FIXED_WIDTH_FONT;

            }
        }

        return font;

    }/* get_window_font */

    /*
     * colour_in_use
     *
     * Check if a colour is set in any window.
     *
     */
    internal static int ColorInUse(zword color)
    {
        int max = (Main.h_version == ZMachine.V6) ? 8 : 2;
        int i;

        for (i = 0; i < max; i++)
        {

            zword bg = FastMem.Hi(wp[i].colour);
            zword fg = FastMem.Lo(wp[i].colour);

            if (color == fg || color == bg)
                return 1;
        }

        return 0;

    }/* colour_in_use */

    ///*
    // * get_current_window
    // *
    // * Get the currently active window.
    // *
    // */

    //zword get_current_window (void)
    //{

    //    return cwp - wp;

    //}/* get_current_window */

    private static int CwpIndex()
    {
        for (int i = 0; i < wp.Length; i++)
        {
            if (wp[i].index == cwp.index) return i;
        }
        return -1;
    }
}
