/* text.c - Text manipulation functions
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
using Frotz.Constants;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Text;
using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class Text
    {
        private enum StringType
        {
            LOW_STRING, ABBREVIATION, HIGH_STRING, EMBEDDED_STRING, VOCABULARY
        };

        private const string _alphabet = " ^0123456789.,!?_#'\"/\\-:()";

        private static zword[]? Decoded = null;
        private static zword[]? Encoded = null;
        private static int Resolution;


        /* 
         * According to Matteo De Luigi <matteo.de.luigi@libero.it>, 
         * 0xab and 0xbb were in each other's proper positions.
         *   Sat Apr 21, 2001
         */
        private static readonly zword[] zscii_to_latin1 = {
            0x0e4, 0x0f6, 0x0fc, 0x0c4, 0x0d6, 0x0dc, 0x0df, 0x0bb,
            0x0ab, 0x0eb, 0x0ef, 0x0ff, 0x0cb, 0x0cf, 0x0e1, 0x0e9,
            0x0ed, 0x0f3, 0x0fa, 0x0fd, 0x0c1, 0x0c9, 0x0cd, 0x0d3,
            0x0da, 0x0dd, 0x0e0, 0x0e8, 0x0ec, 0x0f2, 0x0f9, 0x0c0,
            0x0c8, 0x0cc, 0x0d2, 0x0d9, 0x0e2, 0x0ea, 0x0ee, 0x0f4,
            0x0fb, 0x0c2, 0x0ca, 0x0ce, 0x0d4, 0x0db, 0x0e5, 0x0c5,
            0x0f8, 0x0d8, 0x0e3, 0x0f1, 0x0f5, 0x0c3, 0x0d1, 0x0d5,
            0x0e6, 0x0c6, 0x0e7, 0x0c7, 0x0fe, 0x0f0, 0x0de, 0x0d0,
            0x0a3, 0x153, 0x152, 0x0a1, 0x0bf
        };

        /*
         * init_text
         *
         * Initialize text variables.
         *
         */

        internal static void InitText()
        {
            Decoded = null;
            Encoded = null;

            Resolution = 0;
        }

        /*
         * translate_from_zscii
         *
         * Map a ZSCII character into Unicode.
         *
         */

        internal static zword TranslateFromZscii(zbyte c)
        {
            if (c == 0xfc)
                return CharCodes.ZC_MENU_CLICK;
            if (c == 0xfd)
                return CharCodes.ZC_DOUBLE_CLICK;
            if (c == 0xfe)
                return CharCodes.ZC_SINGLE_CLICK;

            if (c >= 0x9b && Main.StoryId != Story.BEYOND_ZORK)
            {
                if (Main.hx_unicode_table != 0)
                {	/* game has its own Unicode table */

                    FastMem.LowByte(Main.hx_unicode_table, out zbyte N);

                    if (c - 0x9b < N)
                    {

                        zword addr = (zword)(Main.hx_unicode_table + 1 + 2 * (c - 0x9b));

                        FastMem.LowWord(addr, out zword unicode);

                        if (unicode < 0x20)
                            return '?';

                        return unicode;

                    }
                    else
                    {
                        return '?';
                    }
                }
                else if (c <= 0xdf) /* game uses standard set */
                {
                    return zscii_to_latin1[c - 0x9b];
                }
                else
                {
                    return '?';
                }
            }

            return c;

        }/* translate_from_zscii */

        /*
         * unicode_to_zscii
         *
         * Convert a Unicode character to ZSCII, returning 0 on failure.
         *
         */

        internal static zbyte UnicodeToZscii(zword c)
        {
            int i;

            if (c >= CharCodes.ZC_LATIN1_MIN)
            {
                if (Main.hx_unicode_table != 0)
                {	/* game has its own Unicode table */

                    FastMem.LowByte(Main.hx_unicode_table, out zbyte N);

                    for (i = 0x9b; i < 0x9b + N; i++)
                    {
                        zword addr = (zword)(Main.hx_unicode_table + 1 + 2 * (i - 0x9b));

                        FastMem.LowWord(addr, out zword unicode);

                        if (c == unicode)
                            return (zbyte)i;
                    }

                    return 0;
                }
                else
                {   /* game uses standard set */
                    for (i = 0x9b; i <= 0xdf; i++)
                    {
                        if (c == zscii_to_latin1[i - 0x9b])
                            return (zbyte)i;
                    }

                    return 0;

                }
            }
            return (zbyte)c;

        }/* unicode_to_zscii */

        /*
         * translate_to_zscii
         *
         * Map a Unicode character onto the ZSCII alphabet.
         *
         */

        internal static zbyte TranslateToZscii(zword c)
        {

            if (c == CharCodes.ZC_SINGLE_CLICK)
                return 0xfe;
            if (c == CharCodes.ZC_DOUBLE_CLICK)
                return 0xfd;
            if (c == CharCodes.ZC_MENU_CLICK)
                return 0xfc;
            if (c == 0)
                return 0;

            c = UnicodeToZscii(c);
            if (c == 0)
                c = '?';

            return (zbyte)c;

        }/* translate_to_zscii */

        /*
         * alphabet
         *
         * Return a character from one of the three character sets.
         *
         */

        private static zword Alphabet(int set, int index)
        {
            if (Main.h_version > ZMachine.V1 && set == 2 && index == 1)
                return 0x0D;		/* always newline */

            if (Main.h_alphabet != 0)
            {	/* game uses its own alphabet */

                zword addr = (zword)(Main.h_alphabet + 26 * set + index);
                FastMem.LowByte(addr, out zbyte c);
                return TranslateFromZscii(c);
            }
            else			/* game uses default alphabet */

            if (set == 0)
            {
                return (zword)('a' + index);
            }
            else if (set == 1)
            {
                return (zword)('A' + index);
            }
            else if (Main.h_version == ZMachine.V1)
            {
                return _alphabet[index];
            }
            else
            {
                return _alphabet[index];
            }
        }/* alphabet */

        /*
         * find_resolution
         *
         * Find the number of bytes used for dictionary resolution.
         *
         */

        internal static void FindResolution()
        {
            zword dct = Main.h_dictionary;

            FastMem.LowByte(dct, out zbyte sep_count);
            dct += (zword)(1 + sep_count);  /* skip word separators */
            FastMem.LowByte(dct, out zbyte entry_len);

            Resolution = (Main.h_version <= ZMachine.V3) ? 2 : 3;

            if (2 * Resolution > entry_len)
            {
                Err.RuntimeError(ErrorCodes.ERR_DICT_LEN);
            }

            Decoded = new zword[3 * Resolution + 1];
            Encoded = new zword[Resolution];
        }/* find_resolution */

        /*
         * load_string
         *
         * Copy a ZSCII string from the memory to the global "decoded" string.
         *
         */

        internal static void LoadString(zword addr, zword length)
        {
            if (Decoded is null)
                ThrowHelper.ThrowInvalidOperationException("Decoded not initialized");

            int i = 0;

            if (Resolution == 0) FindResolution();

            while (i < 3 * Resolution)
            {
                if (i < length)
                {
                    FastMem.LowByte(addr, out zbyte c);
                    addr++;

                    Decoded[i++] = Text.TranslateFromZscii(c);
                }
                else
                {
                    Decoded[i++] = 0;
                }
            }
        }/* load_string */

        /*
         * encode_text
         *
         * Encode the Unicode text in the global "decoded" string then write
         * the result to the global "encoded" array. (This is used to look up
         * words in the dictionary.) Up to V3 the vocabulary resolution is
         * two, and from V4 it is three Z-characters.
         * Because each word contains three Z-characters, that makes six or
         * nine Z-characters respectively. Longer words are chopped to the
         * proper size, shorter words are are padded out with 5's. For word
         * completion we pad with 0s and 31s, the minimum and maximum
         * Z-characters.
         *
         */

        private static readonly zword[] again = { 'a', 'g', 'a', 'i', 'n', 0, 0, 0, 0 };
        private static readonly zword[] examine = { 'e', 'x', 'a', 'm', 'i', 'n', 'e', 0, 0 };
        private static readonly zword[] wait = { 'w', 'a', 'i', 't', 0, 0, 0, 0, 0 };

        internal static void EncodeText(int padding)
        {
            // zbyte *zchars;
            // const zword *ptr;
            zword c;
            int i = 0;
            int ptr = 0;

            if (Resolution == 0) FindResolution();

            if (Decoded is null || Encoded is null)
                ThrowHelper.ThrowInvalidOperationException("Decoded or Endoded not initialized");

            Span<zbyte> zchars = stackalloc zbyte[3 * (Resolution + 1)];
            //                ptr = decoded;

            /* Expand abbreviations that some old Infocom games lack */

            if (Main.option_expand_abbreviations && Main.h_version <= ZMachine.V8)
            {
                if (padding == 0x05 && Decoded[1] == 0)
                {
                    switch (Decoded[0])
                    {
                        case 'g': Decoded = again; break;
                        case 'x': Decoded = examine; break;
                        case 'z': Decoded = wait; break;
                    }
                }
            }

            /* Translate string to a sequence of Z-characters */

            while (i < 3 * Resolution)
            {
                if ((ptr < Decoded.Length) && (c = Decoded[ptr++]) != 0)
                {
                    int index, set;
                    zbyte c2;

                    if (c == 32)
                    {
                        zchars[i++] = 0;
                        continue;
                    }

                    /* Search character in the alphabet */

                    for (set = 0; set < 3; set++)
                    {
                        for (index = 0; index < 26; index++)
                        {
                            if (c == Alphabet(set, index))
                                goto letter_found;
                        }
                    }

                    /* Character not found, store its ZSCII value */

                    c2 = TranslateToZscii(c);

                    zchars[i++] = 5;
                    zchars[i++] = 6;
                    zchars[i++] = (zbyte)(c2 >> 5);
                    zchars[i++] = (zbyte)(c2 & 0x1f);

                    continue;

                letter_found:

                    /* Character found, store its index */

                    if (set != 0)
                        zchars[i++] = (zbyte)(((Main.h_version <= ZMachine.V2) ? 1 : 3) + set);

                    zchars[i++] = (zbyte)(index + 6);

                }
                else
                {
                    zchars[i++] = (zbyte)padding;
                }
            }

            /* Three Z-characters make a 16bit word */

            for (i = 0; i < Resolution; i++)
            {
                Encoded[i] = (zword)(
                    (zchars[3 * i + 0] << 10) |
                    (zchars[3 * i + 1] << 5) |
                    (zchars[3 * i + 2]));
            }

            Encoded[Resolution - 1] |= 0x8000;

        }/* encode_text */

        /*
         * z_check_unicode, test if a unicode character can be printed (bit 0) and read (bit 1).
         *
         * 	zargs[0] = Unicode
         *
         */

        internal static void ZCheckUnicode()
        {
            zword c = Process.zargs[0];
            zword result = 0;

            if (c <= 0x1f)
            {
                if (c is 0x08 or 0x0d or 0x1b)
                    result = 2;
            }
            else
            {
                result = c <= 0x7e ? (zword)3 : OS.CheckUnicode(Screen.GetWindowFont(Main.cwin), c);
            }

            Process.Store(result);

        }/* z_check_unicode */

        /*
         * z_encode_text, encode a ZSCII string for use in a dictionary.
         *
         *	zargs[0] = address of text buffer
         *	zargs[1] = length of ASCII string
         *	zargs[2] = offset of ASCII string within the text buffer
         *	zargs[3] = address to store encoded text in
         *
         * This is a V5+ opcode and therefore the dictionary resolution must be
         * three 16bit words.
         *
         */

        internal static void ZEncodeText()
        {
            LoadString((zword)(Process.zargs[0] + Process.zargs[2]), Process.zargs[1]);
            EncodeText(0x05);

            if (Encoded is null)
                ThrowHelper.ThrowInvalidOperationException("Encoding not initialized.");

            for (int i = 0; i < Resolution; i++)
                FastMem.StoreW((zword)(Process.zargs[3] + 2 * i), Encoded[i]);

        }/* z_encode_text */

        /*
         * decode_text
         *
         * Convert encoded text to Unicode. The encoded text consists of 16bit
         * words. Every word holds 3 Z-characters (5 bits each) plus a spare
         * bit to mark the last word. The Z-characters translate to ZSCII by
         * looking at the current current character set. Some select another
         * character set, others refer to abbreviations.
         *
         * There are several different string types:
         *
         *    LOW_STRING - from the lower 64KB (byte address)
         *    ABBREVIATION - from the abbreviations table (word address)
         *    HIGH_STRING - from the end of the memory map (packed address)
         *    EMBEDDED_STRING - from the instruction stream (at PC)
         *    VOCABULARY - from the dictionary (byte address)
         *
         * The last type is only used for word completion.
         *
         */
        private static int ptrDt = 0;

        private static void DecodeText(StringType st, zword addr)
        {
            // zword* ptr;
            int byte_addr;
            zword c2;
            zword code;
            zbyte c, prev_c = 0;
            int shift_state = 0;
            int shift_lock = 0;
            int status = 0;

            // ptr = NULL;		/* makes compilers shut up */
            byte_addr = 0;

            if (Resolution == 0) FindResolution();

            /* Calculate the byte address if necessary */

            if (st == StringType.ABBREVIATION)
            {
                byte_addr = addr << 1;
            }
            else if (st == StringType.HIGH_STRING)
            {
                byte_addr = Main.h_version switch
                {
                    <= ZMachine.V3 => addr << 1,
                    <= ZMachine.V5 => addr << 2,
                    <= ZMachine.V7 => (addr << 2) + (Main.h_strings_offset << 3),
                    _  => addr << 3
                };

                if (byte_addr >= Main.StorySize)
                    Err.RuntimeError(ErrorCodes.ERR_ILL_PRINT_ADDR);
            }

            /* Loop until a 16bit word has the highest bit set */
            if (st == StringType.VOCABULARY) ptrDt = 0;

            do
            {
                int i;
                /* Fetch the next 16bit word */

                if (st is StringType.LOW_STRING or StringType.VOCABULARY)
                {
                    FastMem.LowWord(addr, out code);
                    addr += 2;
                }
                else if (st is StringType.HIGH_STRING or StringType.ABBREVIATION)
                {
                    FastMem.HighWord(byte_addr, out code);
                    byte_addr += 2;
                }
                else
                {
                    FastMem.CodeWord(out code);
                }

                /* Read its three Z-characters */
                for (i = 10; i >= 0; i -= 5)
                {
                    zword abbr_addr;
                    zword ptr_addr;
                    zword zc;

                    c = (zbyte)((code >> i) & 0x1f);

                    switch (status)
                    {
                        case 0:	/* normal operation */

                            if (shift_state == 2 && c == 6)
                            {
                                status = 2;
                            }
                            else if (Main.h_version == ZMachine.V1 && c == 1)
                            {
                                Buffer.NewLine();
                            }
                            else if (Main.h_version >= ZMachine.V2 && shift_state == 2 && c == 7)
                            {
                                Buffer.NewLine();
                            }
                            else if (c >= 6)
                            {
                                OutChar(st, Alphabet(shift_state, c - 6));
                            }
                            else if (c == 0)
                            {
                                OutChar(st, ' ');
                            }
                            else if (Main.h_version >= ZMachine.V2 && c == 1)
                            {
                                status = 1;
                            }
                            else if (Main.h_version >= ZMachine.V3 && c <= 3)
                            {
                                status = 1;
                            }
                            else
                            {
                                shift_state = (shift_lock + (c & 1) + 1) % 3;

                                if (Main.h_version <= ZMachine.V2 && c >= 4)
                                    shift_lock = shift_state;

                                break;
                            }

                            shift_state = shift_lock;

                            break;

                        case 1:	/* abbreviation */

                            ptr_addr = (zword)(Main.h_abbreviations + 64 * (prev_c - 1) + 2 * c);

                            FastMem.LowWord(ptr_addr, out abbr_addr);
                            DecodeText(StringType.ABBREVIATION, abbr_addr);

                            status = 0;
                            break;

                        case 2:	/* ZSCII character - first part */

                            status = 3;
                            break;

                        case 3:	/* ZSCII character - second part */

                            zc = (zword)((prev_c << 5) | c);

                            c2 = TranslateFromZscii((zbyte)zc); // TODO This doesn't seem right
                            OutChar(st, c2);

                            status = 0;
                            break;
                    }

                    prev_c = c;

                }

            } while (!((code & 0x8000) > 0));

            if (st == StringType.VOCABULARY) ptrDt = 0;
        }/* decode_text */

        //#undef outchar

        /*
         * z_new_line, print a new line.
         *
         * 	no zargs used
         *
         */

        internal static void ZNewLine() => Buffer.NewLine(); /* z_new_line */

        /*
         * z_print, print a string embedded in the instruction stream.
         *
         *	no zargs used
         *
         */

        internal static void ZPrint() => DecodeText(StringType.EMBEDDED_STRING, 0); /* z_print */

        /*
         * z_print_addr, print a string from the lower 64KB.
         *
         *	zargs[0] = address of string to print
         *
         */

        internal static void ZPrintAddr() => DecodeText(StringType.LOW_STRING, Process.zargs[0]); /* z_print_addr */

        /*
         * z_print_char print a single ZSCII character.
         *
         *	zargs[0] = ZSCII character to be printed
         *
         */

        internal static void ZPrintChar() => Buffer.PrintChar(TranslateFromZscii((zbyte)Process.zargs[0])); /* z_print_char */

        /*
         * z_print_form, print a formatted table.
         *
         *	zargs[0] = address of formatted table to be printed
         *
         */

        internal static void ZPrintForm()
        {
            zword addr = Process.zargs[0];

            bool first = true;

            for (; ;)
            {
                FastMem.LowWord(addr, out zword count);
                addr += 2;

                if (count == 0)
                    break;

                if (!first)
                    Buffer.NewLine();

                while (count-- > 0)
                {
                    FastMem.LowByte(addr, out zbyte c);
                    addr++;

                    Buffer.PrintChar(TranslateFromZscii(c));
                }

                first = false;

            }

        }/* z_print_form */

        /*
         * print_num
         *
         * Print a signed 16bit number.
         *
         */
        internal static void PrintNum(zword value)
        {
            int i;

            /* Print sign */

            if ((short)value < 0)
            {
                Buffer.PrintChar('-');
                value = (zword)(-(short)value);
            }

            /* Print absolute value */

            for (i = 10000; i != 0; i /= 10)
            {
                if (value >= i || i == 1)
                    Buffer.PrintChar((zword)('0' + (value / i) % 10));
            }
        }/* print_num */

        /*
         * z_print_num, print a signed number.
         *
         * 	zargs[0] = number to print
         *
         */

        internal static void ZPrintNum() => PrintNum(Process.zargs[0]);/* z_print_num */

        /*
         * print_object
         *
         * Print an object description.
         *
         */

        internal static void PrintObject(zword object_var)
        {
            zword addr = CObject.ObjectName(object_var);
            zword code = 0x94a5;

            FastMem.LowByte(addr, out zbyte length);
            addr++;

            if (length != 0)
                FastMem.LowWord(addr, out code);

            if (code == 0x94a5)
            { 	/* encoded text 0x94a5 == empty string */
                PrintString("object#");	/* supply a generic name */
                PrintNum(object_var);		/* for anonymous objects */
            }
            else
            {
                DecodeText(StringType.LOW_STRING, addr);
            }
        }/* print_object */

        /*
         * z_print_obj, print an object description.
         *
         * 	zargs[0] = number of object to be printed
         *
         */

        internal static void ZPrintObj() => PrintObject(Process.zargs[0]);/* z_print_obj */

        /*
         * z_print_paddr, print the string at the given packed address.
         *
         * 	zargs[0] = packed address of string to be printed
         *
         */

        internal static void ZPrintPaddr() => DecodeText(StringType.HIGH_STRING, Process.zargs[0]);/* z_print_paddr */

        /*
         * z_print_ret, print the string at PC, print newline then return true.
         *
         * 	no zargs used
         *
         */

        internal static void ZPrintRet()
        {

            DecodeText(StringType.EMBEDDED_STRING, 0);
            Buffer.NewLine();
            Process.Ret(1);

        }/* z_print_ret */

        /*
         * print_string
         *
         * Print a string of ASCII characters.
         *
         */
        internal static void PrintString(ReadOnlySpan<char> s)
        {
            foreach (char c in s)
            {
                if (c == '\n')
                    Buffer.NewLine();
                else
                    Buffer.PrintChar(c);
            }
        }/* print_string */

        /*
         * z_print_unicode
         *
         * 	zargs[0] = Unicode
         *
         */

        internal static void ZPrintUnicode()
        {
            zword c = Process.zargs[0];
            Buffer.PrintChar(c < 0x20 ? '?' : c);
        }/* z_print_unicode */

        /*
         * lookup_text
         *
         * Scan a dictionary searching for the given word. The first argument
         * can be
         *
         * 0x00 - find the first word which is >= the given one
         * 0x05 - find the word which exactly matches the given one
         * 0x1f - find the last word which is <= the given one
         *
         * The return value is 0 if the search fails.
         *
         */
        internal static zword LookupText(int padding, zword dct)
        {
            zword entry_addr;
            zword entry;
            zword addr;
            int entry_number;
            int lower, upper;
            int i;
            bool sorted;

            if (Resolution == 0) FindResolution();

            Text.EncodeText(padding);

            if (Encoded is null)
                ThrowHelper.ThrowInvalidOperationException("Encoding not initialized.");

            FastMem.LowByte(dct, out zbyte sep_count);		/* skip word separators */
            dct += (zword)(1 + sep_count);
            FastMem.LowByte(dct, out zbyte entry_len);		/* get length of entries */
            dct += 1;
            FastMem.LowWord(dct, out zword entry_count);		/* get number of entries */
            dct += 2;

            if ((short)entry_count < 0)
            {	/* bad luck, entries aren't sorted */
                entry_count = (zword)(-(short)entry_count);
                sorted = false;
            }
            else
            {
                sorted = true;      /* entries are sorted */
            }

            lower = 0;
            upper = entry_count - 1;

            while (lower <= upper)
            {
                entry_number = sorted
                    ? (lower + upper) / 2 /* binary search */
                    : lower;              /* linear search */

                entry_addr = (zword)(dct + entry_number * entry_len);

                /* Compare word to dictionary entry */

                addr = entry_addr;

                for (i = 0; i < Resolution; i++)
                {
                    FastMem.LowWord(addr, out entry);
                    if (Encoded[i] != entry)
                        goto continuing;
                    addr += 2;
                }

                return entry_addr;		/* exact match found, return now */

            continuing:

                if (sorted)             /* binary search */
                {
                    if (Encoded[i] > entry)
                        lower = entry_number + 1;
                    else
                        upper = entry_number - 1;
                }
                else
                {
                    lower++;                           /* linear search */
                }
            }

            /* No exact match has been found */

            if (padding == 0x05)
                return 0;

            entry_number = (padding == 0x00) ? lower : upper;

            if (entry_number == -1 || entry_number == entry_count)
                return 0;

            return (zword)(dct + entry_number * entry_len);

        }/* lookup_text */

        /*
         * tokenise_text
         *
         * Translate a single word to a token and append it to the token
         * buffer. Every token consists of the address of the dictionary
         * entry, the length of the word and the offset of the word from
         * the start of the text buffer. Unknown words cause empty slots
         * if the flag is set (such that the text can be scanned several
         * times with different dictionaries); otherwise they are zero.
         *
         */
        private static void TokeniseText(zword text, zword length, zword from, zword parse, zword dct, bool flag)
        {
            zword addr;

            FastMem.LowByte(parse, out zbyte token_max);
            parse++;
            FastMem.LowByte(parse, out zbyte token_count);

            if (token_count < token_max)
            {	/* sufficient space left for token? */

                FastMem.StoreB(parse++, (zbyte)(token_count + 1));

                LoadString((zword)(text + from), length);

                addr = LookupText(0x05, dct);

                if (addr != 0 || !flag)
                {
                    parse += (zword)(4 * token_count); // Will parse get updated properly?

                    FastMem.StoreW((zword)(parse + 0), addr);
                    FastMem.StoreB((zword)(parse + 2), (zbyte)length);
                    FastMem.StoreB((zword)(parse + 3), (zbyte)from);
                }

            }

        }/* tokenise_text */

        /*
         * tokenise_line
         *
         * Split an input line into words and translate the words to tokens.
         *
         */

        internal static void TokeniseLine(zword text, zword token, zword dct, bool flag)
        {
            zword addr1;
            zword addr2;
            zbyte length;
            zbyte c;

            length = 0;		/* makes compilers shut up */

            /* Use standard dictionary if the given dictionary is zero */

            if (dct == 0)
                dct = Main.h_dictionary;

            /* Remove all tokens before inserting new ones */

            FastMem.StoreB((zword)(token + 1), 0);

            /* Move the first pointer across the text buffer searching for the
               beginning of a word. If this succeeds, store the position in a
               second pointer. Move the first pointer searching for the end of
               the word. When it is found, "tokenise" the word. Continue until
               the end of the buffer is reached. */

            addr1 = text;
            addr2 = 0;

            if (Main.h_version >= ZMachine.V5)
            {
                addr1++;
                FastMem.LowByte(addr1, out length);
            }

            do
            {
                zword sep_addr;
                zbyte separator;

                /* Fetch next ZSCII character */

                addr1++;

                if (Main.h_version >= ZMachine.V5 && addr1 == text + 2 + length)
                    c = 0;
                else
                    FastMem.LowByte(addr1, out c);

                /* Check for separator */

                sep_addr = dct;

                FastMem.LowByte(sep_addr, out zbyte sep_count);
                sep_addr++;

                do
                {
                    FastMem.LowByte(sep_addr, out separator);
                    sep_addr++;
                } while (c != separator && --sep_count != 0);

                /* This could be the start or the end of a word */

                if (sep_count == 0 && c != ' ' && c != 0)
                {
                    if (addr2 == 0)
                        addr2 = addr1;
                }
                else if (addr2 != 0)
                {
                    TokeniseText(text,
                        (zword)(addr1 - addr2),
                        (zword)(addr2 - text),
                        token, dct, flag);

                    addr2 = 0;
                }

                /* Translate separator (which is a word in its own right) */

                if (sep_count != 0)
                {
                    TokeniseText(text, 1,
                        (zword)(addr1 - text),
                        token, dct, flag);
                }
            } while (c != 0);

        }/* tokenise_line */

        /*
         * z_tokenise, make a lexical analysis of a ZSCII string.
         *
         *	zargs[0] = address of string to analyze
         *	zargs[1] = address of token buffer
         *	zargs[2] = address of dictionary (optional)
         *	zargs[3] = set when unknown words cause empty slots (optional)
         *
         */
        internal static void ZTokenise()
        {
            /* Supply default arguments */

            if (Process.zargc < 3)
                Process.zargs[2] = 0;
            if (Process.zargc < 4)
                Process.zargs[3] = 0;

            /* Call tokenise_line to do the real work */
            TokeniseLine(Process.zargs[0], Process.zargs[1], Process.zargs[2], Process.zargs[3] != 0);
        }/* z_tokenise */

        /*
         * completion
         *
         * Scan the vocabulary to complete the last word on the input line
         * (similar to "tcsh" under Unix). The return value is
         *
         *    2 ==> completion is impossible
         *    1 ==> completion is ambiguous
         *    0 ==> completion is successful
         *
         * The function also returns a string in its second argument. In case
         * of 2, the string is empty; in case of 1, the string is the longest
         * extension of the last word on the input line that is common to all
         * possible completions (for instance, if the last word on the input
         * is "fo" and its only possible completions are "follow" and "folly"
         * then the string is "ll"); in case of 0, the string is an extension
         * to the last word that results in the only possible completion.
         *
         */
        public static int Completion(ReadOnlySpan<char> buffer, out string result)
        {
            zword minaddr;
            zword maxaddr;
            //zword *ptr;
            char c;
            int len;
            int i;

            if (Decoded is null)
                ThrowHelper.ThrowInvalidOperationException("Decoded not initialized.");

            for (int j = 0; j < Decoded.Length; j++)
            {
                Decoded[j] = 0;
            }

            result = string.Empty;

            if (Resolution == 0) FindResolution();

            /* Copy last word to "decoded" string */

            len = 0;
            int pos = 0;

            while (pos < buffer.Length && (c = buffer[pos++]) != 0)
            {
                if (c != ' ')
                {
                    if (len < 3 * Resolution)
                        Decoded[len++] = c;
                }
                else
                {
                    len = 0;
                }
            }
            Decoded[len] = 0;

            /* Search the dictionary for first and last possible extensions */

            minaddr = LookupText(0x00, Main.h_dictionary);
            maxaddr = LookupText(0x1f, Main.h_dictionary);

            if (minaddr == 0 || maxaddr == 0 || minaddr > maxaddr)
                return 2;

            /* Copy first extension to "result" string */

            DecodeText(StringType.VOCABULARY, minaddr);

            // ptr = result;
            var temp = new StringBuilder(len);

            for (i = len; (c = (char)Decoded[i]) != 0; i++)
                temp.Append(c);

            /* Merge second extension with "result" string */

            DecodeText(StringType.VOCABULARY, maxaddr);

            int ptr = 0;

            for (i = len; (c = (char)Decoded[i]) != 0; i++, ptr++)
            {
                if (ptr < temp.Length - 1 && temp[ptr] != c)
                    break;
            }
            temp.Length = ptr;

            /* Search was ambiguous or successful */

            result = temp.ToString();

            return (minaddr == maxaddr) ? 0 : 1;

        }/* completion */

        /*
         * unicode_tolower
         *
         * Convert a Unicode character to lowercase.
         * Taken from Zip2000 by Kevin Bracey.
         *
         */

        // TODO There were all unsigned char arrays; and they were all consts
        private static readonly zword[] tolower_basic_latin = { // 0x100
            0x00,0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,0x09,0x0A,0x0B,0x0C,0x0D,0x0E,0x0F,
            0x10,0x11,0x12,0x13,0x14,0x15,0x16,0x17,0x18,0x19,0x1A,0x1B,0x1C,0x1D,0x1E,0x1F,
            0x20,0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x29,0x2A,0x2B,0x2C,0x2D,0x2E,0x2F,
            0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,
            0x40,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6A,0x6B,0x6C,0x6D,0x6E,0x6F,
            0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x5B,0x5C,0x5D,0x5E,0x5F,
            0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6A,0x6B,0x6C,0x6D,0x6E,0x6F,
            0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7A,0x7B,0x7C,0x7D,0x7E,0x7F,
            0x80,0x81,0x82,0x83,0x84,0x85,0x86,0x87,0x88,0x89,0x8A,0x8B,0x8C,0x8D,0x8E,0x8F,
            0x90,0x91,0x92,0x93,0x94,0x95,0x96,0x97,0x98,0x99,0x9A,0x9B,0x9C,0x9D,0x9E,0x9F,
            0xA0,0xA1,0xA2,0xA3,0xA4,0xA5,0xA6,0xA7,0xA8,0xA9,0xAA,0xAB,0xAC,0xAD,0xAE,0xAF,
            0xB0,0xB1,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,
            0xE0,0xE1,0xE2,0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xEB,0xEC,0xED,0xEE,0xEF,
            0xF0,0xF1,0xF2,0xF3,0xF4,0xF5,0xF6,0xD7,0xF8,0xF9,0xFA,0xFB,0xFC,0xFD,0xFE,0xDF,
            0xE0,0xE1,0xE2,0xE3,0xE4,0xE5,0xE6,0xE7,0xE8,0xE9,0xEA,0xEB,0xEC,0xED,0xEE,0xEF,
            0xF0,0xF1,0xF2,0xF3,0xF4,0xF5,0xF6,0xF7,0xF8,0xF9,0xFA,0xFB,0xFC,0xFD,0xFE,0xFF
        };
        private static readonly zword[] tolower_latin_extended_a = { // 0x80
            0x01,0x01,0x03,0x03,0x05,0x05,0x07,0x07,0x09,0x09,0x0B,0x0B,0x0D,0x0D,0x0F,0x0F,
            0x11,0x11,0x13,0x13,0x15,0x15,0x17,0x17,0x19,0x19,0x1B,0x1B,0x1D,0x1D,0x1F,0x1F,
            0x21,0x21,0x23,0x23,0x25,0x25,0x27,0x27,0x29,0x29,0x2B,0x2B,0x2D,0x2D,0x2F,0x2F,
            0x00,0x31,0x33,0x33,0x35,0x35,0x37,0x37,0x38,0x3A,0x3A,0x3C,0x3C,0x3E,0x3E,0x40,
            0x40,0x42,0x42,0x44,0x44,0x46,0x46,0x48,0x48,0x49,0x4B,0x4B,0x4D,0x4D,0x4F,0x4F,
            0x51,0x51,0x53,0x53,0x55,0x55,0x57,0x57,0x59,0x59,0x5B,0x5B,0x5D,0x5D,0x5F,0x5F,
            0x61,0x61,0x63,0x63,0x65,0x65,0x67,0x67,0x69,0x69,0x6B,0x6B,0x6D,0x6D,0x6F,0x6F,
            0x71,0x71,0x73,0x73,0x75,0x75,0x77,0x77,0x00,0x7A,0x7A,0x7C,0x7C,0x7E,0x7E,0x7F
        };
        private static readonly zword[] tolower_greek = { //0x50
            0x80,0x81,0x82,0x83,0x84,0x85,0xAC,0x87,0xAD,0xAE,0xAF,0x8B,0xCC,0x8D,0xCD,0xCE,
            0x90,0xB1,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,
            0xC0,0xC1,0xA2,0xC3,0xC4,0xC5,0xC6,0xC7,0xC8,0xC9,0xCA,0xCB,0xAC,0xAD,0xAE,0xAF,
            0xB0,0xB1,0xB2,0xB3,0xB4,0xB5,0xB6,0xB7,0xB8,0xB9,0xBA,0xBB,0xBC,0xBD,0xBE,0xBF,
            0xC0,0xC1,0xC2,0xC3,0xC4,0xC5,0xC6,0xC7,0xC8,0xC9,0xCA,0xCB,0xCC,0xCD,0xCE,0xCF
        };
        private static readonly zword[] tolower_cyrillic = { // 0x60
            0x00,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x5B,0x5C,0x5D,0x5E,0x5F,
            0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,
            0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,0x4F,
            0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3A,0x3B,0x3C,0x3D,0x3E,0x3F,
            0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,0x4F,
            0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5A,0x5B,0x5C,0x5D,0x5E,0x5F
        };

        internal static zword UnicodeToLower(zword c)
        {
            if (c < 0x0100)
                c = tolower_basic_latin[c];
            else if (c == 0x0130)
                c = 0x0069;	/* Capital I with dot -> lower case i */
            else if (c == 0x0178)
                c = 0x00FF;	/* Capital Y diaeresis -> lower case y diaeresis */
            else if (c < 0x0180)
                c = (zword)(tolower_latin_extended_a[c - 0x100] + 0x100);
            else if (c is >= 0x380 and < 0x3D0)
                c = (zword)(tolower_greek[c - 0x380] + 0x300);
            else if (c is >= 0x400 and < 0x460)
                c = (zword)(tolower_cyrillic[c - 0x400] + 0x400);

            return c;
        }

        private static void OutChar(StringType st, zword c)
        {
            if (st == StringType.VOCABULARY)
            {
                Decoded![ptrDt++] = c;
            }
            else
            {
                Buffer.PrintChar(c);
            }
        }
    }
}
