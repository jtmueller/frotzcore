/*
 * infodump V7/3
 *
 * Infocom data file dumper, etc. for V1 to V8 games.
 * Works on everything I have, except for parsing information in V6 games.
 *
 * The most useful options are; -i to display the header information, such as
 * game version, serial number and release; -t to display the object tree which
 * shows where all objects start and which item is in which room; -g to show
 * the sentence grammar acceptable to the game; -d to see all the recognised
 * words.
 *
 * Required files:
 *    showhead.c - show header information
 *    showdict.c - show dictionary and abbreviations
 *    showobj.c  - show objects
 *    showverb.c - show verb grammar
 *    txio.c     - I/O support
 *    tx.h       - Include file required by everything above
 *    getopt.c   - The standard getopt function
 *
 * Usage: infodump [options...] story-file [story-file...]
 *     -i   show game information in header (default)
 *     -a   show abbreviations
 *     -m   show data file map
 *     -o   show objects
 *     -t   show object tree
 *     -g   show verb grammar
 *     -d n show dictionary (n = columns)
 *     -a   all of the above
 *     -w n display width (0 = no wrap)
 *
 * Mark Howell 28 August 1992 howell_ma@movies.enet.dec.com
 *
 * History:
 *    Fix verb table display for later V4 and V5 games
 *    Fix property list
 *    Add verb table
 *    Add support for V1 and V2 games
 *    Improve output
 *    Improve verb formatting
 *    Rewrite and add map
 *    Add globals address and V6 start PC
 *    Fix lint warnings and some miscellaneous bugs
 *    Add support for V7 and V8 games
 *    Fix Inform grammar tables
 *    Fix Inform adjectives table
 *    Add support for Inform 6 (helped by Matthew T. Russotto)
 *    Add header flag for "timed input"
 *    Add header extension table and Unicode table
 *    Add Inform and user symbol table support
 */

namespace ZTools;

public static class InfoDump
{

    /* Options */

    //static short OPTION_A = 0;
    private static readonly short OPTION_I = 1;

    //static short OPTION_O = 2;
    //static short OPTION_T = 3;
    //static short OPTION_G = 4;
    //static short OPTION_D = 5;
    //static short OPTION_M = 6;
    private const short MAXOPT = 7;

    /*
     * main
     *
     * Process command line arguments and process each story file.
     */

    // TODO Make this internal and just pass it out from txd.main (which I should also rename)
    public record ZToolInfo(string Header, string Text);

    public static List<ZToolInfo> Main(byte[] storyFile, string[] args)
    {
        //int i;
        // int c, f, errflg = 0;
        int columns;
        int[] options = new int[MAXOPT];
        int symbolic;

        /* Clear all options */
        Array.Clear(options);

        columns = 0;
        symbolic = 0;

        /* Parse the options */

        options[OPTION_I] = 1;

        //    while ((c = getopt (argc, argv, "hafiotgmdsc:w:u:")) != EOF) {
        //    switch (c) {
        //        case 'f':
        //        for (i = 0; i < MAXOPT; i++)
        //            options[i] = 1;
        //        break;
        //        case 'a':
        //        options[OPTION_A] = 1;
        //        break;
        //        case 'i':
        //        options[OPTION_I] = 1;
        //        break;
        //        case 'o':
        //        options[OPTION_O] = 1;
        //        break;
        //        case 't':
        //        options[OPTION_T] = 1;
        //        break;
        //        case 'g':
        //        options[OPTION_G] = 1;
        //        break;
        //        case 'm':
        //        options[OPTION_M] = 1;
        //        break;
        //        case 'd':
        //        options[OPTION_D] = 1;
        //        break;
        //        case 's':
        //        symbolic = 1;
        //        break;
        //        case 'c':
        //        columns = atoi (optarg);
        //        break;
        //        case 'w':
        //        tx_set_width (atoi (optarg));
        //        break;
        //        case 'u':
        //            symbolic = 1;
        //        init_symbols (optarg);
        //        break;
        //        case 'h':
        //        case '?':
        //        default:
        //        errflg++;
        //    }
        //    }

        /* Display usage if unknown flag or no story file */

        //    if (errflg || optind >= argc) {
        //    show_help (argv[0]);
        //    exit (EXIT_FAILURE);
        //    }

        //    /* If no options then force header option information on */

        //    for (f = 0, i = 0; i < MAXOPT; i++)
        //    f += options[i];
        //    if (f == 0)
        //    options[OPTION_I] = 1;

        /* Process any story files on the command line */

        return ProcessStory(storyFile, options, columns, symbolic);

    } /* main */

    /*
     * show_help
     */

    private static void ShowHelp(string program)
    {
        Console.Error.WriteLine($"usage: {program} [options...] story-file [story-file...]\n\n");
        Console.Error.WriteLine("INFODUMP version 7/3 - display Infocom story file information. By Mark Howell\n");
        Console.Error.WriteLine("Works with V1 to V8 Infocom games.\n\n");
        Console.Error.WriteLine("\t-i   show game information in header (default)\n");
        Console.Error.WriteLine("\t-a   show abbreviations\n");
        Console.Error.WriteLine("\t-m   show data file map\n");
        Console.Error.WriteLine("\t-o   show objects\n");
        Console.Error.WriteLine("\t-t   show object tree\n");
        Console.Error.WriteLine("\t-g   show verb grammar\n");
        Console.Error.WriteLine("\t-d   show dictionary\n");
        Console.Error.WriteLine("\t-f   full report (all of the above)\n");
        Console.Error.WriteLine("\t-c n number of columns for dictionary display\n");
        Console.Error.WriteLine("\t-w n display width (0 = no wrap)\n");
        Console.Error.WriteLine("\t-s Display Inform symbolic names in object and grammar displays\n");
        Console.Error.WriteLine("\t-u <file> Display symbols from file in object and grammar displays (implies -s)\n");

    }/* show_help */

    /*
     * process_story
     *
     * Load the story and display all parts of the data file requested.
     */

    private static List<ZToolInfo> ProcessStory(byte[] storyFile, int[] options, int columns, int symbolic)
    {
        List<ZToolInfo> _areas = new();

        txio.OpenStory(storyFile);

        txio.Configure(TxH.V1, TxH.V8);

        // txio.load_cache ();

        FixDictionary();

        txio.StartStringBuilder();
        ShowHead.ShowHeader();
        _areas.Add(new ZToolInfo("Header", txio.GetTextFromStringBuilder()));

        ShowMap();
        _areas.Add(new ZToolInfo("Map", txio.GetTextFromStringBuilder()));

        ShowDict.ShowAbbreviations();
        _areas.Add(new ZToolInfo("Abbreviations", txio.GetTextFromStringBuilder()));

        ShowObj.show_objects(symbolic);
        _areas.Add(new ZToolInfo("Objects", txio.GetTextFromStringBuilder()));

        ShowObj.ShowTree();
        _areas.Add(new ZToolInfo("Tree", txio.GetTextFromStringBuilder()));

        ShowVerb.ShowVerbs(symbolic);
        _areas.Add(new ZToolInfo("Verbs", txio.GetTextFromStringBuilder()));

        ShowDict.ShowDictionary(columns);
        _areas.Add(new ZToolInfo("Dictionary", txio.GetTextFromStringBuilder()));

        txio.CloseStory();

        return _areas;

    } /* process_story */

    /*
     * fix_dictionary
     *
     * Fix the end of text flag for each word in the dictionary. Some older games
     * are missing the end of text flag on some words. All the words are fixed up
     * so that they can be printed.
     */

    internal static void FixDictionary()
    {
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        ulong address = txio.header.dictionary;
        ulong separator_count = txio.ReadDataByte(ref address);
        address += separator_count;
        byte word_size = txio.ReadDataByte(ref address);
        ushort word_count = txio.ReadDataWord(ref address);

        for (int i = 1; i <= word_count; i++)
        {

            /* Check that the word is in non-paged memory before writing */

            if ((address + 4) < txio.header.resident_size)
            {
                if ((uint)txio.header.version <= TxH.V3)
                    TxH.SetByte((int)address + 2, (byte)(TxH.GetByte(address + 2) | 0x80));
                else
                    TxH.SetByte((int)address + 4, (byte)(TxH.GetByte(address + 4) | 0x80));
            }

            address += word_size;
        }

    } /* fix_dictionary */

    private const int MAX_AREA = 20;

    private static void SetArea(ulong base_addr, ulong end_addr, string name_string)
    {
        var a = new AreaT(base_addr, end_addr, name_string);
        areas.Add(a);
    }

    private readonly record struct AreaT(ulong AreaBase, ulong End, string Name);

    /*
     * show_map
     *
     * Show the map of the data area. This is done by calling the configure routine
     * for each area. Each area is then sorted in ascending order and displayed.
     */

    private static readonly PooledList<AreaT> areas = new();

    private static void ShowMap()
    {
        uint ext_table_size;
        ulong ext_table_base, ext_table_end;
        ulong unicode_table_base, unicode_table_end;
        int i;

        areas.Clear();

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        var header = txio.header;

        /* Configure areas */

        SetArea(0, 63, "Story file header");

        ext_table_base = header.mouse_table;
        if (ext_table_base > 0)
        {
            ext_table_size = TxH.GetWord((int)ext_table_base);
            ext_table_end = ext_table_base + 2 + ext_table_size * 2 - 1;
            SetArea(ext_table_base, ext_table_end, "Header extension table");
            if (ext_table_size > 2)
            {
                unicode_table_base = TxH.GetWord((int)ext_table_base + 6);
                if (unicode_table_base > 0)
                {
                    unicode_table_end = unicode_table_base + (ulong)TxH.GetByte((int)unicode_table_base) * 2;
                    SetArea(unicode_table_base, unicode_table_end, "Unicode table");
                }
            }
        }

        ShowDict.ConfigureAbbreviations(out uint abbr_count, out ulong abbr_table_base, out ulong abbr_table_end,
                     out ulong abbr_data_base, out ulong abbr_data_end);

        if (abbr_count > 0)
        {
            SetArea(abbr_table_base, abbr_table_end, "Abbreviation pointer table");
            SetArea(abbr_data_base, abbr_data_end, "Abbreviation data");
        }

        ShowDict.ConfigureDictionary(out _, out ulong word_table_base, out ulong word_table_end);

        SetArea(word_table_base, word_table_end, "Dictionary");
        ShowObj.ConfigureObjectTables(out _, out ulong obj_table_base, out ulong obj_table_end,
                     out ulong obj_data_base, out ulong obj_data_end);

        SetArea(obj_table_base, obj_table_end, "Object table");
        SetArea(obj_data_base, obj_data_end, "Property data");

        ShowVerb.ConfigureParseTables(out uint verb_count, out uint action_count, out uint parse_count, out uint verb_type, out uint prep_type,
                    out ulong verb_table_base, out ulong verb_data_base,
                    out ulong action_table_base, out ulong preact_table_base,
                    out ulong prep_table_base, out ulong prep_table_end);

        if ((verb_count > 0) && (verb_type != (int)TxH.ParserTypes.Infocom6Grammar))
        {
            SetArea(verb_table_base, verb_data_base - 1, "Grammar pointer table");
            SetArea(verb_data_base, action_table_base - 1, "Grammar data");
            SetArea(action_table_base, preact_table_base - 1, "Action routine table");
            if (verb_type < (int)TxH.ParserTypes.InformGV2)
            {
                SetArea(preact_table_base, prep_table_base - 1, (verb_type >= (int)TxH.ParserTypes.Inform5Grammar) ? "Parsing routine table" : "Pre-action routine table");
                SetArea(prep_table_base, prep_table_end, "Preposition table");
            }
        }
        else if (verb_count > 0)
        {
            SetArea(verb_table_base, verb_table_base + 8 * verb_count - 1, "Verb grammar table");
            SetArea(verb_data_base, prep_table_base - 1, "Grammar entries");
            SetArea(action_table_base, preact_table_base - 1, "Action routine table");
            SetArea(preact_table_base, preact_table_base + action_count * 2 - 1, "Pre-action routine table");
        }

        InfInfo.ConfigureInformTables(obj_data_end, out ushort inform_version, out ulong class_numbers_base, out ulong class_numbers_end,
                        out ulong property_names_base, out ulong property_names_end, out ulong attr_names_base, out ulong attr_names_end);

        if (inform_version >= TxH.INFORM_6)
        {
            SetArea(class_numbers_base, class_numbers_end, "Class Prototype Object Numbers");
            SetArea(property_names_base, property_names_end, "Property Names Table");
            if (inform_version >= TxH.INFORM_610)
            {
                SetArea(attr_names_base, attr_names_end, "Attribute Names Table");
            }
        }

        SetArea(header.globals,
              (ulong)header.globals + (240 * 2) - 1,
              "Global variables");

        SetArea(header.resident_size,
              (ulong)txio.file_size - 1,
              "Paged memory");

        if (header.alphabet > 0)
        {
            SetArea(header.alphabet,
                  (ulong)header.alphabet + (26 * 3) - 1,
                  "Alphabet");
        }

        /* Sort areas */

        areas.Sort(default(AreaComparer));

        /* Print area map */

        txio.TxPrint("\n    **** Story file map ****\n\n");

        txio.TxPrint(" Base    End   Size\n");
        for (i = 0; i < areas.Count; i++)
        {
            if (i > 0 && (areas[i].AreaBase - 1) > areas[i - 1].End)
            {
                txio.TxPrintf("{0,5:X}  {1,5:X}  {2,5:X}\n",
                       areas[i - 1].End + 1, areas[i].AreaBase - 1,
                       (areas[i].AreaBase - 1) - (areas[i - 1].End + 1) + 1);
            }

            txio.TxPrintf("{0,5:X}  {1,5:X}  {2,5:X}  {3}\n",
                   areas[i].AreaBase, areas[i].End,
                   areas[i].End - areas[i].AreaBase + 1,
                   areas[i].Name);
        }


    }/* show_map */

    private readonly struct AreaComparer : IComparer<AreaT>
    {
        public int Compare(AreaT x, AreaT y) => (int)(x.AreaBase - y.AreaBase);
    }
}
