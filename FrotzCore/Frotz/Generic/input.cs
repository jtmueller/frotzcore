/* input.c - High level input functions
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
using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class Input
    {

        //zword unicode_tolower (zword);

        /*
         * is_terminator
         *
         * Check if the given key is an input terminator.
         *
         */
        internal static bool IsTerminator(zword key)
        {
            if (key == CharCodes.ZC_TIME_OUT)
                return true;
            if (key == CharCodes.ZC_RETURN)
                return true;
            if (key is >= CharCodes.ZC_HKEY_MIN and <= CharCodes.ZC_HKEY_MAX)
                return true;

            if (Main.h_terminating_keys != 0)
            {
                if (key is >= CharCodes.ZC_ARROW_MIN and <= CharCodes.ZC_MENU_CLICK)
                {

                    zword addr = Main.h_terminating_keys;
                    zbyte c;

                    do
                    {
                        FastMem.LowByte(addr, out c);
                        if (c == 0xff || key == Text.TranslateFromZscii(c))
                            return true;
                        addr++;
                    } while (c != 0);

                }
            }

            return false;

        }/* is_terminator */

        /*
         * z_make_menu, add or remove a menu and branch if successful.
         *
         *	zargs[0] = number of menu
         *	zargs[1] = table of menu entries or 0 to remove menu
         *
         */
        private static readonly zword[] menu = new zword[32];

        internal static void ZMakeMenu()
        {
            /* This opcode was only used for the Macintosh version of Journey.
               It controls menus with numbers greater than 2 (menus 0, 1 and 2
               are system menus). */

            if (Process.zargs[0] < 3)
            {
                Process.Branch(false);
                return;
            }

            if (Process.zargs[1] != 0)
            {
                FastMem.LowWord(Process.zargs[1], out zword items);

                for (int i = 0; i < items; i++)
                {
                    FastMem.LowWord(Process.zargs[1] + 2 + (2 * i), out zword item);
                    FastMem.LowByte(item, out zbyte length);

                    if (length > 31)
                        length = 31;
                    menu[length] = 0;

                    for (int j = 0; j < length; j++)
                    {
                        FastMem.LowByte(item + j + 1, out zbyte c);
                        menu[j] = Text.TranslateFromZscii(c);
                    }

                    if (i == 0)
                        OS.Menu(ZMachine.MENU_NEW, Process.zargs[0], menu);
                    else
                        OS.Menu(ZMachine.MENU_ADD, Process.zargs[0], menu);
                }
            }
            else
            {
                OS.Menu(ZMachine.MENU_REMOVE, Process.zargs[0], Array.Empty<zword>());
            }

            Process.Branch(true);

        }/* z_make_menu */

        /*
         * read_yes_or_no
         *
         * Ask the user a question; return true if the answer is yes.
         *
         */
        internal static bool ReadYesOrNo(string s)
        {
            zword key;

            Text.PrintString(s);
            Text.PrintString("? (y/n) >");

            key = Stream.StreamReadKey(0, 0, false);

            if (key is 'y' or 'Y')
            {
                Text.PrintString("y\n");
                return true;
            }
            else
            {
                Text.PrintString("n\n");
                return false;
            }

        }/* read_yes_or_no */

        /*
         * read_string
         *
         * Read a string from the current input stream.
         *
         */

        internal static void ReadString(int max, Span<zword> buffer)
        {
            zword key;
            buffer[0] = 0;

            do
            {
                key = Stream.StreamReadInput(max, buffer, 0, 0, false, false);
            } while (key != CharCodes.ZC_RETURN);

        }/* read_string */

        /*
         * read_number
         *
         * Ask the user to type in a number and return it.
         *
         */

        internal static int ReadNumber()
        {
            Span<zword> buffer = stackalloc zword[6];
            int value = 0;
            int i;

            Input.ReadString(5, buffer);

            for (i = 0; buffer[i] != 0; i++)
            {
                if (buffer[i] is >= '0' and <= '9')
                    value = 10 * value + buffer[i] - '0';
            }

            return value;

        }/* read_number */

        /*
         * z_read, read a line of input and (in V5+) store the terminating key.
         *
         *	zargs[0] = address of text buffer
         *	zargs[1] = address of token buffer
         *	zargs[2] = timeout in tenths of a second (optional)
         *	zargs[3] = packed address of routine to be called on timeout
         *
         */
        internal static void ZRead()
        {
            using var pooled = SpanOwner<zword>.Allocate(General.INPUT_BUFFER_SIZE);
            var buffer = pooled.Span;
            zword addr;
            zword key;
            zbyte size;
            int i;

            /* Supply default arguments */

            if (Process.zargc < 3)
                Process.zargs[2] = 0;

            /* Get maximum input size */

            addr = Process.zargs[0];

            FastMem.LowByte(addr, out zbyte max);

            if (Main.h_version <= ZMachine.V4)
                max--;

            if (max >= General.INPUT_BUFFER_SIZE)
                max = General.INPUT_BUFFER_SIZE - 1;

            /* Get initial input size */

            if (Main.h_version >= ZMachine.V5)
            {
                addr++;
                FastMem.LowByte(addr, out size);
            }
            else
            {
                size = 0;
            }

            /* Copy initial input to local buffer */

            for (i = 0; i < size; i++)
            {
                addr++;
                FastMem.LowByte(addr, out zbyte c);
                buffer[i] = Text.TranslateFromZscii(c);
            }

            buffer[i] = 0;

            /* Draw status line for V1 to V3 games */

            if (Main.h_version <= ZMachine.V3)
                Screen.ZShowStatus();

            /* Read input from current input stream */

            key = Stream.StreamReadInput(
                max, buffer,        /* buffer and size */
                Process.zargs[2],       /* timeout value   */
                Process.zargs[3],       /* timeout routine */
                true,               /* enable hot keys */
                Main.h_version == ZMachine.V6); /* no script in V6 */

            if (key == CharCodes.ZC_BAD)
                return;

            /* Perform save_undo for V1 to V4 games */

            if (Main.h_version <= ZMachine.V4)
                FastMem.SaveUndo();

            /* Copy local buffer back to dynamic memory */

            for (i = 0; buffer[i] != 0; i++)
            {
                if (key == CharCodes.ZC_RETURN)
                {
                    buffer[i] = Text.UnicodeToLower(buffer[i]);
                }

                FastMem.StoreB((zword)(Process.zargs[0] + ((Main.h_version <= ZMachine.V4) ? 1 : 2) + i), Text.TranslateToZscii(buffer[i]));

            }

            /* Add null character (V1-V4) or write input length into 2nd byte */

            if (Main.h_version <= ZMachine.V4)
                FastMem.StoreB((zword)(Process.zargs[0] + 1 + i), 0);
            else
                FastMem.StoreB((zword)(Process.zargs[0] + 1), (byte)i);

            /* Tokenise line if a token buffer is present */

            if (key == CharCodes.ZC_RETURN && Process.zargs[1] != 0)
                Text.TokeniseLine(Process.zargs[0], Process.zargs[1], 0, false);

            /* Store key */

            if (Main.h_version >= ZMachine.V5)
                Process.Store(Text.TranslateToZscii(key));
        }/* z_read */

        /*
         * z_read_char, read and store a key.
         *
         *	zargs[0] = input device (must be 1)
         *	zargs[1] = timeout in tenths of a second (optional)
         *	zargs[2] = packed address of routine to be called on timeout
         *
         */

        internal static void ZReadChar()
        {
            zword key;

            /* Supply default arguments */

            if (Process.zargc < 2)
                Process.zargs[1] = 0;

            /* Read input from the current input stream */

            key = Stream.StreamReadKey(
                Process.zargs[1],	/* timeout value   */
                Process.zargs[2],	/* timeout routine */
                true);  	/* enable hot keys */

            if (key == CharCodes.ZC_BAD)
                return;

            /* Store key */

            Process.Store(Text.TranslateToZscii(key));

        }/* z_read_char */

        /*
         * z_read_mouse, write the current mouse status into a table.
         *
         *	zargs[0] = address of table
         *
         */

        internal static void ZReadMouse()
        {
            /* Read the mouse position, the last menu click
               and which buttons are down */

            zword btn = OS.ReadMouse();
            Main.hx_mouse_y = Main.MouseY;
            Main.hx_mouse_x = Main.MouseX;

            FastMem.StoreW((zword)(Process.zargs[0] + 0), Main.hx_mouse_y);
            FastMem.StoreW((zword)(Process.zargs[0] + 2), Main.hx_mouse_x);
            FastMem.StoreW((zword)(Process.zargs[0] + 4), btn);		/* mouse button bits */
            FastMem.StoreW((zword)(Process.zargs[0] + 6), Main.menu_selected);	/* menu selection */

        }/* z_read_mouse */
    }
}