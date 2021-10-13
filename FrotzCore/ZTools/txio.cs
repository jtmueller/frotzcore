/* txio.c
 *
 * I/O routines for Z code disassembler and story file dumper.
 *
 * Mark Howell 26 August 1992 howell_ma@movies.enet.dec.com
 *
 */

using System.Buffers;
using System.Diagnostics;

namespace ZTools;

public static class txio
{
    internal static TxH.ZHeaderT? header;

    internal static ulong story_scaler;
    internal static ulong story_shift;
    internal static ulong code_scaler;
    internal static ulong code_shift;
    internal static int property_mask;
    internal static int property_size_mask;

    internal static bool option_inform = false;

    internal static int file_size = 0;

    private static readonly string[] v1_lookup_table;
    private static readonly string[] v3_lookup_table;
    private static readonly string[] euro_substitute;
    private static readonly string[] inform_euro_substitute;

    private static int lookup_table_loaded = 0;
    private static readonly char[,] lookup_table;

    static txio()
    {
        lookup_table = new char[3, 26];

        v1_lookup_table = new string[] {
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            " 0123456789.,!?_#'\"/\\<-:()"
        };

        v3_lookup_table = new string[] {
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            " \n0123456789.,!?_#'\"/\\-:()"
        };

        euro_substitute = new string[] {
            "ae", "oe", "ue", "Ae", "Oe", "Ue", "ss", ">>", "<<", "e",
            "i",  "y",  "E",  "I",  "a",  "e",  "i",  "o",  "u",  "y",
            "A",  "E",  "I",  "O",  "U",  "Y",  "a",  "e",  "i",  "o",
            "u",  "A",  "E",  "I",  "O",  "U",  "a",  "e",  "i",  "o",
            "u",  "A",  "E",  "I",  "O",  "U",  "a",  "A",  "o",  "O",
            "a",  "n",  "o",  "A",  "N",  "O",  "ae", "AE", "c",  "C",
            "th", "th", "Th", "Th", "L",  "oe", "OE", "!",  "?"
        };

        inform_euro_substitute = new string[] {
            "ae", "oe", "ue", "AE", "OE", "UE", "ss", ">>", "<<", ":e",
            ":i",  ":y",  ":E",  ":I",  "'a",  "'e",  "'i",  "'o",  "'u",  "'y",
            "'A",  "'E",  "'I",  "'O",  "'U",  "'Y",  "`a",  "`e",  "`i",  "`o",
            "`u",  "`A",  "`E",  "`I",  "`O",  "`U",  "^a",  "^e",  "^i",  "^o",
            "^u",  "^A",  "^E",  "^I",  "^O",  "^U",  "oa",  "oA",  "\\o",  "\\O",
            "~a",  "~n",  "~o",  "~A",  "~N",  "~O",  "ae", "AE", "cc",  "cC",
            "th", "et", "Th", "Et", "LL",  "oe", "OE", "!!",  "??"
        };
    }

    private const int TX_SCREEN_COLS = 79;
    private static char[] tx_line = Array.Empty<char>();
    internal static int tx_line_pos = 0;
    internal static int tx_col = 1;
    internal static int tx_margin = 0;
    internal static int tx_do_margin = 1;
    internal static int tx_screen_cols = TX_SCREEN_COLS;

    //internal class cache_entry_t
    //{
    //    cache_entry_t flink;
    //    // uint page_number;
    //    zbyte_t[] data = new zbyte_t[tx_h.PAGE_SIZE];
    //}

    private static Stream? gfp = null;

    //static cache_entry_t *cache = NULL;

    // static uint current_data_page = 0;
    //static cache_entry_t *current_data_cachep = NULL;

    // static uint data_size;

    private static zbyte[] buffer = Array.Empty<zbyte>();

    internal static void Configure(int min_version, int max_version)
    {
        if (gfp == null)
            throw new InvalidOperationException("gfp not initialized.");

        // buffer = new zbyte_t[tx_h.PAGE_SIZE];
        int i;

        buffer = new zbyte[gfp.Length];
        gfp.Read(buffer, 0, buffer.Length);

        //#if !defined(lint)
        //    assert (sizeof (zheader_t) == 64);
        //    assert (sizeof (zheader_t) <= PAGE_SIZE);
        //#endif /* !defined(lint) */

        // read_page(0, buffer);
        TxH.Datap = buffer;

        header = new TxH.ZHeaderT
        {
            version = TxH.GetByte(TxH.H_VERSION),
            config = TxH.GetByte(TxH.H_CONFIG),
            release = TxH.GetWord(TxH.H_RELEASE),
            resident_size = TxH.GetWord(TxH.H_RESIDENT_SIZE),
            start_pc = TxH.GetWord(TxH.H_START_PC),
            dictionary = TxH.GetWord(TxH.H_DICTIONARY),
            objects = TxH.GetWord(TxH.H_OBJECTS),
            globals = TxH.GetWord(TxH.H_GLOBALS),
            dynamic_size = TxH.GetWord(TxH.H_DYNAMIC_SIZE),
            flags = TxH.GetWord(TxH.H_FLAGS)
        };
        for (i = 0; i < header.serial.Length; i++)
            header.serial[i] = TxH.GetByte(TxH.H_SERIAL + i);
        header.abbreviations = TxH.GetWord(TxH.H_ABBREVIATIONS);
        header.file_size = TxH.GetWord(TxH.H_FILE_SIZE);
        header.checksum = TxH.GetWord(TxH.H_CHECKSUM);
        header.interpreter_number = TxH.GetByte(TxH.H_INTERPRETER_NUMBER);
        header.interpreter_version = TxH.GetByte(TxH.H_INTERPRETER_VERSION);
        header.screen_rows = TxH.GetByte(TxH.H_SCREEN_ROWS);
        header.screen_columns = TxH.GetByte(TxH.H_SCREEN_COLUMNS);
        header.screen_width = TxH.GetWord(TxH.H_SCREEN_WIDTH);
        header.screen_height = TxH.GetWord(TxH.H_SCREEN_HEIGHT);
        if (header.version != TxH.V6)
        {
            header.font_width = (byte)TxH.GetWord(TxH.H_FONT_WIDTH);
            header.font_height = TxH.GetByte(TxH.H_FONT_HEIGHT);
        }
        else
        {
            header.font_width = (byte)TxH.GetWord(TxH.H_FONT_HEIGHT);
            header.font_height = TxH.GetByte(TxH.H_FONT_WIDTH);
        }
        header.routines_offset = TxH.GetWord(TxH.H_ROUTINES_OFFSET);
        header.strings_offset = TxH.GetWord(TxH.H_STRINGS_OFFSET);
        header.default_background = TxH.GetByte(TxH.H_DEFAULT_BACKGROUND);
        header.default_foreground = TxH.GetByte(TxH.H_DEFAULT_FOREGROUND);
        header.terminating_keys = TxH.GetWord(TxH.H_TERMINATING_KEYS);
        header.line_width = TxH.GetWord(TxH.H_LINE_WIDTH);
        header.specification_hi = TxH.GetByte(TxH.H_SPECIFICATION_HI);
        header.specification_lo = TxH.GetByte(TxH.H_SPECIFICATION_LO);
        header.alphabet = TxH.GetWord(TxH.H_ALPHABET);
        header.mouse_table = TxH.GetWord(TxH.H_MOUSE_TABLE);
        for (i = 0; i < header.name.Length; i++)
            header.name[i] = TxH.GetByte(TxH.H_NAME + i);

        if (header.version < (uint)min_version ||
        header.version > (uint)max_version ||
        ((uint)header.config & TxH.CONFIG_BYTE_SWAPPED) != 0)
        {
            throw new ArgumentException("\nFatal: wrong game or version\n");
        }

        if ((uint)header.version < TxH.V4)
        {
            story_scaler = 2;
            story_shift = 1;
            code_scaler = 2;
            code_shift = 1;
            property_mask = TxH.P3_MAX_PROPERTIES - 1;
            property_size_mask = 0xe0;
        }
        else if ((uint)header.version < TxH.V6)
        {
            story_scaler = 4;
            story_shift = 2;
            code_scaler = 4;
            code_shift = 2;
            property_mask = TxH.P4_MAX_PROPERTIES - 1;
            property_size_mask = 0x3f;
        }
        else if ((uint)header.version < TxH.V8)
        {
            story_scaler = 8;
            story_shift = 3;
            code_scaler = 4;
            code_shift = 2;
            property_mask = TxH.P4_MAX_PROPERTIES - 1;
            property_size_mask = 0x3f;
        }
        else
        {
            story_scaler = 8;
            story_shift = 3;
            code_scaler = 8;
            code_shift = 3;
            property_mask = TxH.P4_MAX_PROPERTIES - 1;
            property_size_mask = 0x3f;
        }

        /* Calculate the file size */

        if ((uint)header.file_size == 0)
        {
            throw new ArgumentException("Can't handle files with no length. Giving up!");
            // file_size = get_story_size();
        }

        file_size = (uint)header.version switch
        {
            <= TxH.V3 => header.file_size * 2,
            <= TxH.V5 => header.file_size * 4,
            _ => header.file_size * 8
        };
    }/* configure */

    internal static void OpenStory(byte[] story)
    {
        gfp?.Dispose();
        gfp = new MemoryStream(story);
    }

    internal static void OpenStory(IMemoryOwner<byte> story)
    {
        gfp?.Dispose();
        gfp = story.AsStream();
    }

    internal static void OpenStory(string storyname)
    {
        gfp?.Dispose();
        gfp = new FileStream(storyname, FileMode.Open);
        if (gfp == null)
        {
            throw new InvalidOperationException("Fatal: game file not found\n");
        }

    } /* open_story */

    internal static void CloseStory() => gfp?.Dispose();/* close_story */

    internal static void ReadPage(uint page, Span<byte> buffer)
    {
        if (gfp == null)
            throw new InvalidOperationException("gfp not initialized.");

        int bytes_to_read = file_size == 0 ? 64 : page != (uint)(file_size / TxH.PAGE_SIZE) ? TxH.PAGE_SIZE : file_size & TxH.PAGE_MASK;
        gfp.Position = page * TxH.PAGE_SIZE;
        gfp.Read(buffer[..bytes_to_read]);

    } /* read_page */

    //internal static void load_cache()
    //{
    //    //    ulong file_size;
    //    //    uint i, file_pages, data_pages;
    //    //    cache_entry_t *cachep;

    //    //    /* Must have at least one cache page for memory calculation */

    //    //    cachep = (cache_entry_t *) malloc (sizeof (cache_entry_t));
    //    //    if (cachep == NULL) {
    //    //    (void) fprintf (stderr, "\nFatal: insufficient memory\n");
    //    //    exit (EXIT_FAILURE);
    //    //    }
    //    //    cachep->flink = cache;
    //    //    cachep->page_number = 0;
    //    //    cache = cachep;

    //    //    /* Calculate dynamic cache pages required */

    //    //    data_pages = ((uint) header.resident_size + PAGE_MASK) >> PAGE_SHIFT;
    //    //    data_size = data_pages * PAGE_SIZE;
    //    //    file_size = (ulong) header.file_size * story_scaler;
    //    //    file_pages = (uint) ((file_size + PAGE_MASK) >> PAGE_SHIFT);

    //    //    /* Allocate static data area and initialise it */

    //    //    datap = (zbyte_t *) malloc ((size_t) data_size);
    //    //    if (datap == NULL) {
    //    //    (void) fprintf (stderr, "\nFatal: insufficient memory\n");
    //    //    exit (EXIT_FAILURE);
    //    //    }
    //    //    for (i = 0; i < data_pages; i++)
    //    //    read_page (i, &datap[i * PAGE_SIZE]);

    //    //    /* Allocate cache pages and initialise them */

    //    //    for (i = data_pages; cachep != NULL && i < file_pages && i < data_pages + MAX_CACHE; i++) {
    //    //    cachep = (cache_entry_t *) malloc (sizeof (cache_entry_t));
    //    //    if (cachep != NULL) {
    //    //        cachep->flink = cache;
    //    //        cachep->page_number = i;
    //    //        read_page (cachep->page_number, cachep->data);
    //    //        cache = cachep;
    //    //        }
    //    //    }

    //} /* load_cache */

    internal static zword ReadDataWord(ref ulong addr)
    {
        uint w = (uint)ReadDataByte(ref addr) << 8;
        w |= ReadDataByte(ref addr);

        return (zword)w;

    }/* txio.read_data_word */

    internal static zbyte ReadDataByte(ref ulong addr)
    {
        // uint page_number, page_offset;
        zbyte value;

        //if (addr < (ulong)txio.data_size)
        //    value = buffer[addr];
        //else
        //{
        //    page_number = (uint)(addr >> tx_h.PAGE_SHIFT);
        //    page_offset = (uint)(addr & (ulong)tx_h.PAGE_MASK);
        //    if (page_number != current_data_page)
        //    {
        //        current_data_cachep = update_cache(page_number);
        //        current_data_page = page_number;
        //    }
        //    value = current_data_cachep->data[page_offset];
        //}

        value = buffer[addr];
        addr++;

        return value;
    }/* txio.read_data_byte */

    internal static int DecodeText(ref ulong address)
    {
        int i, j, char_count, synonym_flag, synonym = 0, ascii_flag, ascii = 0;
        int data, code, shift_state, shift_lock;
        ulong addr;

        if (header is null)
            throw new InvalidOperationException("txio header was not initialized");

        /*
         * Load correct character translation table for this game.
         */

        if (lookup_table_loaded == 0)
        {
            for (i = 0; i < 3; i++)
            {
                for (j = 0; j < 26; j++)
                {
                    lookup_table[i, j] = (uint)header.alphabet > 0
                        ? (char)TxH.GetByte(header.alphabet + (i * 26) + j)
                        : (uint)header.version == TxH.V1
                            ? v1_lookup_table[i][j] : v3_lookup_table[i][j];

                    if (option_inform && lookup_table[i, j] == '\"')
                    {
                        lookup_table[i, j] = '~';
                    }
                }
                lookup_table_loaded = 1;
            }
        }

        /* Set state variables */

        shift_state = 0;
        shift_lock = 0;
        char_count = 0;
        ascii_flag = 0;
        synonym_flag = 0;

        do
        {

            /*
             * Read one 16 bit word. Each word contains three 5 bit codes. If the
             * high bit is set then this is the last word in the string.
             */
            data = txio.ReadDataWord(ref address);

            for (i = 10; i >= 0; i -= 5)
            {
                /* Get code, high bits first */

                code = (data >> i) & 0x1f;

                /* Synonym codes */

                if (synonym_flag > 0)
                {

                    synonym_flag = 0;
                    synonym = (synonym - 1) * 64;
                    addr = (ulong)TxH.GetWord(header.abbreviations + synonym + (code * 2)) * 2;
                    char_count += txio.DecodeText(ref addr);
                    shift_state = shift_lock;

                    /* ASCII codes */

                }
                else if (ascii_flag > 0)
                {

                    /*
                     * If this is the first part ASCII code then remember it.
                     * Because the codes are only 5 bits you need two codes to make
                     * one eight bit ASCII character. The first code contains the
                     * top 3 bits. The second code contains the bottom 5 bits.
                     */

                    if (ascii_flag++ == 1)
                    {
                        ascii = code << 5;
                    }

                    /*
                     * If this is the second part ASCII code then assemble the
                     * character from the two codes and output it.
                     */

                    else
                    {

                        ascii_flag = 0;
                        txio.TxPrint((char)(ascii | code));
                        char_count++;

                    }

                    /* Character codes */

                }
                else if (code > 5)
                {

                    code -= 6;

                    /*
                     * If this is character 0 in the punctuation set then the next two
                     * codes make an ASCII character.
                     */

                    if (shift_state == 2 && code == 0)
                    {
                        ascii_flag = 1;
                    }

                    /*
                     * If this is character 1 in the punctuation set then this
                     * is a new line.
                     */

                    else if (shift_state == 2 && code == 1 && (uint)header.version > TxH.V1)
                    {
                        TxPrint(option_inform ? '^' : '\n');
                    }

                    /*
                     * This is a normal character so select it from the character
                     * table appropriate for the current shift state.
                     */

                    else
                    {

                        TxPrint(lookup_table[shift_state, code]);
                        char_count++;

                    }

                    shift_state = shift_lock;

                    /* Special codes 0 to 5 */

                }
                else
                {

                    /*
                     * Space: 0
                     *
                     * Output a space character.
                     *
                     */

                    if (code == 0)
                    {
                        TxPrint(' ');
                        char_count++;
                    }
                    else
                    {
                        /*
                         * The use of the synonym and shift codes is the only difference between
                         * the different versions.
                         */

                        if ((uint)header.version < TxH.V3)
                        {
                            /*
                             * Newline or synonym: 1
                             *
                             * Output a newline character or set synonym flag.
                             *
                             */

                            if (code == 1)
                            {

                                if ((uint)header.version == TxH.V1)
                                {
                                    TxPrint(option_inform ? '^' : '\n');
                                    char_count++;
                                }
                                else
                                {
                                    synonym_flag = 1;
                                    synonym = code;
                                }

                                /*
                                 * Shift keys: 2, 3, 4 or 5
                                 *
                                 * Shift keys 2 & 3 only shift the next character and can be used regardless of
                                 * the state of the shift lock. Shift keys 4 & 5 lock the shift until reset.
                                 *
                                 * The following code implements the the shift code state transitions:
                                 *
                                 *               +-------------+-------------+-------------+-------------+
                                 *               |       Shift   State       |        Lock   State       |
                                 * +-------------+-------------+-------------+-------------+-------------+
                                 * | Code        |      2      |       3     |      4      |      5      |
                                 * +-------------+-------------+-------------+-------------+-------------+
                                 * | lowercase   | uppercase   | punctuation | uppercase   | punctuation |
                                 * | uppercase   | punctuation | lowercase   | punctuation | lowercase   |
                                 * | punctuation | lowercase   | uppercase   | lowercase   | uppercase   |
                                 * +-------------+-------------+-------------+-------------+-------------+
                                 *
                                 */

                            }
                            else
                            {
                                if (code < 4)
                                    shift_state = (shift_lock + code + 2) % 3;
                                else
                                    shift_lock = shift_state = (shift_lock + code) % 3;
                            }
                        }
                        else
                        {
                            /*
                             * Synonym table: 1, 2 or 3
                             *
                             * Selects which of three synonym tables the synonym
                             * code following in the next code is to use.
                             *
                             */
                            if (code < 4)
                            {

                                synonym_flag = 1;
                                synonym = code;
                                /*
                                 * Shift key: 4 or 5
                                 *
                                 * Selects the shift state for the next character,
                                 * either uppercase (4) or punctuation (5). The shift
                                 * state automatically gets reset back to lowercase for
                                 * V3+ games after the next character is output.
                                 *
                                 */
                            }
                            else
                            {
                                shift_state = code - 3;
                                shift_lock = 0;
                            }
                        }
                    }
                }
            }
        } while ((data & 0x8000) == 0);

        return char_count;

    }/* txio.decode_text */

    //#ifdef __STDC__
    //static cache_entry_t *update_cache (uint page_number)
    //#else
    //static cache_entry_t *update_cache (page_number)
    //uint page_number;
    //#endif
    //{
    //    cache_entry_t *cachep, *lastp;

    //    for (lastp = cache, cachep = cache;
    //         cachep->flink != NULL &&
    //         cachep->page_number &&
    //         cachep->page_number != page_number;
    //         lastp = cachep, cachep = cachep->flink)
    //        ;
    //    if (cachep->page_number != page_number) {
    //        if (cachep->flink == NULL && cachep->page_number) {
    //            if (current_data_page == (uint) cachep->page_number)
    //                current_data_page = 0;
    //    }
    //        cachep->page_number = page_number;
    //        read_page (page_number, cachep->data);
    //    }
    //    if (lastp != cache) {
    //        lastp->flink = cachep->flink;
    //        cachep->flink = cache;
    //        cache = cachep;
    //    }

    //    return (cachep);

    //}/* update_cache */

    ///*
    // * get_story_size
    // *
    // * Calculate the size of the game file. Only used for very old games that do not
    // * have the game file size in the header.
    // *
    // */

    //#ifdef __STDC__
    //static ulong get_story_size (void)
    //#else
    //static ulong get_story_size ()
    //#endif
    //{
    //    ulong file_length;

    //    /* Read whole file to calculate file size */

    //    rewind (gfp);
    //    for (file_length = 0; fgetc (gfp) != EOF; file_length++)
    //    ;
    //    rewind (gfp);

    //    return (file_length);

    //}/* get_story_size */

    internal static void TxPrintf(string format, object? arg0)
    {
        if (tx_screen_cols != 0)
        {
            TxPrint(string.Format(format, arg0));
        }
        else
        {
            sb.AppendFormat(format, arg0);
        }
    }

    internal static void TxPrintf(string format, object? arg0, object? arg1)
    {
        if (tx_screen_cols != 0)
        {
            TxPrint(string.Format(format, arg0, arg1));
        }
        else
        {
            sb.AppendFormat(format, arg0, arg1);
        }
    }

    internal static void TxPrintf(string format, object? arg0, object? arg1, object? arg2)
    {
        if (tx_screen_cols != 0)
        {
            TxPrint(string.Format(format, arg0, arg1, arg2));
        }
        else
        {
            sb.AppendFormat(format, arg0, arg1, arg2);
        }
    }

    internal static void TxPrintf(string format, params object?[] args)
    {
        if (tx_screen_cols != 0)
        {
            TxPrint(string.Format(format, args));
        }
        else
        {
            sb.AppendFormat(format, args);
        }
    }

    internal static void TxPrintf(FormattableString format)
    {
        if (tx_screen_cols != 0)
        {
            TxPrint(format.ToString());
        }
        else
        {
            sb.AppendFormat(format.Format, format.GetArguments());
        }
    }

    internal static void TxPrint(ReadOnlySpan<char> chars)
    {
        if (tx_screen_cols != 0)
        {
            if (tx_line == null || tx_line.Length == 0)
            {
                tx_line = new char[TX_SCREEN_COLS];
            }

            int count = chars.Length;
            if (count > TX_SCREEN_COLS)
            {
                throw new ArgumentException("\nFatal: buffer space overflow\n");
            }
            for (int i = 0; i < count; i++)
            {
                TxWriteChar(chars[i]);
            }
        }
        else
        {
            sb.Append(chars);
        }
    }/* txio.tx_printf */

    internal static void TxPrint(char c)
    {
        if (tx_screen_cols != 0)
        {
            if (tx_line == null || tx_line.Length == 0)
            {
                tx_line = new char[TX_SCREEN_COLS];
            }

            if (1 > TX_SCREEN_COLS)
            {
                throw new ArgumentException("\nFatal: buffer space overflow\n");
            }

            TxWriteChar(c);
        }
        else
        {
            sb.Append(c);
        }
    }/* txio.tx_printf */

    private static void WriteHighZscii(int c) => Debug.WriteLine("WriteHighZscii", c);//    static zword_t unicode_table[256];//    static int unicode_table_loaded;//    int unicode_table_addr;//    int length, i;//    if (!unicode_table_loaded) {//        if (header.mouse_table && (tx_h.get_word(header.mouse_table) > 2)) {//        unicode_table_addr = tx_h.get_word(header.mouse_table + 6);//        if (unicode_table_addr) {//            length = tx_h.get_byte(unicode_table_addr);//        for (i = 0; i < unicode_table_addr; i++)//                unicode_table[i + 155] = tx_h.get_word(unicode_table_addr + 1 + i*2);//        }//    }//    unicode_table_loaded = 1;//    }//    if ((c <= 0xdf) && !unicode_table[c]) {//        if (option_inform)//        txio.tx_printf("@%s", inform_euro_substitute[c - 0x9b]);//    else//        txio.tx_printf (euro_substitute[c - 0x9b]);//    }//    else /* no non-inform version of these.  *///        txio.tx_printf("@{%x}", unicode_table[c]);

    private static void TxWriteChar(int c)
    {
        int i;
        int cp;

        /* In V6 games a tab is a paragraph indent gap and a vertical tab is
           an inter-sentence gap. Both can be set to a space for readability */

        if (c is '\v' or '\t')
            c = ' ';

        //    /* European characters should be substituted by their replacements. */

        if (c is >= 0x9b and <= 0xfb)
        {
            WriteHighZscii(c);
            return;
        }

        if (tx_col == tx_screen_cols + 1 || c == '\n')
        {
            tx_do_margin = 1;
            if (tx_line_pos < tx_line.Length) tx_line[tx_line_pos++] = '\0';
            int eol = Array.IndexOf(tx_line, '\0');
            if (eol == -1) eol = tx_line_pos;
            var temp = tx_line.AsSpan(..eol);
            cp = temp.LastIndexOf(' ');
            if (c is ' ' or '\n' || cp == -1)
            {
                sb.Append(temp);
                sb.Append('\n');
                tx_line_pos = 0;
                tx_col = 1;

                ClearTxLine();
                return;
            }
            else
            {
                tx_line[cp++] = '\0';

                sb.Append(temp[..cp]);
                sb.Append('\n');
                tx_line_pos = 0;
                tx_col = 1;
                TxPrint(temp[cp..]);
            }
        }

        if (tx_do_margin > 0)
        {
            tx_do_margin = 0;
            for (i = 1; i < tx_margin; i++)
                TxWriteChar(' ');
        }

        tx_line[tx_line_pos++] = (char)c;
        tx_col++;

    }/* tx_write_char */

    private static void ClearTxLine()
    {
        Array.Fill(tx_line, ' ');
    }

    internal static void TxFixMargin(int flag) => tx_margin = flag > 0 ? tx_col : 0; /* txio.tx_fix_margin */

    internal static void TxSetWidth(int width)
    {
        if (width > tx_screen_cols)
        {
            if (tx_line.Length > 0)
            {
                tx_line[tx_line_pos++] = '\0';
                sb.Append(tx_line);
            }
            tx_line_pos = 0;
            // free (tx_line);
            // tx_line = NULL;
        }
        tx_screen_cols = width;

    }/* tx_set_width */

    private static readonly System.Text.StringBuilder sb = new();
    internal static void StartStringBuilder() => sb.Clear();

    internal static string GetTextFromStringBuilder()
    {
        int len = sb.Length;
        for (int i = len - 1; i >= 0; --i)
        {
            if (sb[i] == '\0')
                len--;
            else
                break;
        }
        if (len < sb.Length)
            sb.Length = len;

        string text = sb.ToString();
        sb.Length = 0;

        return text;
    }
}
