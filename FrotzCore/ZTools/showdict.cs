/*
 * showdict - part of infodump
 *
 * Dictionary and abbreviation table routines.
 */

namespace ZTools;

public static class ShowDict
{
    /*
     * show_dictionary
     *
     * List the dictionary in the number of columns specified. If the number of
     * columns is one then also display the data associated with each word.
     */

    internal static void ShowDictionary(int columns)
    {
        ulong dict_address, word_address;
        uint separator_count, word_size, length;
        int i;
        bool inform_flags = false;
        int dictpar1 = 0;

        uint flag;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        var header = txio.header;

        /* Force default column count if none specified */

        if (columns == 0)
            columns = ((uint)header.version < TxH.V4) ? 5 : 4;

        /* Get dictionary configuration */

        ConfigureDictionary(out uint word_count, out ulong word_table_base, out ulong word_table_end);

        if (header.serial[0] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[1] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[2] is >= (byte)'0' and <= (byte)'1' &&
            header.serial[3] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[4] is >= (byte)'0' and <= (byte)'3' &&
            header.serial[5] is >= (byte)'0' and <= (byte)'9' &&
            header.serial[0] != '8')
        {
            inform_flags = true;
        }

        txio.TxPrint("\n    **** Dictionary ****\n\n");

        /* Display the separators */

        dict_address = word_table_base;
        separator_count = txio.ReadDataByte(ref dict_address);
        txio.TxPrint("  Word separators = \"");
        for (; separator_count > 0; separator_count--)
            txio.TxPrintf("{0:c}", (char)txio.ReadDataByte(ref dict_address));
        txio.TxPrint("\"\n");

        /* Get word size and count */

        word_size = txio.ReadDataByte(ref dict_address);
        word_count = txio.ReadDataWord(ref dict_address);

        txio.TxPrintf("  Word count = {0}, word size = {0}\n", (int)word_count, (int)word_size);

        /* Display each entry in the dictionary */

        for (i = 1; (uint)i <= word_count; i++)
        {
            /* Set column breaks */

            if (columns == 1 || (i % columns) == 1)
                txio.TxPrint("\n");

            txio.TxPrintf("[{0:d4}] ", i);

            /* Calculate address of next entry */

            word_address = dict_address;
            dict_address += word_size;

            if (columns == 1)
                txio.TxPrintf("@ ${0:X2} ", (uint)word_address);

            /* Display the text for the word */

            for (length = (uint)txio.DecodeText(ref word_address); length <= word_size; length++)
                txio.TxPrint(" ");

            /* For a single column list also display the data for each entry */

            if (columns == 1)
            {
                txio.TxPrint("[");
                for (flag = 0; word_address < dict_address; flag++)
                {
                    if (flag > 0)
                        txio.TxPrint(" ");
                    else
                        dictpar1 = TxH.GetByte(word_address);

                    txio.TxPrintf("{0:X2}", (uint)txio.ReadDataByte(ref word_address));
                }
                txio.TxPrint("]");

                if (inform_flags)
                {
                    if ((dictpar1 & TxH.NOUN) > 0)
                        txio.TxPrint(" <noun>");
                    if ((dictpar1 & TxH.PREP) > 0)
                        txio.TxPrint(" <prep>");
                    if ((dictpar1 & TxH.PLURAL) > 0)
                        txio.TxPrint(" <plural>");
                    if ((dictpar1 & TxH.META) > 0)
                        txio.TxPrint(" <meta>");
                    if ((dictpar1 & TxH.VERB_INFORM) > 0)
                        txio.TxPrint(" <verb>");
                }
                else if (header.version != TxH.V6)
                {
                    flag = (uint)(dictpar1 & TxH.DATA_FIRST);
                    switch (flag)
                    {
                        case TxH.DIR_FIRST:
                            if ((dictpar1 & TxH.DIR) > 0)
                                txio.TxPrint(" <dir>");
                            break;
                        case TxH.ADJ_FIRST:
                            if ((dictpar1 & TxH.DESC) > 0)
                                txio.TxPrint(" <adj>");
                            break;
                        case TxH.VERB_FIRST:
                            if ((dictpar1 & TxH.VERB) > 0)
                                txio.TxPrint(" <verb>");
                            break;
                        case TxH.PREP_FIRST:
                            if ((dictpar1 & TxH.PREP) > 0)
                                txio.TxPrint(" <prep>");
                            break;
                    }
                    if ((dictpar1 & TxH.DIR) > 0 && (flag != TxH.DIR_FIRST))
                        txio.TxPrint(" <dir>");
                    if ((dictpar1 & TxH.DESC) > 0 && (flag != TxH.ADJ_FIRST))
                        txio.TxPrint(" <adj>");
                    if ((dictpar1 & TxH.VERB) > 0 && (flag != TxH.VERB_FIRST))
                        txio.TxPrint(" <verb>");
                    if ((dictpar1 & TxH.PREP) > 0 && (flag != TxH.PREP_FIRST))
                        txio.TxPrint(" <prep>");
                    if ((dictpar1 & TxH.NOUN) > 0)
                        txio.TxPrint(" <noun>");
                    if ((dictpar1 & TxH.SPECIAL) > 0)
                        txio.TxPrint(" <special>");
                }
            }
        }
        txio.TxPrint("\n");

    }/* show_dictionary */

    /*
     * configure_dictionary
     *
     * Determine the dictionary start and end addresses, together with the number
     * of word entries.
     *
     * Format:
     *
     * As ASCIC string listing the punctuation to be treated as words. Correct
     * recognition of punctuation is important for parsing.
     *
     * A byte word size. Not the size of the displayed word, but the amount of data
     * occupied by each word entry in the dictionary.
     *
     * A word word count. Total size of dictionary is word count * word size.
     *
     * Word count word entries. The format of the textual part of the word is fixed
     * by the Z machine, but the data following each word can vary. The text for
     * the word starts each entry. It is a packed string. The data
     * associated with each word is used in parsing a sentence. It includes flags
     * to identify the type of word (verb, noun, etc.) and data specific to each
     * word type.
     */

    internal static void ConfigureDictionary(out uint word_count,
        out ulong word_table_base,
        out ulong word_table_end)
    {
        ulong dict_address;
        uint separator_count, word_size;

        word_table_base = 0;
        word_table_end = 0;
        word_count = 0;

        /* Dictionary base address comes from the header */
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        word_table_base = txio.header.dictionary;

        /* Skip the separator list */

        dict_address = word_table_base;
        separator_count = txio.ReadDataByte(ref dict_address);
        dict_address += separator_count;

        /* Get entry size and count */

        word_size = txio.ReadDataByte(ref dict_address);
        word_count = txio.ReadDataWord(ref dict_address);

        /* Calculate dictionary end address */

        word_table_end = (dict_address + (word_size * word_count)) - 1;

    }/* configure_dictionary */

    /*
     * show_abbreviations
     *
     * Display the list of abbreviations used to compress text strings.
     */

    internal static void ShowAbbreviations()
    {
        ulong table_address, abbreviation_address;
        int i;

        /* Get abbreviations configuration */

        ConfigureAbbreviations(out uint abbr_count, out ulong abbr_table_base, out ulong abbr_table_end,
                     out ulong abbr_data_base, out ulong abbr_data_end);

        txio.TxPrint("\n    **** Abbreviations ****\n\n");

        /* No abbreviations if count is zero (V1 games only) */

        if (abbr_count == 0)
        {
            txio.TxPrint("No abbreviation information.\n");
        }
        else
        {

            /* Display each abbreviation */

            table_address = abbr_table_base;

            for (i = 0; (uint)i < abbr_count; i++)
            {

                /* Get address of abbreviation text from table */

                abbreviation_address = (ulong)txio.ReadDataWord(ref table_address) * 2;
                txio.TxPrintf("[{0:d2}] \"", i);
                txio.DecodeText(ref abbreviation_address);
                txio.TxPrint("\"\n");
            }
        }
    }

    //}/* show_abbreviations */

    /*
     * configure_abbreviations
     *
     * Determine the abbreviation table start and end addresses, together
     * with the abbreviation text start and end addresses, and the number
     * of abbreviation entries.
     *
     * Format:
     *
     * The abbreviation information consists of two parts. Firstly a table of
     * word sized pointers corresponding to the abbreviation number, and
     * secondly, the packed string data for each abbreviation.
     *
     * Note: the pointers have to be multiplied by 2 *regardless* of the game
     * version to get the byte address for each abbreviation.
     */

    internal static void ConfigureAbbreviations(
        out uint abbr_count, out ulong abbr_table_base, out ulong abbr_table_end,
        out ulong abbr_data_base, out ulong abbr_data_end)
    {
        ulong table_address, address;
        int i, tables;

        abbr_table_base = 0;
        abbr_table_end = 0;
        abbr_data_base = 0;
        abbr_data_end = 0;
        abbr_count = 0;

        /* The abbreviation table address comes from the header */
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        abbr_table_base = txio.header.abbreviations;

        /* Check if there is any abbreviation table (V2 games and above) */

        if (abbr_table_base > 0)
        {

            /* Calculate the number of abbreviation tables (V2 = 1, V3+ = 3) */

            tables = ((uint)txio.header.version < TxH.V3) ? 1 : 3;

            /* Calculate abbreviation count and table end address */

            abbr_count = (uint)(tables * 32);
            abbr_table_end = abbr_table_base + (abbr_count * 2) - 1;

            /* Calculate the high and low address for the abbreviation strings */

            table_address = abbr_table_base;
            for (i = 0; (uint)i < abbr_count; i++)
            {
                address = (ulong)txio.ReadDataWord(ref table_address) * 2;
                if (abbr_data_base == 0 || address < abbr_data_base)
                    abbr_data_base = address;
                if (abbr_data_end == 0 || address > abbr_data_end)
                    abbr_data_end = address;
            }

            /* Scan last string to get the actual end of the string */

            while (((uint)txio.ReadDataWord(ref abbr_data_end) & 0x8000) == 0) ;

            abbr_data_end--;
        }

    }/* configure_abbreviations */
}
