// #define TXD_DEBUG

/* txd.c V7/3
 *
 * Z code disassembler for Infocom game files
 *
 * Requires txio.c, getopt.c, showverb.c and tx.h.
 *
 * Works for all V1, V2, V3, V4, V5, V6, V7 and V8 games.
 *
 * Usage: txd story-file-name
 *
 * Mark Howell 25 August 1992 howell_ma@movies.enet.dec.com
 *
 * History:
 *    Merge separate disassemblers for each type into one program
 *    Fix logic error in low routine scan
 *    Force PC past start PC in middle routine scan
 *    Update opcodes
 *    Add pre-action and action verb names to routines
 *    Add print mode descriptions
 *    Wrap long lines of text
 *    Cleanup for 16 bit machines
 *    Change JGE and JLE to JG and JL
 *    Add support for V1 and V2 games
 *    Fix bug in printing of last string
 *    Add symbolic names for routines and labels
 *    Add verb syntax
 *    Improve verb formatting
 *    Add command line options
 *    Fix check for low address
 *    Add a switch to turn off grammar
 *    Add support for V6 games
 *    Make start of code for low scan the end of dictionary data
 *    Generate Inform style syntax as an option
 *    Add dump style and width option
 *    Fix lint warnings
 *    Fix inter-routine backward jumps
 *    Update operand names
 *    Add support for V7 and V8 games
 *    Limit cache size to MAX_CACHE pages
 *    Improve translation of constants to symbols
 *    Distinguish between pre-actions and Inform parsing routines
 *    Introduce indirect operands, eg. load [sp] sp
 *    Fix object 0 problem
 *    Add support for European characters (codes up to 223)
 *    Add support for Inform 6 (helped by Matthew T. Russotto)
 *    Add support for GV2 (MTR)
 *    Add support for Infocom V6 games
 *    Fix dependencies on sizeof(int) == 2 (mostly cosmetic) NOT DONE
 *    Add -S dump-strings at address option
 *    Remove GV2A support
 *    Add unicode disassembly support
 *    Add Inform and user symbol table support
 */

namespace ZTools;

using zbyte_t = System.Byte;
using zword_t = System.UInt16;

public static class Txd
{
    internal const int MAX_PCS = 100;

    private static ulong RoundCode(ulong address)
        => (address + (txio.code_scaler - 1)) & ~(txio.code_scaler - 1);

    private static ulong RoundData(ulong address)
        => (address + (txio.story_scaler - 1)) & ~(txio.story_scaler - 1);

    private static ulong[] pctable = Array.Empty<ulong>();
    private static int pcindex = 0;
    private static ulong start_data_pc, end_data_pc;
    private static TxH.DecodeT? decode;
    private static TxH.OpcodeT? opcode;
    private static List<TxH.CRefItemT>? strings_base = null;
    private static List<TxH.CRefItemT>? routines_base = null;
    private static TxH.CRefItemT? current_routine = null;
    private static int locals_count = 0;
    private static ulong start_of_routine = 0;
    private static uint verb_count = 0;
    private static uint action_count = 0;
    private static uint parse_count = 0;
    private static uint parser_type = 0;
    private static uint prep_type = 0;
    private static ulong verb_table_base = 0;
    private static ulong verb_data_base = 0;
    private static ulong action_table_base = 0;
    private static ulong preact_table_base = 0;
    private static ulong prep_table_base = 0;
    private static ulong prep_table_end = 0;
    //private static readonly int[] verb_sizes = new int[4] { 2, 4, 7, 7 };

    private static ulong dict_start = 0;
    private static ulong dict_end = 0;
    private static ulong word_size = 0;
    private static ulong word_count = 0;
    private static ulong code_base = 0;
    private static int obj_count = 0;
    private static ulong obj_table_base = 0, obj_table_end = 0, obj_data_base = 0, obj_data_end = 0;
    private static ushort inform_version = 0;
    private static ulong class_numbers_base = 0, class_numbers_end = 0;
    private static ulong property_names_base = 0, property_names_end = 0;
    private static ulong attr_names_base = 0, attr_names_end = 0;
    private static readonly int option_labels = 1;
    private static readonly int option_grammar = 1;
    private static readonly int option_dump = 0;
    private static readonly int option_width = 79;
    private static readonly int option_symbols = 0;
    private static readonly ulong string_location = 0;

    public static string Main(byte[] storyData, string[] args) =>
        //    int c, errflg = 0;

        //    /* Parse the options */

        //    while ((c = getopt (argc, argv, "abdghnsw:S:u:")) != EOF) {
        //    switch (c) {
        //        case 'a':
        //        option_inform = 6;
        //        break;
        //        case 'd':
        //        option_dump = 1;
        //        break;
        //        case 'g':
        //        option_grammar = 0;
        //        break;
        //        case 'n':
        //        option_labels = 0;
        //        break;
        //        case 'w':
        //        option_width = atoi (optarg);
        //        break;
        //        case 'u':
        //        init_symbols(optarg);
        //        /*FALLTHRU*/
        //        case 's':
        //        option_symbols = 1;
        //        break;
        //        case 'S':
        //#ifdef HAS_STRTOUL
        //            string_location = strtoul(optarg, (char **)NULL, 0);
        //#else
        //            string_location = atoi(optarg);
        //#endif
        //        break;
        //        case 'h':
        //        case '?':
        //        default:
        //        errflg++;
        //    }
        //    }

        //    /* Display usage if unknown flag or no story file */

        //    if (errflg || optind >= argc) {
        //    (void) fprintf (stderr, "usage: %s [options...] story-file [story-file...]\n\n", argv[0]);
        //    (void) fprintf (stderr, "TXD version 7/3 - disassemble Infocom story files. By Mark Howell\n");
        //    (void) fprintf (stderr, "Works with V1 to V8 Infocom games.\n\n");
        //    (void) fprintf (stderr, "\t-a   generate alternate syntax used by Inform\n");
        //    (void) fprintf (stderr, "\t-d   dump hex of opcodes and data\n");
        //    (void) fprintf (stderr, "\t-g   turn off grammar for action routines\n");
        //    (void) fprintf (stderr, "\t-n   use addresses instead of labels\n");
        //    (void) fprintf (stderr, "\t-w n display width (0 = no wrap)\n");
        //    (void) fprintf (stderr, "\t-s   Symbolic mode (Inform 6+ only)\n");
        //    (void) fprintf (stderr, "\t-u <file> Read user symbol table, implies -s for Inform games\n");
        //    (void) fprintf (stderr, "\t-S n Dump high strings only, starting at address n\n");
        //    exit (EXIT_FAILURE);
        //    }

        //    /* Process any story files on the command line */

        //    for (; optind < argc; optind++)
        ProcessStory(storyData);//    exit (EXIT_SUCCESS);/* main */

    internal static string ProcessStory(byte[] storyData)
    {
        routines_base = new System.Collections.Generic.List<TxH.CRefItemT>();

        decode = new TxH.DecodeT();
        opcode = new TxH.OpcodeT();

        pctable = new ulong[MAX_PCS];

        txio.TxSetWidth(option_width);

        txio.OpenStory(storyData);

        txio.StartStringBuilder();

        txio.Configure(TxH.V1, TxH.V8);

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");

        var header = txio.header;

        //    load_cache ();

        SetupDictionary();

        if (option_grammar > 0)
        {
            ShowVerb.ConfigureParseTables(out verb_count, out action_count, out parse_count, out parser_type, out prep_type,
                        out verb_table_base, out verb_data_base,
                        out action_table_base, out preact_table_base,
                        out prep_table_base, out prep_table_end);
        }

        if (option_symbols > 0 && (parser_type >= (int)TxH.ParserTypes.InformGV1))
        {
            ShowObj.ConfigureObjectTables(out obj_count, out obj_table_base, out obj_table_end,
                              out obj_data_base, out obj_data_end);
            InfInfo.ConfigureInformTables(obj_data_end, out inform_version, out class_numbers_base, out class_numbers_end,
                            out property_names_base, out property_names_end, out attr_names_base, out attr_names_end);
        }

        if (header.version != TxH.V6 && header.version != TxH.V7)
        {
            decode.Pc = code_base = dict_end;
            decode.InitialPc = (ulong)header.start_pc - 1;
        }
        else
        {
            decode.Pc = code_base = header.routines_offset * txio.story_scaler;
            decode.InitialPc = decode.Pc + header.start_pc * txio.code_scaler;
        }

        txio.TxPrintf("Resident data ends at {0:X}, program starts at {1:X}, file ends at {2:X}\n",
               (ulong)header.resident_size, decode.InitialPc, (ulong)txio.file_size);
        txio.TxPrintf("\nStarting analysis pass at address {0:X}\n", decode.Pc);

#if TXD_DEBUG
        decode.first_pass = 0;
        decode.low_address = decode.initial_pc;
        decode.high_address = (ulong)txio.file_size;
#else
        decode.FirstPass = true;
#endif

        if (string_location > 0)
        {
            DecodeStrings(string_location);
            return txio.GetTextFromStringBuilder();
        }
        DecodeProgram();

        ScanStrings(decode.Pc);

#if !TXD_DEBUG
        txio.TxPrintf("\nEnd of analysis pass, low address = {0:X}, high address = {1:X}\n",
               decode.LowAddress, decode.HighAddress);
        if (start_data_pc > 0)
        {
            txio.TxPrintf("\n{0} bytes of data in code from {1:X} to {2:X}\n",
                   end_data_pc - start_data_pc,
                   start_data_pc, end_data_pc);
        }

        if ((decode.LowAddress - code_base) >= txio.story_scaler)
        {
            txio.TxPrintf("\n{0} bytes of data before code from {1:X} to {2:X}\n",
                   decode.LowAddress - code_base,
                   code_base, decode.LowAddress);
            if (option_dump > 0)
            {
                txio.TxPrint("\n[Start of data");
                if (option_labels == 0)
                    txio.TxPrintf(" at {0:X}", code_base);
                txio.TxPrint("]\n\n");
                DumpData(code_base, decode.LowAddress - 1);
                txio.TxPrint("\n[End of data");
                if (option_labels == 0)
                    txio.TxPrintf(" at {0:X}", decode.LowAddress - 1);
                txio.TxPrint("]\n");
            }
        }

        if (option_labels > 0)
            RenumberCref(routines_base);

        decode.FirstPass = false;
        DecodeProgram();

        DecodeStrings(decode.Pc);
#endif

        txio.CloseStory();

        return txio.GetTextFromStringBuilder();

    }/* process_story */

    ///* decode_program - Decode Z code in two passes */

    // static int count = 0;

    internal static void DecodeProgram()
    {
        ulong pc, low_pc, high_pc, prev_low_pc, prev_high_pc;
        int i, vars;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        if (decode is null)
            throw new InvalidOperationException("txd was not initialized");

        if (decode.FirstPass)
        {
            bool flag;
            if (decode.Pc < decode.InitialPc)
            {
                /* Scan for low routines */
                decode.Pc = RoundCode(decode.Pc);
                for (pc = decode.Pc, flag = false; pc < decode.InitialPc && !flag; pc += txio.code_scaler)
                {
                    for (vars = (char)txio.ReadDataByte(ref pc); vars < 0 || vars > 15; vars = (char)txio.ReadDataByte(ref pc))
                        pc = RoundCode(pc);
                    decode.Pc = pc - 1;
                    for (i = 0, flag = true; i < 3 && flag; i++)
                    {
                        pcindex = 0;
                        decode.Pc = RoundCode(decode.Pc);
                        if (DecodeRountine() != TxH.END_OF_ROUTINE || pcindex > 0)
                            flag = false;
                    }
                    decode.Pc = pc - 1;
                }
                if (flag && (uint)header.version < TxH.V5)
                {
                    pc = decode.Pc;
                    vars = (char)txio.ReadDataByte(ref pc);
                    low_pc = decode.Pc;
                    for (pc = pc + ((ulong)vars * 2) - 1, flag = false; pc >= low_pc && !flag; pc -= txio.story_scaler)
                    {
                        decode.Pc = pc;
                        for (i = 0, flag = true; i < 3 && flag; i++)
                        {
                            pcindex = 0;
                            decode.Pc = RoundCode(decode.Pc);
                            if (DecodeRountine() != TxH.END_OF_ROUTINE || pcindex > 0)
                                flag = false;
                        }
                        decode.Pc = pc;
                    }
                }
                if (!flag || decode.Pc > decode.InitialPc)
                    decode.Pc = decode.InitialPc;
            }
            /* Fill in middle routines */
            decode.LowAddress = decode.Pc;
            decode.HighAddress = decode.Pc;
            start_data_pc = 0;
            end_data_pc = 0;
            do
            {
                if (option_labels > 0)
                {
                    routines_base = null;
                }
                prev_low_pc = decode.LowAddress;
                prev_high_pc = decode.HighAddress;
                flag = false;
                pcindex = 0;
                low_pc = decode.LowAddress;
                high_pc = decode.HighAddress;
                pc = decode.Pc = decode.LowAddress;
                while (decode.Pc <= high_pc || decode.Pc <= decode.InitialPc)
                {
                    if (start_data_pc == decode.Pc)
                        decode.Pc = end_data_pc;
                    if (DecodeRountine() != TxH.END_OF_ROUTINE)
                    {
                        if (start_data_pc == 0)
                            start_data_pc = decode.Pc;
                        flag = true;
                        end_data_pc = 0;
                        pcindex = 0;
                        pc = RoundCode(pc);
                        do
                        {
                            pc += txio.code_scaler;
                            vars = (char)txio.ReadDataByte(ref pc);
                            pc--;
                        } while (vars is < 0 or > 15);
                        decode.Pc = pc;
                    }
                    else
                    {
                        if (pc < decode.InitialPc && decode.Pc > decode.InitialPc)
                        {
                            pc = decode.Pc = decode.InitialPc;
                            decode.LowAddress = low_pc;
                            decode.HighAddress = high_pc;
                        }
                        else
                        {
                            if (start_data_pc > 0 && end_data_pc == 0)
                                end_data_pc = pc;
                            pc = RoundCode(decode.Pc);
                            if (!flag)
                            {
                                low_pc = decode.LowAddress;
                                high_pc = decode.HighAddress;
                            }
                        }
                    }
                }
                decode.LowAddress = low_pc;
                decode.HighAddress = high_pc;
                // Console.WriteLine("{0} < {1} : {2} > {3}", low_pc, prev_low_pc, high_pc, prev_high_pc);

            } while (low_pc < prev_low_pc || high_pc > prev_high_pc);
            /* Scan for high routines */
            pc = decode.Pc;
            while (DecodeRountine() == TxH.END_OF_ROUTINE)
            {
                decode.HighAddress = pc;
                pc = decode.Pc;
            }
        }
        else
        {
            txio.TxPrint("\n[Start of code");
            if (option_labels == 0)
                txio.TxPrintf(" at {0:X}", decode.LowAddress);
            txio.TxPrint("]\n");
            for (decode.Pc = decode.LowAddress;
                 decode.Pc <= decode.HighAddress;)
            {
                DecodeRountine();
            }

            txio.TxPrint("\n[End of code");
            if (option_labels == 0)
                txio.TxPrintf(" at {0:X}", decode.Pc);
            txio.TxPrint("]\n");
        }

    }/* decode_program */

    /* decode_routine - Decode a routine from start address to last instruction */

    private static int DecodeRountine()
    {
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        if (decode is null)
            throw new InvalidOperationException("txd was not initialized");

        ulong old_pc, old_start;
        TxH.CRefItemT? cref_item;
        int vars, status, i, locals;

        if (decode.FirstPass)
        {
            cref_item = null;
            if (option_labels > 0)
                cref_item = current_routine;
            old_start = start_of_routine;
            locals = locals_count;
            old_pc = decode.Pc;
            decode.Pc = RoundCode(decode.Pc);
            vars = txio.ReadDataByte(ref decode.Pc);
            if (vars is >= 0 and <= 15)
            {
                if (option_labels > 0)
                    AddRoutine(decode.Pc - 1);
                locals_count = vars;
                if ((uint)header.version < TxH.V5)
                {
                    for (; vars > 0; vars--)
                        txio.ReadDataWord(ref decode.Pc);
                }

                start_of_routine = decode.Pc;
                if (DecodeCode() == TxH.END_OF_ROUTINE)
                    return (TxH.END_OF_ROUTINE);
                if (option_labels > 0)
                    current_routine = cref_item;
                start_of_routine = old_start;
                locals_count = locals;
            }
            decode.Pc = old_pc;
            if ((status = DecodeCode()) != TxH.END_OF_ROUTINE)
            {
                decode.Pc = old_pc;
            }
            else
            {
                pctable[pcindex++] = old_pc;
                if (pcindex == MAX_PCS)
                {
                    throw new NotImplementedException("\nFatal: too many orphan code fragments\n");
                }
            }
        }
        else
        {
            if (decode.Pc == start_data_pc)
            {
                if (option_dump > 0)
                {
                    txio.TxPrint("\n[Start of data");
                    if (option_labels == 0)
                        txio.TxPrintf(" at {0:X}", start_data_pc);
                    txio.TxPrint("]\n\n");
                    DumpData(start_data_pc, end_data_pc - 1);
                    txio.TxPrint("\n[End of data");
                    if (option_labels == 0)
                        txio.TxPrintf(" at {0:X}", end_data_pc - 1);
                    txio.TxPrint("]\n");
                }
                decode.Pc = end_data_pc;
            }
            for (i = 0; i < pcindex && decode.Pc != pctable[i]; i++)
                ;
            if (i == pcindex)
            {
                decode.Pc = RoundCode(decode.Pc);
                start_of_routine = decode.Pc;
                vars = txio.ReadDataByte(ref decode.Pc);
                if (option_labels > 0)
                {
                    txio.TxPrintf("{0}outine {1}{2:d4}, {3} local",
                                       (decode.Pc - 1 == decode.InitialPc) ? "\nMain r" : "\nR",
                                       (txio.option_inform) ? 'r' : 'R',
                           LookupRoutine(decode.Pc - 1, 1),
                                       vars);
                }
                else
                {
                    txio.TxPrintf("{0}outine {1:X}, {2} local",
                           (decode.Pc - 1 == decode.InitialPc) ? "\nMain r" : "\nR",
                                       decode.Pc - 1,
                                       vars);
                }
                if (vars != 1)
                    txio.TxPrint('s');
                if ((uint)header.version < TxH.V5)
                {
                    txio.TxPrint(" (");
                    txio.TxFixMargin(1);
                    for (; vars > 0; vars--)
                    {
                        txio.TxPrintf("{0:X4}", (uint)txio.ReadDataWord(ref decode.Pc));
                        if (vars > 1)
                            txio.TxPrint(", ");
                    }
                    txio.TxFixMargin(0);
                    txio.TxPrint(')');
                }
                txio.TxPrint('\n');
                LookupVerb(start_of_routine);
                txio.TxPrint('\n');
            }
            else
            {
                txio.TxPrint("\norphan code fragment:\n\n");
            }

            status = DecodeCode();
        }

        return (status);

    }/* decode_routine */

    /* decode_code - grab opcode and determine the class */

    private static int DecodeCode()
    {
        int status;
        int label;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        if (decode is null || opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        decode.HighPc = decode.Pc;
        do
        {
            if (!decode.FirstPass)
            {
                if (option_labels > 0)
                {
                    label = LookupLabel(decode.Pc, 0);
                    if (label != 0)
                        txio.TxPrintf("{0}{1:d4}: ", (txio.option_inform) ? 'l' : 'L', label);
                    else
                        txio.TxPrint("       ");
                }
                else
                {
                    txio.TxPrintf("{0:X5}:  ", decode.Pc);
                }
            }
            opcode.OpCode = txio.ReadDataByte(ref decode.Pc);

            if ((uint)header.version > TxH.V4 && opcode.OpCode == 0xbe)
            {
                opcode.OpCode = txio.ReadDataByte(ref decode.Pc);
                opcode.OpClass = TxH.EXTENDED_OPERAND;
            }
            else
            {
                opcode.OpClass = opcode.OpCode < 0x80
                    ? TxH.TWO_OPERAND
                    : opcode.OpCode < 0xb0
                        ? TxH.ONE_OPERAND
                        : opcode.OpCode < 0xc0 ? TxH.ZERO_OPERAND : TxH.VARIABLE_OPERAND;
            }

            status = DecodeOpcode();
        } while (status == TxH.END_OF_INSTRUCTION);

        return (status);

    }/* decode_code */


    /* decode_opcode - Check and decode the opcode itself */

    private static int DecodeOpcode()
    {
        int code;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        if (opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        code = opcode.OpCode;

        switch (opcode.OpClass)
        {
            case TxH.EXTENDED_OPERAND:
                code &= 0x3f;
                return code switch
                {
                    0x00 => DecodeOperands("SAVE", TxH.LOW_ADDR, TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x01 => DecodeOperands("RESTORE", TxH.LOW_ADDR, TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x02 => DecodeOperands("LOG_SHIFT", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x03 => DecodeOperands("ART_SHIFT", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x04 => DecodeOperands("SET_FONT", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x05 => DecodeOperands("DRAW_PICTURE", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x06 => DecodeOperands("PICTURE_DATA", TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN),
                    0x07 => DecodeOperands("ERASE_PICTURE", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x08 => DecodeOperands("SET_MARGINS", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x09 => DecodeOperands("SAVE_UNDO", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x0A => DecodeOperands("RESTORE_UNDO", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),

                    0x10 => DecodeOperands("MOVE_WINDOW", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x11 => DecodeOperands("WINDOW_SIZE", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x12 => DecodeOperands("WINDOW_STYLE", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x13 => DecodeOperands("GET_WIND_PROP", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN),
                    0x14 => DecodeOperands("SCROLL_WINDOW", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x15 => DecodeOperands("POP_STACK", TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x16 => DecodeOperands("READ_MOUSE", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x17 => DecodeOperands("MOUSE_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x18 => DecodeOperands("PUSH_STACK", TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN),
                    0x19 => DecodeOperands("PUT_WIND_PROP", TxH.NUMBER, TxH.NUMBER, TxH.ANYTHING, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x1A => DecodeOperands("PRINT_FORM", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),
                    0x1B => DecodeOperands("MAKE_MENU", TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN),
                    0x1C => DecodeOperands("PICTURE_TABLE", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN),

                    _ => (DecodeOperands("ILLEGAL", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.ILLEGAL)),
                };
            case TxH.TWO_OPERAND:
            case TxH.VARIABLE_OPERAND:
                if (opcode.OpClass == TxH.TWO_OPERAND)
                {
                    code &= 0x1f;
                }

                code &= 0x3f;
                switch (code)
                {
                    case 0x01: return DecodeOperands("JE", TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.BRANCH, TxH.PLAIN);
                    case 0x02: return DecodeOperands("JL", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x03: return DecodeOperands("JG", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x04: return DecodeOperands("DEC_CHK", TxH.VAR, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x05: return DecodeOperands("INC_CHK", TxH.VAR, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x06: return DecodeOperands("JIN", TxH.OBJECT, TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x07: return DecodeOperands("TEST", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x08: return DecodeOperands("OR", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x09: return DecodeOperands("AND", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x0A: return DecodeOperands("TEST_ATTR", TxH.OBJECT, TxH.ATTRNUM, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x0B: return DecodeOperands("SET_ATTR", TxH.OBJECT, TxH.ATTRNUM, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0C: return DecodeOperands("CLEAR_ATTR", TxH.OBJECT, TxH.ATTRNUM, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0D: return DecodeOperands("STORE", TxH.VAR, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0E: return DecodeOperands("INSERT_OBJ", TxH.OBJECT, TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0F: return DecodeOperands("LOADW", TxH.LOW_ADDR, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x10: return DecodeOperands("LOADB", TxH.LOW_ADDR, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x11: return DecodeOperands("GET_PROP", TxH.OBJECT, TxH.PROPNUM, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x12: return DecodeOperands("GET_PROP_ADDR", TxH.OBJECT, TxH.PROPNUM, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x13: return DecodeOperands("GET_NEXT_PROP", TxH.OBJECT, TxH.PROPNUM, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x14: return DecodeOperands("ADD", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x15: return DecodeOperands("SUB", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x16: return DecodeOperands("MUL", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x17: return DecodeOperands("DIV", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x18: return DecodeOperands("MOD", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);

                    case 0x21: return DecodeOperands("STOREW", TxH.LOW_ADDR, TxH.NUMBER, TxH.ANYTHING, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x22: return DecodeOperands("STOREB", TxH.LOW_ADDR, TxH.NUMBER, TxH.ANYTHING, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x23: return DecodeOperands("PUT_PROP", TxH.OBJECT, TxH.NUMBER, TxH.ANYTHING, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x25: return DecodeOperands("PRINT_CHAR", TxH.PCHAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x26: return DecodeOperands("PRINT_NUM", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x27: return DecodeOperands("RANDOM", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x28: return DecodeOperands("PUSH", TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x2A: return DecodeOperands("SPLIT_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x2B: return DecodeOperands("SET_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x33: return DecodeOperands("OUTPUT_STREAM", TxH.PATTR, TxH.LOW_ADDR, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x34: return DecodeOperands("INPUT_STREAM", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x35: return DecodeOperands("SOUND_EFFECT", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.ROUTINE, TxH.NONE, TxH.PLAIN);

                    default:
                        switch (header.version)
                        {
                            case TxH.V1:
                            case TxH.V2:
                            case TxH.V3:
                                switch (code)
                                {
                                    case 0x20: return DecodeOperands("CALL", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);
                                    case 0x24: return DecodeOperands("READ", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x29: return DecodeOperands("PULL", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V4:
                                switch (code)
                                {
                                    case 0x19: return DecodeOperands("CALL_2S", TxH.ROUTINE, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.STORE, TxH.CALL);

                                    case 0x20: return DecodeOperands("CALL_VS", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);

                                    case 0x24: return DecodeOperands("READ", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.ROUTINE, TxH.NONE, TxH.PLAIN);

                                    case 0x29: return DecodeOperands("PULL", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x2C: return DecodeOperands("CALL_VS2", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);
                                    case 0x2D: return DecodeOperands("ERASE_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2E: return DecodeOperands("ERASE_LINE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2F: return DecodeOperands("SET_CURSOR", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x31: return DecodeOperands("SET_TEXT_STYLE", TxH.VATTR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x32: return DecodeOperands("BUFFER_MODE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x36: return DecodeOperands("READ_CHAR", TxH.NUMBER, TxH.NUMBER, TxH.ROUTINE, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    case 0x37: return DecodeOperands("SCAN_TABLE", TxH.ANYTHING, TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.BOTH, TxH.PLAIN);
                                }
                                break;
                            case TxH.V5:
                            case TxH.V7:
                            case TxH.V8:
                                switch (code)
                                {
                                    case 0x19: return DecodeOperands("CALL_2S", TxH.ROUTINE, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.STORE, TxH.CALL);
                                    case 0x1A: return DecodeOperands("CALL_2N", TxH.ROUTINE, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.NONE, TxH.CALL);
                                    case 0x1B: return DecodeOperands("SET_COLOUR", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x20: return DecodeOperands("CALL_VS", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);

                                    case 0x24: return DecodeOperands("READ", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.ROUTINE, TxH.STORE, TxH.PLAIN);

                                    case 0x29: return DecodeOperands("PULL", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x2C: return DecodeOperands("CALL_VS2", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);
                                    case 0x2D: return DecodeOperands("ERASE_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2E: return DecodeOperands("ERASE_LINE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2F: return DecodeOperands("SET_CURSOR", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x31: return DecodeOperands("SET_TEXT_STYLE", TxH.VATTR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x32: return DecodeOperands("BUFFER_MODE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x36: return DecodeOperands("READ_CHAR", TxH.NUMBER, TxH.NUMBER, TxH.ROUTINE, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    case 0x37: return DecodeOperands("SCAN_TABLE", TxH.ANYTHING, TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.BOTH, TxH.PLAIN);

                                    case 0x39: return DecodeOperands("CALL_VN", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.NONE, TxH.CALL);
                                    case 0x3A: return DecodeOperands("CALL_VN2", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.NONE, TxH.CALL);
                                    case 0x3B: return DecodeOperands("TOKENISE", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.NONE, TxH.PLAIN);
                                    case 0x3C: return DecodeOperands("ENCODE_TEXT", TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.LOW_ADDR, TxH.NONE, TxH.PLAIN);
                                    case 0x3D: return DecodeOperands("COPY_TABLE", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x3E: return DecodeOperands("PRINT_TABLE", TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NONE, TxH.PLAIN);
                                    case 0x3F: return DecodeOperands("CHECK_ARG_COUNT", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                                }
                                break;
                            case TxH.V6:
                                switch (code)
                                {
                                    case 0x19: return DecodeOperands("CALL_2S", TxH.ROUTINE, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.STORE, TxH.CALL);
                                    case 0x1A: return DecodeOperands("CALL_2N", TxH.ROUTINE, TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.NONE, TxH.CALL);
                                    case 0x1B: return DecodeOperands("SET_COLOUR", TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x1C: return DecodeOperands("THROW", TxH.ANYTHING, TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x20: return DecodeOperands("CALL_VS", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);

                                    case 0x24: return DecodeOperands("READ", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.ROUTINE, TxH.STORE, TxH.PLAIN);

                                    case 0x29: return DecodeOperands("PULL", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);

                                    case 0x2C: return DecodeOperands("CALL_VS2", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.STORE, TxH.CALL);
                                    case 0x2D: return DecodeOperands("ERASE_WINDOW", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2E: return DecodeOperands("ERASE_LINE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x2F: return DecodeOperands("SET_CURSOR", TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x30: return DecodeOperands("GET_CURSOR", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x31: return DecodeOperands("SET_TEXT_STYLE", TxH.VATTR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x32: return DecodeOperands("BUFFER_MODE", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x36: return DecodeOperands("READ_CHAR", TxH.NUMBER, TxH.NUMBER, TxH.ROUTINE, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    case 0x37: return DecodeOperands("SCAN_TABLE", TxH.ANYTHING, TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.BOTH, TxH.PLAIN);
                                    case 0x38: return DecodeOperands("NOT", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    case 0x39: return DecodeOperands("CALL_VN", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.NONE, TxH.CALL);
                                    case 0x3A: return DecodeOperands("CALL_VN2", TxH.ROUTINE, TxH.ANYTHING, TxH.ANYTHING, TxH.ANYTHING, TxH.NONE, TxH.CALL);
                                    case 0x3B: return DecodeOperands("TOKENISE", TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.LOW_ADDR, TxH.NUMBER, TxH.NONE, TxH.PLAIN);
                                    case 0x3C: return DecodeOperands("ENCODE_TEXT", TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.LOW_ADDR, TxH.NONE, TxH.PLAIN);
                                    case 0x3D: return DecodeOperands("COPY_TABLE", TxH.LOW_ADDR, TxH.NUMBER, TxH.LOW_ADDR, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                    case 0x3E: return DecodeOperands("PRINT_TABLE", TxH.LOW_ADDR, TxH.NUMBER, TxH.NUMBER, TxH.NUMBER, TxH.NONE, TxH.PLAIN);
                                    case 0x3F: return DecodeOperands("CHECK_ARG_COUNT", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                                }
                                break;
                        }
                        return DecodeOperands("ILLEGAL", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.ILLEGAL);
                }

            case TxH.ONE_OPERAND:
                code &= 0x0f;
                switch (code)
                {
                    case 0x00: return DecodeOperands("JZ", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                    case 0x01: return DecodeOperands("GET_SIBLING", TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BOTH, TxH.PLAIN);
                    case 0x02: return DecodeOperands("GET_CHILD", TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BOTH, TxH.PLAIN);
                    case 0x03: return DecodeOperands("GET_PARENT", TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x04: return DecodeOperands("GET_PROP_LEN", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                    case 0x05: return DecodeOperands("INC", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x06: return DecodeOperands("DEC", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x07: return DecodeOperands("PRINT_ADDR", TxH.LOW_ADDR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x09: return DecodeOperands("REMOVE_OBJ", TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0A: return DecodeOperands("PRINT_OBJ", TxH.OBJECT, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0B: return DecodeOperands("RET", TxH.ANYTHING, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);
                    case 0x0C: return DecodeOperands("JUMP", TxH.LABEL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);
                    case 0x0D: return DecodeOperands("PRINT_PADDR", TxH.STATIC, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x0E: return DecodeOperands("LOAD", TxH.VAR, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);

                    default:
                        switch (header.version)
                        {
                            case TxH.V1:
                            case TxH.V2:
                            case TxH.V3:
                                switch (code)
                                {
                                    case 0x0F: return DecodeOperands("NOT", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V4:
                                switch (code)
                                {
                                    case 0x08: return DecodeOperands("CALL_1S", TxH.ROUTINE, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.CALL);

                                    case 0x0F: return DecodeOperands("NOT", TxH.NUMBER, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V5:
                            case TxH.V6:
                            case TxH.V7:
                            case TxH.V8:
                                switch (code)
                                {
                                    case 0x08: return DecodeOperands(".CALL_1S", TxH.ROUTINE, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.CALL);

                                    case 0x0F: return DecodeOperands("CALL_1N", TxH.ROUTINE, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.CALL);
                                }
                                break;
                        }
                        return (DecodeOperands("ILLEGAL", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.ILLEGAL));
                }

            case TxH.ZERO_OPERAND:
                code &= 0x0f;
                switch (code)
                {
                    case 0x00: return DecodeOperands("RTRUE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);
                    case 0x01: return DecodeOperands("RFALSE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);
                    case 0x02: return DecodeOperands("PRINT", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.TEXT, TxH.PLAIN);
                    case 0x03: return DecodeOperands("PRINT_RET", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.TEXT, TxH.RETURN);
                    case 0x04: return DecodeOperands("NOP", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x07: return DecodeOperands("RESTART", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                    case 0x08: return DecodeOperands("RET_POPPED", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);

                    case 0x0A: return DecodeOperands("QUIT", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.RETURN);
                    case 0x0B: return DecodeOperands("NEW_LINE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                    case 0x0D: return DecodeOperands("VERIFY", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);

                    default:
                        switch (header.version)
                        {
                            case TxH.V1:
                            case TxH.V2:
                            case TxH.V3:
                                switch (code)
                                {
                                    case 0x05: return DecodeOperands("SAVE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                                    case 0x06: return DecodeOperands("RESTORE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);

                                    case 0x09: return DecodeOperands("POP", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x0C: return DecodeOperands("SHOW_STATUS", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V4:
                                switch (code)
                                {
                                    case 0x09: return DecodeOperands("POP", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);

                                    case 0x05: return DecodeOperands("SAVE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    case 0x06: return DecodeOperands("RESTORE", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V5:
                            case TxH.V7:
                            case TxH.V8:
                                switch (code)
                                {
                                    case 0x09: return DecodeOperands("CATCH", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);
                                    /* From a bug in Wishbringer V23 */
                                    case 0x0C: return DecodeOperands("SHOW_STATUS", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.PLAIN);
                                }
                                break;
                            case TxH.V6:
                                switch (code)
                                {
                                    case 0x09: return DecodeOperands("CATCH", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.STORE, TxH.PLAIN);

                                    case 0x0F: return DecodeOperands("PIRACY", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.BRANCH, TxH.PLAIN);
                                }
                                break;
                        }
                        return (DecodeOperands("ILLEGAL", TxH.NIL, TxH.NIL, TxH.NIL, TxH.NIL, TxH.NONE, TxH.ILLEGAL));
                }

            default:
                throw new NotImplementedException($"\nFatal: bad class ({opcode.OpClass})\n");
        }

    }/* decode_opcode */

    /* decode_operands - Decode operands of opcode */

    private static int DecodeOperands(string opcode_name, int par1, int par2, int par3, int par4, int extra, int type)
    {
        int len;
        int i, status;

        if (decode is null || opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        opcode.Par[0] = par1;
        opcode.Par[1] = par2;
        opcode.Par[2] = par3;
        opcode.Par[3] = par4;
        opcode.Extra = extra;
        opcode.Type = type;

        if (opcode.Type == TxH.ILLEGAL)
            return TxH.BAD_OPCODE;

        if (decode.FirstPass)
        {
            status = DecodeParameters(out _);
            if (status > 0)
                return TxH.BAD_OPCODE;
            status = DecodeExtra();
        }
        else
        {
            if (option_dump > 0)
                DumpOpcode(decode.Pc, opcode.OpCode, opcode.OpClass, opcode.Par, opcode.Extra);
            if (txio.option_inform)
            {
                len = opcode_name.Length;
                for (i = 0; i < len; i++)
                    txio.TxPrintf("{0}", char.ToLowerInvariant(opcode_name[i]));
            }
            else
            {
                txio.TxPrint(opcode_name);
                // len = strlen (opcode_name);
                len = opcode_name.Length;
            }
            for (; len < 16; len++)
                txio.TxPrint(" ");
            DecodeParameters(out int opers);
            if (opers > 0 && opcode.Extra != TxH.NONE)
                txio.TxPrint(" ");
            status = DecodeExtra();
            txio.TxPrint("\n");
        }
        if (decode.Pc > decode.HighPc)
            decode.HighPc = decode.Pc;

        return status;
    }/* decode_operands */

    /* decode_parameters - Decode input parameters */

    private static int DecodeParameters(out int opers)
    {
        int status, modes, addr_mode, maxopers;

        if (decode is null || opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        opers = 0;

        switch (opcode.OpClass)
        {
            case TxH.ONE_OPERAND:
                status = DecodeParameter((opcode.OpCode >> 4) & 0x03, 0);
                if (status > 0)
                    return (status);
                opers = 1;
                break;

            case TxH.TWO_OPERAND:
                status = DecodeParameter((opcode.OpCode & 0x40) > 0 ? TxH.VARIABLE : TxH.BYTE_IMMED, 0);
                if (status > 0)
                    return status;
                if (!decode.FirstPass)
                {
                    if (!txio.option_inform && opcode.Type == TxH.CALL)
                        txio.TxPrint(" (");
                    else
                        txio.TxPrintf("{0}", (txio.option_inform) ? ' ' : ',');
                }
                status = DecodeParameter((opcode.OpCode & 0x20) > 0 ? TxH.VARIABLE : TxH.BYTE_IMMED, 1);
                if (status > 0)
                    return (status);
                opers = 2;
                if (!txio.option_inform && !decode.FirstPass && opcode.Type == TxH.CALL && opers > 1)
                    txio.TxPrint(")");
                break;

            case TxH.VARIABLE_OPERAND:
            case TxH.EXTENDED_OPERAND:
                if ((opcode.OpCode & 0x3f) == 0x2c ||
                (opcode.OpCode & 0x3f) == 0x3a)
                {
                    modes = txio.ReadDataWord(ref decode.Pc);
                    maxopers = 8;
                }
                else
                {
                    modes = txio.ReadDataByte(ref decode.Pc);
                    maxopers = 4;
                }
                for (addr_mode = 0, opers = 0;
                 (addr_mode != TxH.NO_OPERAND) && maxopers > 0; maxopers--)
                {
                    addr_mode = (modes >> ((maxopers - 1) * 2)) & 0x03;
                    if (addr_mode != TxH.NO_OPERAND)
                    {
                        if (!decode.FirstPass && opers > 0)
                        {
                            if (!txio.option_inform && opcode.Type == TxH.CALL && opers == 1)
                                txio.TxPrint(" (");
                            else
                                txio.TxPrintf("{0}", (txio.option_inform) ? ' ' : ',');
                        }
                        status = DecodeParameter(addr_mode, opers);
                        if (status > 0)
                            return (status);
                        opers++;
                    }
                }
                if (!txio.option_inform && !decode.FirstPass && opcode.Type == TxH.CALL && opers > 1)
                    txio.TxPrint(")");
                break;

            case TxH.ZERO_OPERAND:
                break;

            default:
                throw new ArgumentException($"\nFatal: bad class ({opcode.OpClass})\n");
        }

        return (0);
    }/* decode_parameters */

    /* decode_parameter - Decode one input parameter */

    private static int DecodeParameter(int addr_mode, int opers)
    {
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        if (decode is null || opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        ulong addr;
        uint value = 0;
        int routine, vars, par, dictionary, s;

        par = (opers < 4) ? opcode.Par[opers] : TxH.ANYTHING;

        switch (addr_mode)
        {

            case TxH.WORD_IMMED:
                value = txio.ReadDataWord(ref decode.Pc);
                break;

            case TxH.BYTE_IMMED:
                value = txio.ReadDataByte(ref decode.Pc);
                break;

            case TxH.VARIABLE:
                value = txio.ReadDataByte(ref decode.Pc);
                par = TxH.VAR;
                break;

            case TxH.NO_OPERAND:
                return 0;

            default:
                throw new ArgumentException($"\nFatal: bad addressing mode ({addr_mode})\n");
        }

        /*
         * To make the code more readable, VAR type operands are not translated
         * as constants, eg. INC 5 is actually printed as INC L05. However, if
         * the VAR type operand _is_ a variable, the translation should look like
         * INC [L05], ie. increment the variable which is given by the contents
         * of local variable #5. Earlier versions of "txd" translated both cases
         * as INC L05. This bug was finally detected by Graham Nelson.
         */

        if (opers < 4 && opcode.Par[opers] == TxH.VAR)
            par = (addr_mode == TxH.VARIABLE) ? TxH.INDIRECT : TxH.VAR;

        switch (par)
        {

            case TxH.NIL:
                if (!decode.FirstPass)
                {
                    Console.Error.Write("\nWarning: Unexpected Parameter #{0} near {1:X5}\n", opers, decode.Pc);
                    PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }
                break;

            case TxH.ANYTHING:
                if (!decode.FirstPass)
                {
                    addr = txio.code_scaler * value + txio.story_scaler * header.strings_offset;
                    s = LookupString(addr);
                    if (s > 0)
                        txio.TxPrintf("{0}{1,3:d}", (txio.option_inform) ? 's' : 'S', s);
                    addr = value;
                    dictionary = InDictionary(addr);
                    if (dictionary > 0)
                    {
                        if (s > 0)
                            txio.TxPrintf(" {0} ", (txio.option_inform) ? "or" : "OR");
                        txio.TxPrint("\"");
                        txio.DecodeText(ref addr);
                        txio.TxPrint("\"");
                    }
                    if (dictionary == 0 && s == 0)
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }
                break;

            case TxH.VAR:
                if (!decode.FirstPass)
                {
                    if (value == 0)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "sp" : "(SP)+");
                    else
                        PrintVariable(value);
                }
                else
                {
                    if ((int)value > 0 && (int)value < 16 && (int)value > locals_count)
                        return (1);
                }
                break;

            case TxH.NUMBER:
                if (!decode.FirstPass)
                    PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                break;

            case TxH.PROPNUM:
                if (!decode.FirstPass)
                {
                    if (Symbols.PrintPropertyName(property_names_base, (int)value) == 0)
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }

                break;

            case TxH.ATTRNUM:
                if (!decode.FirstPass)
                {
                    if (Symbols.PrintAttributeName(attr_names_base, (int)value) == 0)
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }

                break;

            case TxH.LOW_ADDR:
                if (!decode.FirstPass)
                {
                    addr = value;
                    dictionary = InDictionary(addr);
                    if (dictionary > 0)
                    {
                        txio.TxPrint("\"");
                        txio.DecodeText(ref addr);
                        txio.TxPrint("\"");
                    }
                    else
                    {
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                    }
                }
                break;

            case TxH.ROUTINE:
                addr = txio.code_scaler * value + header.routines_offset * txio.story_scaler;
                if (!decode.FirstPass)
                {
                    if (option_labels > 0)
                    {
                        if (value != 0)
                        {
                            routine = LookupRoutine(addr, 0);
                            if (routine != 0)
                            {
                                txio.TxPrintf("{0}{1,4:d}", (txio.option_inform) ? 'r' : 'R', routine);
                            }
                            else
                            {
                                Console.Error.Write("\nWarning: found call to nonexistent routine!\n");
                                txio.TxPrintf("{0:X}", addr);
                            }
                        }
                        else
                        {
                            PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                        }
                    }
                    else
                    {
                        txio.TxPrintf("{0:X}", addr);
                    }
                }
                else
                {
                    if (addr < decode.LowAddress &&
                        addr >= code_base)
                    {
                        vars = txio.ReadDataByte(ref addr);
                        if (vars is >= 0 and <= 15)
                            decode.LowAddress = addr - 1;
                    }
                    if (addr > decode.HighAddress &&
                        addr < (ulong)txio.file_size)
                    {
                        vars = txio.ReadDataByte(ref addr);
                        if (vars is >= 0 and <= 15)
                            decode.HighAddress = addr - 1;
                    }
                }
                break;

            case TxH.OBJECT:
                if (!decode.FirstPass)
                {
                    // if (value == 0 || showobj.print_object_desc ((int)value) == 0) // TODO I don't like this section
                    if (value == 0)
                    {
                        ShowObj.PrintObjectDesc((int)value);
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                    }
                }
                break;

            case TxH.STATIC:
                if (!decode.FirstPass)
                {
                    addr = txio.code_scaler * value + txio.story_scaler * header.strings_offset;
                    s = LookupString(addr);
                    if (s != 0)
                    {
                        txio.TxPrintf("{0}{1,3:d}", (txio.option_inform) ? 's' : 'S', s);
                    }
                    else
                    {
#if !TXD_DEBUG
                        Console.Error.WriteLine("\nWarning: printing of nonexistent string\n");
#endif
                        txio.TxPrintf("{0:X}", addr);
                    }
                }
                break;

            case TxH.LABEL:
                addr = decode.Pc + (ulong)(short)value - 2; // TODO Check this math somehow
                if (decode.FirstPass && addr < decode.LowAddress)
                    return (1);
                if (option_labels > 0)
                {
                    if (decode.FirstPass)
                        AddLabel(addr);
                    else
                        txio.TxPrintf("{0}{1,4:d}", (txio.option_inform) ? 'l' : 'L', LookupLabel(addr, 1));
                }
                else
                {
                    if (!decode.FirstPass)
                        txio.TxPrintf("{0:X}", addr);
                }
                if (addr > decode.HighPc)
                    decode.HighPc = addr;
                break;

            case TxH.PCHAR:
                if (!decode.FirstPass)
                {
                    if (IsPrint((char)value))
                        txio.TxPrintf("\'{0}\'", (char)value);
                    else
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }

                break;

            case TxH.VATTR:
                if (!decode.FirstPass)
                {
                    if (value == TxH.ROMAN)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "roman" : "ROMAN");
                    else if (value == TxH.REVERSE)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "reverse" : "REVERSE");
                    else if (value == TxH.BOLDFACE)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "boldface" : "BOLDFACE");
                    else if (value == TxH.EMPHASIS)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "emphasis" : "EMPHASIS");
                    else if (value == TxH.FIXED_FONT)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "fixed_font" : "FIXED_FONT");
                    else
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }
                break;

            case TxH.PATTR:
                if (!decode.FirstPass)
                {
                    if ((int)value == 1)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "output_enable" : "OUTPUT_ENABLE");
                    else if ((int)value == 2)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "scripting_enable" : "SCRIPTING_ENABLE");
                    else if ((int)value == 3)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "redirect_enable" : "REDIRECT_ENABLE");
                    else if ((int)value == 4)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "record_enable" : "RECORD_ENABLE");
                    else if ((int)value == -1)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "output_disable" : "OUTPUT_DISABLE");
                    else if ((int)value == -2)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "scripting_disable" : "SCRIPTING_DISABLE");
                    else if ((int)value == -3)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "redirect_disable" : "REDIRECT_DISABLE");
                    else if ((int)value == -4)
                        txio.TxPrintf("{0}", (txio.option_inform) ? "record_disable" : "RECORD_DISABLE");
                    else
                        PrintInteger(value, addr_mode == TxH.BYTE_IMMED);
                }
                break;

            case TxH.INDIRECT:
                if (!decode.FirstPass)
                {
                    if (value == 0)
                    {
                        txio.TxPrintf("[{0}]", (txio.option_inform) ? "sp" : "(SP)+");
                    }
                    else
                    {
                        txio.TxPrint("[");
                        PrintVariable(value);
                        txio.TxPrint("]");
                    }
                }
                break;

            default:
                throw new ArgumentException($"\nFatal: bad operand type ({par})\n");
        }

        return (0);
    }/* decode_parameter */

    /* decode_extra - Decode branches, stores and text */

    private static int DecodeExtra()
    {
        ulong addr;
        zbyte_t firstbyte;

        if (decode is null || opcode is null)
            throw new InvalidOperationException("txd was not initialized");

        if (opcode.Extra == TxH.STORE || opcode.Extra == TxH.BOTH)
        {
            addr = txio.ReadDataByte(ref decode.Pc);
            if (!decode.FirstPass)
            {
                if (!txio.option_inform) // || (txio.option_inform >= 6))
                    txio.TxPrint("-> ");
                if (addr == 0)
                    txio.TxPrintf("{0}", (txio.option_inform) ? "sp" : "-(SP)");
                else
                    PrintVariable((uint)addr);

                if (opcode.Extra == TxH.BOTH)
                    txio.TxPrint(" ");
            }
            else
            {
                if (addr > 0 && addr < 16 && addr > (ulong)locals_count)
                    return (TxH.BAD_OPCODE);
            }
        }

        if (opcode.Extra == TxH.BRANCH || opcode.Extra == TxH.BOTH)
        {
            addr = firstbyte = txio.ReadDataByte(ref decode.Pc);
            addr &= 0x7f;
            if ((addr & 0x40) > 0)
            {
                addr &= 0x3f;
            }
            else
            {
                addr = (addr << 8) | txio.ReadDataByte(ref decode.Pc);
                if ((addr & 0x2000) > 0)
                {
                    addr &= 0x1fff;
                    unchecked
                    {
                        addr |= (ulong)~0x1fff; // TODO Is there a better way to handle this?
                    }
                }
            }
            if (!decode.FirstPass)
            {
                if ((addr > 1) && (firstbyte & 0x40) == 0 && (txio.option_inform) && (option_labels > 0)) // TODO Option_inform >= 6
                {
                    txio.TxPrint("?");  /* Inform 6 long branch */
                }
                if ((firstbyte & 0x80) > 0)
                    txio.TxPrintf("{0}", (txio.option_inform) ? "" : "[TRUE]");
                else
                    txio.TxPrintf("{0}", (txio.option_inform) ? "~" : "[FALSE]");
            }
            if (addr == 0)
            {
                if (!decode.FirstPass)
                    txio.TxPrintf("{0}", (txio.option_inform) ? "rfalse" : " RFALSE");
            }
            else if (addr == 1)
            {
                if (!decode.FirstPass)
                    txio.TxPrintf("{0}", (txio.option_inform) ? "rtrue" : " RTRUE");
            }
            else
            {
                addr = decode.Pc + addr - 2;
                if (decode.FirstPass && addr < start_of_routine)
                    return (TxH.BAD_OPCODE);
                if (option_labels > 0)
                {
                    if (decode.FirstPass)
                        AddLabel(addr);
                    else
                        txio.TxPrintf("{0}{1:d4}", (txio.option_inform) ? "l" : " L", LookupLabel(addr, 1));
                }
                else
                {
                    if (!decode.FirstPass)
                        txio.TxPrintf("{0}{1:X}", (txio.option_inform) ? "" : " ", addr);
                }
                if (addr > decode.HighPc)
                    decode.HighPc = addr;
            }
        }

        if (opcode.Extra == TxH.TEXT)
        {
            if (decode.FirstPass)
            {
                while ((short)txio.ReadDataWord(ref decode.Pc) >= 0) ;
            }
            else
            {
                PrintText(ref decode.Pc);
            }
        }

        if (opcode.Type == TxH.RETURN)
        {
            if (decode.Pc > decode.HighPc)
                return (TxH.END_OF_ROUTINE);
        }

        return (TxH.END_OF_INSTRUCTION);

    }/* decode_outputs */

    ///* decode_strings - Dump text after end of code */

    internal static void DecodeStrings(ulong pc)
    {
        int count = 1;

        pc = RoundData(pc);
        txio.TxPrint("\n[Start of text");
        if (option_labels == 0)
            txio.TxPrintf(" at {0:X}", pc);
        txio.TxPrint("]\n\n");
        while (pc < (ulong)txio.file_size)
        {
            if (option_labels > 0)
                txio.TxPrintf("{0}{1:d3}: ", (txio.option_inform) ? 's' : 'S', count++);
            else
                txio.TxPrintf("{0:X}: S{1:d3} ", pc, count++);
            PrintText(ref pc);
            txio.TxPrint("\n");
            pc = RoundCode(pc);
        }
        txio.TxPrint("\n[End of text");
        if (option_labels == 0)
            txio.TxPrintf(" at %lx", pc);
        txio.TxPrint("]\n\n[End of file]\n");

    }/* decode_strings */

    /* scan_strings - build string address table */

    internal static void ScanStrings(ulong pc)
    {
        int count = 1;
        zword_t data;

        strings_base = new System.Collections.Generic.List<TxH.CRefItemT>();

        pc = RoundData(pc);
        ulong old_pc = pc;
        while (pc < (ulong)txio.file_size)
        {
            var cref_item = new TxH.CRefItemT
            {
                Address = pc,
                Number = count++
            };
            strings_base.Insert(0, cref_item);
            old_pc = pc;
            do
                data = txio.ReadDataWord(ref pc);
            while (pc < (ulong)txio.file_size && ((uint)data & 0x8000) == 0);
            pc = RoundCode(pc);
            if ((uint)(data & 0x8000) > 0)
                old_pc = pc;
        }
        txio.file_size = (int)old_pc;

    }/* scan_strings */

    /* lookup_string - lookup a string address */

    internal static int LookupString(ulong addr)
    {
        if (decode is null)
            throw new InvalidOperationException("txd was not initialized");

        if (addr <= decode.HighAddress || addr >= (ulong)txio.file_size)
            return 0;

        for (int i = 0; i < strings_base?.Count; i++)
        {
            if (strings_base[i].Address == addr)
                return strings_base[i].Number;
        }

        return 0;

    }/* lookup_string */

    private static void LookupVerb(ulong addr)
    {
        ulong address, routine;
        uint i;

        bool first = true;

        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        var header = txio.header;

        address = action_table_base;
        for (i = 0; i < action_count; i++)
        {
            routine = txio.ReadDataWord(ref address) * txio.code_scaler + txio.story_scaler * header.routines_offset;
            if (routine == addr)
            {
                if (first)
                {
                    txio.TxPrint("    Action routine for:\n");
                    txio.TxPrint("        ");
                    txio.TxFixMargin(1);
                    first = false;
                }
                ShowVerb.ShowSyntaxOfAction(i,
                    verb_table_base,
                    verb_count,
                    parser_type,
                    prep_type,
                    prep_table_base,
                    attr_names_base);
            }
        }
        txio.TxFixMargin(0);

        first = true;
        address = preact_table_base;
        if (parser_type >= (int)TxH.ParserTypes.InformGV2)
        {
            if (ShowVerb.IsGv2ParsingRoutine(addr, verb_table_base,
                           verb_count))
            {
                txio.TxPrint("    Parsing routine for:\n");
                txio.TxPrint("        ");
                txio.TxFixMargin(1);
                first = false;
                ShowVerb.ShowSyntaxOfParsingRoutine(addr,
                                verb_table_base,
                                verb_count,
                                parser_type,
                                prep_type,
                                prep_table_base,
                                attr_names_base);
            }
        }
        else if (parser_type >= (int)TxH.ParserTypes.Inform5Grammar)
        {
            for (i = 0; i < parse_count; i++)
            {
                routine = txio.ReadDataWord(ref address) * txio.code_scaler + txio.story_scaler * header.routines_offset;
                if (routine == addr)
                {
                    if (first)
                    {
                        txio.TxPrint("    Parsing routine for:\n");
                        txio.TxPrint("        ");
                        txio.TxFixMargin(1);
                        first = false;
                    }
                    ShowVerb.ShowSyntaxOfParsingRoutine(i,
                              verb_table_base,
                              verb_count,
                              parser_type,
                              prep_type,
                              prep_table_base,
                              attr_names_base);
                }
            }
        }
        else
        {
            for (i = 0; i < action_count; i++)
            {
                routine = txio.ReadDataWord(ref address) * txio.code_scaler + txio.story_scaler * header.routines_offset;
                if (routine == addr)
                {
                    if (first)
                    {
                        txio.TxPrint("    Pre-action routine for:\n");
                        txio.TxPrint("        ");
                        txio.TxFixMargin(1);
                        first = false;
                    }
                    ShowVerb.ShowSyntaxOfAction(i,
                              verb_table_base,
                              verb_count,
                              parser_type,
                              prep_type,
                              prep_table_base,
                              attr_names_base);
                }
            }
        }
        txio.TxFixMargin(0);

    }/* lookup_verb */

    internal static void SetupDictionary()
    {
        if (txio.header is null)
            throw new InvalidOperationException("txio header was not initialized");
        dict_start = txio.header.dictionary;
        ulong temp = txio.ReadDataByte(ref dict_start);
        dict_start += temp;
        word_size = txio.ReadDataByte(ref dict_start);
        word_count = txio.ReadDataWord(ref dict_start);
        dict_end = dict_start + (word_count * word_size);

    }/* setup_dictionary */

    internal static int InDictionary(ulong word_address)
    {
        if (word_address < dict_start || word_address > dict_end)
            return 0;

        if ((word_address - dict_start) % word_size == 0)
            return 1;

        return 0;

    }/* in_dictionary */

    internal static void AddLabel(ulong addr)
    {
        //    cref_item_t *cref_item, **prev_item, *next_item;

        if (current_routine == null)
            return;

        var child = new TxH.CRefItemT
        {
            Address = addr,
            Number = 0
        };
        current_routine.Child.Add(child);

        //    prev_item = &current_routine->child;
        //    next_item = current_routine->child;
        //    while (next_item != NULL && next_item->address < addr) {
        //    prev_item = &(next_item->next);
        //    next_item = next_item->next;
        //    }

        //    if (next_item == NULL || next_item->address != addr) {
        //    cref_item = (cref_item_t *) malloc (sizeof (cref_item_t));
        //    if (cref_item == NULL) {
        //        (void) fprintf (stderr, "\nFatal: insufficient memory\n");
        //        exit (EXIT_FAILURE);
        //    }
        //    cref_item->next = next_item;
        //    *prev_item = cref_item;
        //    cref_item->child = NULL;
        //    cref_item->address = addr;
        //    cref_item->number = 0;
        //    }

        // add_routine(addr); // TODO I'm not sure this is correct

    }/* add_label */

    private static void AddRoutine(ulong addr)
    {
        //    cref_item_t *cref_item, **prev_item, *next_item;

        //    prev_item = &routines_base;
        //    next_item = routines_base;
        //    while (next_item != NULL && next_item->address < addr) {
        //    prev_item = &(next_item->next);
        //    next_item = next_item->next;
        //    }

        //    if (next_item == NULL || next_item->address != addr) {
        //    cref_item = (cref_item_t *) malloc (sizeof (cref_item_t));
        //    if (cref_item == NULL) {
        //        (void) fprintf (stderr, "\nFatal: insufficient memory\n");
        //        exit (EXIT_FAILURE);
        //    }
        //    cref_item->next = next_item;
        //    *prev_item = cref_item;
        //    cref_item->child = NULL;
        //    cref_item->address = addr;
        //    cref_item->number = 0;
        //    } else
        //    cref_item = next_item;

        //    current_routine = cref_item;

        if (routines_base == null)
        {
            routines_base = new System.Collections.Generic.List<TxH.CRefItemT>();
        }

        var cref_item = new TxH.CRefItemT
        {
            Address = addr,
            Number = 0
        };

        routines_base.Insert(0, cref_item);
        current_routine = cref_item;
    }/* add_routine */

    private static int LookupLabel(ulong addr, int flag)
    {
        if (current_routine == null || routines_base == null)
            throw new InvalidOperationException("Not properly initialized.");

        //cref_item_t *cref_item = current_routine->child;
        //int label;

        //while (cref_item != NULL && cref_item->address != addr)
        //cref_item = cref_item->next;

        //if (cref_item == NULL) {
        //label = 0;
        //if (flag) {
        //    (void) fprintf (stderr, "\nFatal: cannot find label!\n");
        //    exit (EXIT_FAILURE);
        //}
        //} else
        //label = cref_item->number;

        // return (label);

        for (int i = 0; i < current_routine.Child.Count; i++)
        {
            if (current_routine.Child[i].Address == addr)
                return routines_base[i].Number;
        }

        if (flag == 1)
        {
            Console.Error.Write("\nFatal: cannot find label!\n");
            throw new ArgumentException($"Can't find label for addr: {+addr}");
        }
        else
        {
            return 0;
        }
        throw new NotImplementedException();

    }/* lookup_label */

    private static int LookupRoutine(ulong addr, int flag)
    {
        if (current_routine == null)
            throw new InvalidOperationException("Not properly initialized.");

        //    cref_item_t *cref_item = routines_base;

        //    while (cref_item != NULL && cref_item->address != addr)
        //    cref_item = cref_item->next;

        //    if (cref_item == NULL) {
        //    if (flag) {
        //        (void) fprintf (stderr, "\nFatal: cannot find routine!\n");
        //        exit (EXIT_FAILURE);
        //    } else
        //        return (0);
        //    }

        //    if (flag)
        //    current_routine = cref_item;

        //    return (cref_item->number);

        for (int i = 0; i < routines_base?.Count; i++)
        {
            if (routines_base[i].Address == addr)
            {
                if (flag == 1)
                {
                    current_routine = routines_base[i];
                }
                return routines_base[i].Number;
            }
        }
        if (flag == 1)
        {
            throw new ArgumentException("\nFatal: Cannot find routine!\n");
        }
        else
        {
            return 0;
        }
    }/* lookup_routine */

    internal static void RenumberCref(System.Collections.Generic.List<TxH.CRefItemT> items)
    {
        int number = 1;

        for (int i = 0; i < items.Count; i++)
        {
            var cref_item = items[i];
            if (start_data_pc == 0 ||
               cref_item.Address < start_data_pc ||
               cref_item.Address >= end_data_pc)
            {
                cref_item.Number = number++;
            }

            RenumberCref(cref_item.Child);
        }

    }/* renumber_cref */



    //#ifdef __STDC__
    //static int print_object_desc (uint obj)
    //#else
    //static int print_object_desc (obj)
    //uint obj;
    //#endif
    //{
    //    ulong address;

    //    address = (ulong) header.objects;
    //    if ((uint) header.version < V4)
    //    address += ((P3_MAX_PROPERTIES - 1) * 2) + ((obj - 1) * O3_SIZE) + O3_PROPERTY_OFFSET;
    //    else
    //    address += ((P4_MAX_PROPERTIES - 1) * 2) + ((obj - 1) * O4_SIZE) + O4_PROPERTY_OFFSET;

    //    address = (ulong) txio.read_data_word (&address);
    //    if ((uint) txio.read_data_byte (&address)) {
    //    txio.tx_printf ("\"");
    //    (void) txio.decode_text (&address);
    //    txio.tx_printf ("\"");
    //    } else
    //    obj = 0;

    //    return (obj);

    //}/* print_object_desc */

    internal static void PrintText(ref ulong addr)
    {
        txio.TxPrint("\"");
        txio.DecodeText(ref addr);
        txio.TxPrint("\"");
    }/* print_text */

    private static void PrintInteger(uint value, bool flag)
    {
        if (flag)
            txio.TxPrintf("#{0:X2}", value);
        else
            txio.TxPrintf("#{0:X4}", value);
    }

    internal static void DumpData(ulong start_addr, ulong end_addr) =>

        //    int i, c;
        //    ulong addr, save_addr, tx_h.LOW_ADDR, high_addr;

        //    tx_h.LOW_ADDR = start_addr & ~15;
        //    high_addr = (end_addr + 15) & ~15;

        //    for (addr = tx_h.LOW_ADDR; addr < high_addr; ) {
        //    txio.tx_printf ("%5lx: ", (ulong) addr);
        //    save_addr = addr;
        //    for (i = 0; i < 16; i++) {
        //        if (addr < start_addr || addr > end_addr) {
        //        txio.tx_printf ("   ");
        //        addr++;
        //        } else
        //        txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //    }
        //    addr = save_addr;
        //    for (i = 0; i < 16; i++) {
        //        if (addr < start_addr || addr > end_addr) {
        //        txio.tx_printf (" ");
        //        addr++;
        //        } else {
        //        c = txio.read_data_byte (&addr);
        //        txio.tx_printf ("%c", (char) ((isprint (c)) ? c : '.'));
        //        }
        //    }
        //    txio.tx_printf ("\n");
        //    }
        throw new NotImplementedException();/* dump_data */

    private static void DumpOpcode(ulong addr, int op, int opclass, int[] par, int extra) =>
        //    int opers, modes, addr_mode, maxopers, count;
        //    unsigned char t;
        //    ulong save_addr;

        //    count = 0;

        //    addr--;
        //    if (class == EXTENDED_OPERAND) {
        //    addr--;
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //    count++;
        //    }
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //    count++;

        //    if (class == ONE_OPERAND)
        //    dump_operand (&addr, (op >> 4) & 0x03, 0, par, &count);

        //    if (class == TWO_OPERAND) {
        //    dump_operand (&addr, (op & 0x40) ? VARIABLE : BYTE_IMMED, 0, par, &count);
        //    dump_operand (&addr, (op & 0x20) ? VARIABLE : BYTE_IMMED, 1, par, &count);
        //    }

        //    if (class == VARIABLE_OPERAND || class == EXTENDED_OPERAND) {
        //    if ((op & 0x3f) == 0x2c || (op & 0x3f) == 0x3a) {
        //        save_addr = addr;
        //        modes = txio.read_data_word (&addr);
        //        addr = save_addr;
        //        txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //        txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //        count += 2;
        //        maxopers = 8;
        //    } else {
        //        save_addr = addr;
        //        modes = txio.read_data_byte (&addr);
        //        addr = save_addr;
        //        txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //        count++;
        //        maxopers = 4;
        //    }
        //    for (addr_mode = 0, opers = 0; (addr_mode != NO_OPERAND) && maxopers; maxopers--) {
        //        addr_mode = (modes >> ((maxopers - 1) * 2)) & 0x03;
        //        if (addr_mode != NO_OPERAND) {
        //        dump_operand (&addr, addr_mode, opers, par, &count);
        //        opers++;
        //        }
        //    }
        //    }

        //    if (extra == TEXT) {
        //    txio.tx_printf ("...");
        //    count++;
        //    }

        //    if (extra == STORE || extra == BOTH) {
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //    count++;
        //    }

        //    if (extra == BRANCH || extra == BOTH) {
        //    t = (unsigned char) txio.read_data_byte (&addr);
        //    txio.tx_printf ("%02x ", (uint) t);
        //    count++;
        //    if (((uint) t & 0x40) == 0) {
        //        txio.tx_printf ("%02x ", (uint) txio.read_data_byte (&addr));
        //        count++;
        //    }
        //    }

        //    if (count > 8)
        //    txio.tx_printf ("\n                               ");
        //    else
        //    for (; count < 8; count++)
        //        txio.tx_printf ("   ");

        throw new NotImplementedException();/* dump_opcode */

    private static void DumpOperand(ulong addr, int addr_mode, int opers, int[] par, out int count) =>
        //    if (opers < 4 && par[opers] == VAR)
        //    addr_mode = VARIABLE;

        //    if (addr_mode == WORD_IMMED) {
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (addr));
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (addr));
        //    *count += 2;
        //    }

        //    if (addr_mode == BYTE_IMMED || addr_mode == VARIABLE) {
        //    txio.tx_printf ("%02x ", (uint) txio.read_data_byte (addr));
        //    (*count)++;
        //    }

        throw new NotImplementedException();/* dump_operand */

    private static void PrintVariable(uint varnum)
    {
        if (varnum < 16)
        {
            if (option_symbols > 0 && Symbols.PrintLocalName(start_of_routine, (int)(varnum - 1)) > 0) /* null */
            { }
            else if (txio.option_inform)
            {
                txio.TxPrintf("local{0}", varnum - 1);
            }
            else
            {
                txio.TxPrintf("L{0:X2}", varnum - 1);
            }
        }
        else
            if (option_symbols > 0 && Symbols.PrintGlobalName(start_of_routine, (int)(varnum - 16)) > 0) /* null */{ }
        else
        {
            txio.TxPrintf("{0}{1:X2}", txio.option_inform ? 'g' : 'G', varnum - 16);
        }
    }

    private static bool IsPrint(char _) => true;
}
