// #define FULL_HEADER

/*
 * showhead - part of infodump
 *
 * Header display routines.
 */

namespace ZTools;

using zword_t = System.UInt16;

internal class ShowHead
{
    private static readonly string[] interpreter_flags1 = {
        "Byte swapped data",
        "Display time",
        "Unknown (0x04)",
        "Tandy",
        "No status line",
        "Windows available",
        "Proportional fonts used",
        "Unknown (0x80)"
    };
    private static readonly string[] interpreter_flags2 = {
        "Colors",
        "Pictures",
        "Bold font",
        "Emphasis",
        "Fixed space font",
        "Unknown (0x20)",
        "Unknown (0x40)",
        "Timed input"
    };
    private static readonly string[] game_flags1 = {
        "Scripting",
        "Use fixed font",
        "Unknown (0x0004)",
        "Unknown (0x0008)",
        "Supports sound",
        "Unknown (0x0010)",
        "Unknown (0x0020)",
        "Unknown (0x0040)",
        "Unknown (0x0080)",
        "Unknown (0x0200)",
        "Unknown (0x0400)",
        "Unknown (0x0800)",
        "Unknown (0x1000)",
        "Unknown (0x2000)",
        "Unknown (0x4000)",
        "Unknown (0x8000)"
    };
    private static readonly string[] game_flags2 = {
        "Scripting",
        "Use fixed font",
        "Screen refresh required",
        "Supports graphics",
        "Supports undo",
        "Supports mouse",
        "Supports colour",
        "Supports sound",
        "Supports menus",
        "Unknown (0x0200)",
        "Printer error",
        "Unknown (0x0800)",
        "Unknown (0x1000)",
        "Unknown (0x2000)",
        "Unknown (0x4000)",
        "Unknown (0x8000)"
    };

    /*
     * show_header
     *
     * Format the header which is a 64 byte area at the front of the story file.
     * The format of the header is described by the header structure.
     */

    internal static void ShowHeader()
    {
        ulong address;
        int i, j, list;
        int inform = 0; // TODO Was short

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        var header = txio.header;

        if (header.serial[0] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[1] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[2] is >= (byte)'0' and <= (byte)'1' &&
            header.serial[3] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[4] is >= (byte)'0' and <= (byte)'3' &&
            header.serial[5] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[0] != '8')
        {
            inform = 5;

            if (header.name[4] >= '6')
                inform = header.name[4] - '0';
        }

        txio.TxPrint("\n    **** Story file header ****\n\n");

        /* Z-code version */

        txio.TxPrintf("Z-code version:           {0:d}\n", header.version);

        /* Interpreter flags */

        Console.Write("Interpreter flags:        ");
        txio.TxFixMargin(1);
        list = 0;
        for (i = 0; i < 8; i++)
        {
            if (((uint)header.config & (1 << i)) > 0)
            {
                txio.TxPrintf("{0}{1}", (list++) > 0 ? ", " : "",
                       ((uint)header.version < TxH.V4) ? interpreter_flags1[i] : interpreter_flags2[i]);
            }
            else
            {
                if ((uint)header.version < TxH.V4 && i == 1)
                    txio.TxPrintf("{0}Display score/moves", (list++) > 0 ? ", " : "");
            }
        }
        if (list == 0)
            txio.TxPrint("None");

        txio.TxPrint("\n");
        txio.TxFixMargin(0);

        /* Release number */

        txio.TxPrintf("Release number:           {0:d}\n", (int)header.release);

        /* Size of resident memory */

        txio.TxPrintf("Size of resident memory:  {0:X4}\n", (uint)header.resident_size);

        /* Start PC */

        if ((uint)header.version != TxH.V6)
            txio.TxPrintf("Start PC:                 {0:X4}\n", (uint)header.start_pc);
        else
        {
            txio.TxPrintf("Main routine address:     {0:X5}\n", (ulong)
                   ((header.start_pc * txio.code_scaler) +
                    (header.routines_offset * txio.story_scaler)));
        }

        /* Dictionary address */

        txio.TxPrintf("Dictionary address:       {0:X4}\n", (uint)header.dictionary);

        /* Object table address */

        txio.TxPrintf("Object table address:     {0:X4}\n", (uint)header.objects);

        /* Global variables address */

        txio.TxPrintf("Global variables address: {0:X4}\n", (uint)header.globals);

        /* Size of dynamic memory */

        txio.TxPrintf("Size of dynamic memory:   {0:X4}\n", (uint)header.dynamic_size);

        /* Game flags */

        txio.TxPrint("Game flags:               ");
        txio.TxFixMargin(1);
        list = 0;
        for (i = 0; i < 16; i++)
        {
            if (((uint)header.flags & (1 << i)) > 0)
            {
                txio.TxPrintf("{0}{1}", (list++) > 0 ? ", " : "",
                       ((uint)header.version < TxH.V4) ? game_flags1[i] : game_flags2[i]);
            }
        }
        if (list == 0)
            txio.TxPrint("None");
        txio.TxPrint("\n");
        txio.TxFixMargin(0);

        /* Serial number */

        txio.TxPrintf("Serial number:            {0}{1}{2}{3}{4}{5}\n",
            (char)header.serial[0], (char)header.serial[1],
            (char)header.serial[2], (char)header.serial[3],
            (char)header.serial[4], (char)header.serial[5]);

        /* Abbreviations address */

        if ((uint)header.abbreviations > 0)
            txio.TxPrintf("Abbreviations address:    {0:X4}\n", (uint)header.abbreviations);

        /* File size and checksum */

        if ((uint)header.file_size > 0)
        {
            txio.TxPrintf("File size:                {0:X5}\n", (ulong)txio.file_size);
            txio.TxPrintf("Checksum:                 {0:X4}\n", (uint)header.checksum);
        }

#if FULL_HEADER

            /* Interpreter */

            txio.tx_printf ("Interpreter number:       {0} ", header.interpreter_number);
            switch ((uint) header.interpreter_number) {
            case 1 : txio.tx_printf ("DEC-20"); break;
            case 2 : txio.tx_printf ("Apple //e"); break;
            case 3 : txio.tx_printf ("Macintosh"); break;
            case 4 : txio.tx_printf ("Amiga"); break;
            case 5 : txio.tx_printf ("Atari ST"); break;
            case 6 : txio.tx_printf ("IBM/MS-DOS"); break;
            case 7 : txio.tx_printf ("Commodore 128"); break;
            case 8 : txio.tx_printf ("C64"); break;
            case 9 : txio.tx_printf ("Apple //c"); break;
            case 10: txio.tx_printf ("Apple //gs"); break;
            case 11: txio.tx_printf ("Tandy Color Computer"); break;
            default: txio.tx_printf("Unknown"); break;
            }
            txio.tx_printf ("\n");

            /* Interpreter version */

            txio.tx_printf ("Interpreter version:      ");
            //if (isprint ((uint) header.interpreter_version))
            //txio.tx_printf ("{0:c}\n", (char) header.interpreter_version);
            //else
            txio.tx_printf ("{0}\n", (int) header.interpreter_version);

            /* Screen dimensions */

            txio.tx_printf ("Screen rows:              {0}\n", (int) header.screen_rows);
            txio.tx_printf ("Screen columns:           {0}\n", (int) header.screen_columns);
            txio.tx_printf ("Screen width:             {0}\n", (int) header.screen_width);
            txio.tx_printf ("Screen height:            {0}\n", (int) header.screen_height);

            /* Font size */

            txio.tx_printf ("Font width:               {0}\n", (int) header.font_width);
            txio.tx_printf ("Font height:              {0}\n", (int) header.font_height);

#endif // defined(FULL_HEADER)

        /* V6 and V7 offsets */

        if ((uint)header.routines_offset > 0)
            txio.TxPrintf("Routines offset:          {0:X5}\n", header.routines_offset * txio.story_scaler);
        if ((uint)header.strings_offset > 0)
            txio.TxPrintf("Strings offset:           {0:X5}\n", header.strings_offset * txio.story_scaler);

#if FULL_HEADER

            /* Default colours */

            txio.tx_printf ("Background color:         {0}\n", (int) header.default_background);
            txio.tx_printf ("Foreground color:         {0}\n", (int) header.default_foreground);
        
#endif // defined(FULL_HEADER)

        /* Function keys address */

        if ((uint)header.terminating_keys > 0)
        {
            txio.TxPrintf("Terminating keys address: {0:X4}\n", (uint)header.terminating_keys);
            address = header.terminating_keys;
            txio.TxPrint("    Keys used: ");
            txio.TxFixMargin(1);
            list = 0;
            for (i = txio.ReadDataByte(ref address); i > 0;
                 i = txio.ReadDataByte(ref address))
            {
                if (list > 0)
                    txio.TxPrint(", ");
                if (i == 0x81)
                    txio.TxPrint("Up arrow"); /* Arrow keys */
                else if (i == 0x82)
                    txio.TxPrint("Down arrow");
                else if (i == 0x83)
                    txio.TxPrint("Left arrow");
                else if (i == 0x84)
                    txio.TxPrint("Right arrow");
                else if (i is >= 0x85 and <= 0x90)
                    txio.TxPrintf("F{0}", i - 0x84); /* Function keys */
                else if (i is >= 0x91 and <= 0x9a)
                    txio.TxPrintf("KP{0}", i - 0x91); /* Keypad keys */
                else if (i == 0xfc)
                    txio.TxPrint("Menu click");
                else if (i == 0xfd)
                    txio.TxPrint("Single mouse click");
                else if (i == 0xfe)
                    txio.TxPrint("Double mouse click");
                else if (i == 0xff)
                    txio.TxPrint("Any function key");
                else
                    txio.TxPrintf("Unknown key (0x{0:X2})", (uint)i);
                list++;
            }
            txio.TxPrint("\n");
            txio.TxFixMargin(0);
        }

#if FULL_HEADER

            /* Line width */

            txio.tx_printf ("Line width:               {0}\n", (int) header.line_width);

            /* Specification number */

            if ((uint) header.specification_hi > 0)
            txio.tx_printf ("Specification number:   {0}.{1}",
                   (uint) header.specification_hi,
                   (uint) header.specification_lo);

#endif // defined(FULL_HEADER)

        /* Alphabet address */

        if ((uint)header.alphabet > 0)
        {
            txio.TxPrintf("Alphabet address:         {0:4X}\n", (uint)header.alphabet);
            txio.TxPrint("    ");
            txio.TxFixMargin(1);
            for (i = 0; i < 3; i++)
            {
                txio.TxPrint("\"");
                for (j = 0; j < 26; j++)
                    txio.TxPrint((char)TxH.GetByte((ulong)((uint)header.alphabet + (i * 26) + j)));
                txio.TxPrint("\"\n");
            }
            txio.TxFixMargin(0);
        }

        /* Mouse table address */

        if ((uint)header.mouse_table > 0)
            txio.TxPrintf("Header extension address: {0:X4}\n", (uint)header.mouse_table);

#if FULL_HEADER

            /* Name */

        if ((uint)header.name[0] > 0 || (uint)header.name[1] > 0 || (uint)header.name[2] > 0 || (uint)header.name[3] > 0 ||
            (uint)header.name[4] > 0 || (uint)header.name[5] > 0 || (uint)header.name[6] > 0 || (uint)header.name[7] > 0)
        {
            txio.tx_printf ("Name:                     \"");
            for (i = 0; i < header.name.Length; i++)
                txio.tx_printf ("{0}", (char) header.name[i]);
            txio.tx_printf ("\"\n");
            }

#endif // defined(FULL_HEADER)

        /* Inform version -- overlaps name */
        if (inform >= 6)
        {
            txio.TxPrint("Inform Version:           ");
            for (i = 4; i < header.name.Length; i++)
                txio.TxPrint((char)header.name[i]);
            txio.TxPrint('\n');
        }

        ShowHeaderExtension();

    }/* show_header */

    private static void ShowHeaderExtension()
    {
        zword_t tlen = 0;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        if ((uint)txio.header.mouse_table > 0)
        {
            tlen = TxH.GetWord(txio.header.mouse_table);
            txio.TxPrintf("Header extension length:  {0:X4}\n", tlen);
        }
        else
        {
            return;
        }

#if FULL_HEADER
        if (tlen > 0)
            txio.tx_printf("Mouse Y coordinate:       {0:X4}\n", tx_h.get_word(txio.header.mouse_table + 2));
        if (tlen > 1)
            txio.tx_printf("Mouse X coordinate:       {0:X4}\n", tx_h.get_word(txio.header.mouse_table + 4));
#endif

        if (tlen > 2)
            txio.TxPrintf("Unicode table address:    {0:X4}\n", (ulong)TxH.GetWord(txio.header.mouse_table + 6));
    }
}
