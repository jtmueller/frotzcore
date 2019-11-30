/* fastmem.c - Memory related functions (fast version without virtual memory)
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

/*
 * New undo mechanism added by Jim Dunleavy <jim.dunleavy@erha.ie>
 */

using Collections.Pooled;
using Frotz.Constants;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal readonly struct RecordStruct
    {
        public readonly Story StoryId;
        public readonly zword Release;
        public readonly string Serial;

        public RecordStruct(Story story_id, zword release, string serial)
        {
            StoryId = story_id;
            Release = release;
            Serial = serial;
        }
    }

    internal static class FastMem
    {
        private static readonly RecordStruct[] Records = {
            new RecordStruct(Story.SHERLOCK, 97, "871026"),
            new RecordStruct(Story.SHERLOCK,  21, "871214"),
            new RecordStruct(Story.SHERLOCK,  22, "880112"),
            new RecordStruct(Story.SHERLOCK,  26, "880127"),
            new RecordStruct(Story.SHERLOCK,   4, "880324"),
            new RecordStruct(Story.BEYOND_ZORK,   1, "870412"),
            new RecordStruct(Story.BEYOND_ZORK,   1, "870715"),
            new RecordStruct(Story.BEYOND_ZORK,  47, "870915"),
            new RecordStruct(Story.BEYOND_ZORK,  49, "870917"),
            new RecordStruct(Story.BEYOND_ZORK,  51, "870923"),
            new RecordStruct(Story.BEYOND_ZORK,  57, "871221"),
            new RecordStruct(Story.BEYOND_ZORK,  60, "880610"),
            new RecordStruct(Story.ZORK_ZERO,   0, "870831"),
            new RecordStruct(Story.ZORK_ZERO,  96, "880224"),
            new RecordStruct(Story.ZORK_ZERO, 153, "880510"),
            new RecordStruct(Story.ZORK_ZERO, 242, "880830"),
            new RecordStruct(Story.ZORK_ZERO, 242, "880901"),
            new RecordStruct(Story.ZORK_ZERO, 296, "881019"),
            new RecordStruct(Story.ZORK_ZERO, 366, "890323"),
            new RecordStruct(Story.ZORK_ZERO, 383, "890602"),
            new RecordStruct(Story.ZORK_ZERO, 387, "890612"),
            new RecordStruct(Story.ZORK_ZERO, 392, "890714"),
            new RecordStruct(Story.ZORK_ZERO, 393, "890714"),
            new RecordStruct(Story.SHOGUN, 295, "890321"),
            new RecordStruct(Story.SHOGUN, 292, "890314"),
            new RecordStruct(Story.SHOGUN, 311, "890510"),
            new RecordStruct(Story.SHOGUN, 320, "890627"),
            new RecordStruct(Story.SHOGUN, 321, "890629"),
            new RecordStruct(Story.SHOGUN, 322, "890706"),
            new RecordStruct(Story.ARTHUR,  40, "890502"),
            new RecordStruct(Story.ARTHUR,  41, "890504"),
            new RecordStruct(Story.ARTHUR,  54, "890606"),
            new RecordStruct(Story.ARTHUR,  63, "890622"),
            new RecordStruct(Story.ARTHUR,  74, "890714"),
            new RecordStruct(Story.JOURNEY,  46, "880603"),
            new RecordStruct(Story.JOURNEY,   2, "890303"),
            new RecordStruct(Story.JOURNEY,  26, "890316"),
            new RecordStruct(Story.JOURNEY,  30, "890322"),
            new RecordStruct(Story.JOURNEY,  51, "890522"),
            new RecordStruct(Story.JOURNEY,  54, "890526"),
            new RecordStruct(Story.JOURNEY,  77, "890616"),
            new RecordStruct(Story.JOURNEY,  79, "890627"),
            new RecordStruct(Story.JOURNEY,  83, "890706"),
            new RecordStruct(Story.LURKING_HORROR, 203, "870506"),
            new RecordStruct(Story.LURKING_HORROR, 219, "870912"),
            new RecordStruct(Story.LURKING_HORROR, 221, "870918"),
            new RecordStruct(Story.AMFV,  47, "850313"),
            new RecordStruct(Story.UNKNOWN,   0, "------")
        };

        internal static string SaveName = General.DEFAULT_SAVE_NAME;
        internal static string AuxilaryName = General.DEFAULT_AUXILARY_NAME;

        internal static zbyte[] ZMData = Array.Empty<zbyte>();
        internal static zword ZMData_checksum = 0;

        internal static long Zmp = 0;
        internal static long Pcp = 0;

        private static System.IO.MemoryStream? StoryFp = null;
        private static bool FirstRestart = true;
        private static long InitFpPos = 0;

        #region zmp & pcp

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte Lo(zword v) => (byte)(v & 0xff);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte Hi(zword v) => (byte)(v >> 8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetWord(long addr, zword v)
        {
            ZMData[addr] = Hi(v);
            ZMData[addr + 1] = Lo(v);

            DebugState.Output("ZMP: {0} -> {1}", addr, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LowWord(long addr, out zbyte v)
            => v = (byte)((ZMData[addr] << 8) | ZMData[addr + 1]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LowWord(long addr, out zword v)
            => v = (ushort)((ZMData[addr] << 8) | ZMData[addr + 1]);

        // TODO I'm suprised that they return the same thing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void HighWord(long addr, out zword v)
            => LowWord(addr, out v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CodeWord(out zword v)
        {
            v = (zword)(ZMData[Pcp] << 8 | ZMData[Pcp + 1]);
            Pcp += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetByte(long addr, byte v)
        {
            ZMData[addr] = v;
            DebugState.Output("ZMP: {0} -> {1}", addr, v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CodeByte(out zbyte v) => v = ZMData[Pcp++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LowByte(long addr, out zbyte v) => v = ZMData[addr];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void GetPc(out long v) => v = Pcp - Zmp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetPc(long v) => Pcp = Zmp + v;
        #endregion

        /*
         * Data for the undo mechanism.
         * This undo mechanism is based on the scheme used in Evin Robertson's
         * Nitfol interpreter.
         * Undo blocks are stored as differences between states.
         */

        //typedef struct undo_struct undo_t;
        internal readonly struct UndoStruct
        {
            public UndoStruct(long pc, long diffSize, zword frameCount, zword stackSize,
                zword frameOffset, long sp, ReadOnlyMemory<zword> stack, ReadOnlyMemory<byte> undoData)
            {
                Pc = pc; DiffSize = diffSize; FrameCount = frameCount; StackSize = stackSize;
                FrameOffset = frameOffset; Sp = sp; Stack = stack; UndoData = undoData;
            }

            public readonly long Pc;
            public readonly long DiffSize;
            public readonly zword FrameCount;
            public readonly zword StackSize;
            public readonly zword FrameOffset;
            /* undo diff and stack data follow */

            public readonly long Sp;
            public readonly ReadOnlyMemory<zword> Stack;
            public readonly ReadOnlyMemory<byte> UndoData;
        }

        // static undo_struct first_undo = null, last_undo = null, curr_undo = null;
        //static zbyte *undo_mem = NULL, *prev_zmp, *undo_diff;

        private static IMemoryOwner<zbyte>? zmpHandle;
        private static IMemoryOwner<zbyte>? diffHandle;
        private static Memory<zbyte> PrevZmp = Memory<zbyte>.Empty;
        private static Memory<zbyte> UndoDiff = Memory<zbyte>.Empty;
        private static readonly PooledList<UndoStruct> UndoMem = new PooledList<UndoStruct>();
        private static int UndoCount = 0;

        /*
         * get_header_extension
         *
         * Read a value from the header extension (former mouse table).
         *
         */

        internal static zword GetHeaderExtension(int entry)
        {
            if (Main.h_extension_table == 0 || entry > Main.hx_table_size)
                return 0;

            zword addr = (zword)(Main.h_extension_table + 2 * entry);
            LowWord(addr, out zword val);

            return val;

        }/* get_header_extension */

        /*
         * set_header_extension
         *
         * Set an entry in the header extension (former mouse table).
         *
         */

        internal static void SetHeaderExtension(int entry, zword val)
        {
            zword addr;

            if (Main.h_extension_table == 0 || entry > Main.hx_table_size)
                return;

            addr = (zword)(Main.h_extension_table + 2 * entry);
            SetWord(addr, val);

        }/* set_header_extension */

        /*
         * restart_header
         *
         * Set all header fields which hold information about the interpreter.
         *
         */
        internal static void RestartHeader()
        {
            zword screen_x_size;
            zword screen_y_size;
            zbyte font_x_size;
            zbyte font_y_size;

            int i;

            SetByte(ZMachine.H_CONFIG, Main.h_config);
            SetWord(ZMachine.H_FLAGS, Main.h_flags);

            if (Main.h_version >= ZMachine.V4)
            {
                SetByte(ZMachine.H_INTERPRETER_NUMBER, Main.h_interpreter_number);
                SetByte(ZMachine.H_INTERPRETER_VERSION, Main.h_interpreter_version);
                SetByte(ZMachine.H_SCREEN_ROWS, Main.h_screen_rows);
                SetByte(ZMachine.H_SCREEN_COLS, Main.h_screen_cols);
            }

            /* It's less trouble to use font size 1x1 for V5 games, especially
               because of a bug in the unreleased German version of "Zork 1" */

            if (Main.h_version != ZMachine.V6)
            {
                screen_x_size = Main.h_screen_cols;
                screen_y_size = Main.h_screen_rows;
                font_x_size = 1;
                font_y_size = 1;
            }
            else
            {
                screen_x_size = Main.h_screen_width;
                screen_y_size = Main.h_screen_height;
                font_x_size = Main.h_font_width;
                font_y_size = Main.h_font_height;
            }

            if (Main.h_version >= ZMachine.V5)
            {
                SetWord(ZMachine.H_SCREEN_WIDTH, screen_x_size);
                SetWord(ZMachine.H_SCREEN_HEIGHT, screen_y_size);
                SetByte(ZMachine.H_FONT_HEIGHT, font_y_size);
                SetByte(ZMachine.H_FONT_WIDTH, font_x_size);
                SetByte(ZMachine.H_DEFAULT_BACKGROUND, Main.h_default_background);
                SetByte(ZMachine.H_DEFAULT_FOREGROUND, Main.h_default_foreground);
            }

            if ((Main.h_version >= ZMachine.V3) && (Main.h_user_name[0] != 0))
            {
                for (i = 0; i < 8; i++)
                {
                    StoreB((zword)(ZMachine.H_USER_NAME + i), Main.h_user_name[i]);
                }
            }
            SetByte(ZMachine.H_STANDARD_HIGH, Main.h_standard_high);
            SetByte(ZMachine.H_STANDARD_LOW, Main.h_standard_low);

            SetHeaderExtension(ZMachine.HX_FLAGS, Main.hx_flags);
            SetHeaderExtension(ZMachine.HX_FORE_COLOUR, Main.hx_fore_colour);
            SetHeaderExtension(ZMachine.HX_BACK_COLOUR, Main.hx_back_colour);
        }/* restart_header */

        /*
         * init_memory
         *
         * Allocate memory and load the story file.
         *
         */

        internal static void InitMemory()
        {
            long size;
            zword addr;
            zword n;
            int i, j;

            if (Main.StoryData == null || Main.StoryName == null)
                throw new InvalidOperationException("Story not initialized.");

            StoryFp?.Dispose();
            StoryFp = OS.PathOpen(Main.StoryData);
            InitFpPos = StoryFp.Position;

            DebugState.Output("Starting story: {0}", Main.StoryName);

            /* Allocate memory for story header */

            ZMData = new byte[64];
            //Frotz.Other.ZMath.clearArray(ZMData);

            /* Load header into memory */
            if (StoryFp.Read(ZMData, 0, 64) != 64)
            {
                OS.Fatal("Story file read error");
            }

            /* Copy header fields to global variables */
            LowByte(ZMachine.H_VERSION, out Main.h_version);

            if (Main.h_version < ZMachine.V1 || Main.h_version > ZMachine.V8)
            {
                OS.Fatal("Unknown Z-code version");
            }

            LowByte(ZMachine.H_CONFIG, out Main.h_config);
            if (Main.h_version == ZMachine.V3 && ((Main.h_config & ZMachine.CONFIG_BYTE_SWAPPED) != 0))
            {
                OS.Fatal("Byte swapped story file");
            }

            LowWord(ZMachine.H_RELEASE, out Main.h_release);
            LowWord(ZMachine.H_RESIDENT_SIZE, out Main.h_resident_size);
            LowWord(ZMachine.H_START_PC, out Main.h_start_pc);
            LowWord(ZMachine.H_DICTIONARY, out Main.h_dictionary);
            LowWord(ZMachine.H_OBJECTS, out Main.h_objects);
            LowWord(ZMachine.H_GLOBALS, out Main.h_globals);
            LowWord(ZMachine.H_DYNAMIC_SIZE, out Main.h_dynamic_size);
            LowWord(ZMachine.H_FLAGS, out Main.h_flags);

            for (i = 0, addr = ZMachine.H_SERIAL; i < 6; i++, addr++)
            {
                LowByte(addr, out Main.h_serial[i]);
            }
            // TODO serial might need to be a char

            /* Auto-detect buggy story files that need special fixes */

            Main.StoryId = Story.UNKNOWN;

            for (i = 0; Records[i].StoryId != Story.UNKNOWN; i++)
            {

                if (Main.h_release == Records[i].Release)
                {

                    for (j = 0; j < 6; j++)
                    {
                        if (Main.h_serial[j] != Records[i].Serial[j])
                            goto no_match;
                    }

                    Main.StoryId = Records[i].StoryId;

                }

            no_match:; /* null statement */

            }

            LowWord(ZMachine.H_ABBREVIATIONS, out Main.h_abbreviations);
            LowWord(ZMachine.H_FILE_SIZE, out Main.h_file_size);

            /* Calculate story file size in bytes */
            if (Main.h_file_size != 0)
            {
                Main.StorySize = 2 * Main.h_file_size;

                if (Main.h_version >= ZMachine.V4)
                {
                    Main.StorySize *= 2;
                }

                if (Main.h_version >= ZMachine.V6)
                {
                    Main.StorySize *= 2;
                }

                if (Main.StoryId == Story.AMFV && Main.h_release == 47)
                {
                    Main.StorySize = 2 * Main.h_file_size;
                }
                else if (Main.StorySize > 0)
                {/* os_path_open() set the size */
                }
                else
                {/* some old games lack the file size entry */
                    Main.StorySize = StoryFp.Length - InitFpPos;
                    StoryFp.Position = InitFpPos + 64;
                }

                LowWord(ZMachine.H_CHECKSUM, out Main.h_checksum);
                LowWord(ZMachine.H_ALPHABET, out Main.h_alphabet);
                LowWord(ZMachine.H_FUNCTIONS_OFFSET, out Main.h_functions_offset);
                LowWord(ZMachine.H_STRINGS_OFFSET, out Main.h_strings_offset);
                LowWord(ZMachine.H_TERMINATING_KEYS, out Main.h_terminating_keys);
                LowWord(ZMachine.H_EXTENSION_TABLE, out Main.h_extension_table);

                /* Zork Zero beta and Macintosh versions don't have the graphics flag set */

                if (Main.StoryId == Story.ZORK_ZERO)
                {
                    if (Main.h_release == 96 || Main.h_release == 153 ||
                        Main.h_release == 242 || Main.h_release == 296)
                    {
                        Main.h_flags |= ZMachine.GRAPHICS_FLAG;
                    }
                }

                /* Adjust opcode tables */

                if (Main.h_version <= ZMachine.V4)
                {
                    Process.op0_opcodes[0x09] = Variable.ZPop;
                    Process.op0_opcodes[0x0f] = Math.ZNot;
                }
                else
                {
                    Process.op0_opcodes[0x09] = Process.ZCatch;
                    Process.op0_opcodes[0x0f] = Process.ZCallN;
                }

                /* Allocate memory for story data */
                var len = ZMData.Length;
                if (len < Main.StorySize)
                {
                    byte[] temp = ArrayPool<byte>.Shared.Rent(len);
                    try
                    {
                        Array.Copy(ZMData, temp, len);

                        ZMData = new byte[Main.StorySize];
                        //Frotz.Other.ZMath.clearArray(ZMData);
                        Array.Copy(temp, ZMData, len);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(temp);
                    }
                }

                /* Load story file in chunks of 32KB */

                n = 0x8000;

                for (size = 64; size < Main.StorySize; size += n)
                {
                    if (Main.StorySize - size < 0x8000) n = (ushort)(Main.StorySize - size);
                    SetPc(size);

                    int read = StoryFp.Read(ZMData, (int)Pcp, n);

                    if (read != n) OS.Fatal("Story file read error");
                }

                // Take a moment to calculate the checksum of the story file in case verify is called
                ZMData_checksum = 0;
                for (int k = 64; k < ZMData.Length; k++)
                {
                    ZMData_checksum += ZMData[k];
                }
            }

            DebugState.Output("Story Size: {0}", Main.StorySize);

            FirstRestart = true;

            /* Read header extension table */

            Main.hx_table_size = GetHeaderExtension(ZMachine.HX_TABLE_SIZE);
            Main.hx_unicode_table = GetHeaderExtension(ZMachine.HX_UNICODE_TABLE);
            Main.hx_flags = GetHeaderExtension(ZMachine.HX_FLAGS);
        }/* init_memory */

        /// <summary>
        ///  Allocate memory for multiple undo. It is important not to occupy
        ///  all the memory available, since the IO interface may need memory
        ///  during the game, e.g. for loading sounds or pictures.
        /// </summary>
        internal static void InitUndo()
        {
            var pool = MemoryPool<zbyte>.Shared;
            var len = ZMData.Length;
            zmpHandle?.Dispose();
            zmpHandle = pool.Rent(len);
            PrevZmp = zmpHandle.Memory.Slice(0, len);

            diffHandle?.Dispose();
            diffHandle = pool.Rent(len);
            UndoDiff = diffHandle.Memory.Slice(0, len);

            UndoMem.Clear();

            ZMData.AsSpan(..Main.h_dynamic_size).CopyTo(PrevZmp.Span);
        }

        /// <summary>
        /// Free count undo blocks from the beginning of the undo list.
        /// </summary>
        /// <param name="count"></param>
        internal static void FreeUndo(int count)
        {
            for (int i = 0; i < count; i++)
            {
                UndoMem.RemoveAt(0);
            }

        }

        /// <summary>
        /// Close the story file and deallocate memory.
        /// </summary>
        internal static void ResetMemory()
        {
            StoryFp?.Dispose();
            UndoMem.Clear();
        }

        /*
         * storeb
         *
         * Write a byte value to the dynamic Z-machine memory.
         *
         */

        internal static void StoreB(zword addr, zbyte value)
        {
            if (addr >= Main.h_dynamic_size)
                Err.RuntimeError(ErrorCodes.ERR_STORE_RANGE);

            if (addr == ZMachine.H_FLAGS + 1)
            {	/* flags register is modified */

                unchecked { Main.h_flags &= (zword)(~(ZMachine.SCRIPTING_FLAG | ZMachine.FIXED_FONT_FLAG)); }
                Main.h_flags |= (zword)(value & (ZMachine.SCRIPTING_FLAG | ZMachine.FIXED_FONT_FLAG));

                if ((value & ZMachine.SCRIPTING_FLAG) > 0)
                {
                    if (!Main.ostream_script)
                        Files.ScriptOpen();
                }
                else
                {
                    if (Main.ostream_script)
                        Files.ScriptClose();
                }

                Screen.RefreshTextStyle();

            }

            SetByte(addr, value);

            DebugState.Output("storeb: {0} -> {1}", addr, value);
        }/* storeb */

        /*
         * storew
         *
         * Write a word value to the dynamic Z-machine memory.
         *
         */

        internal static void StoreW(zword addr, zword value)
        {
            StoreB((zword)(addr + 0), Hi(value));
            StoreB((zword)(addr + 1), Lo(value));
        }/* storew */

        /*
         * z_restart, re-load dynamic area, clear the stack and set the PC.
         *
         * 	no zargs used
         *
         */
        internal static void ZRestart()
        {
            Buffer.FlushBuffer();

            OS.RestartGame(ZMachine.RESTART_BEGIN);

            Random.SeedRandom(0);

            if (!FirstRestart)
            {
                if (StoryFp == null)
                    throw new InvalidOperationException("StoryFp not initialized.");

                StoryFp.Position = InitFpPos;

                int read = StoryFp.Read(ZMData, 0, Main.h_dynamic_size);
                if (read != Main.h_dynamic_size)
                {
                    OS.Fatal("Story file read error");
                }
            }
            else
            {
                FirstRestart = false;
            }

            RestartHeader();
            Screen.RestartScreen();

            Main.sp = Main.fp = General.STACK_SIZE; // TODO Critical to make sure this logic works; sp = fp = stack + STACK_SIZE;

            Main.frame_count = 0;

            if (Main.h_version != ZMachine.V6)
            {

                zword pc = Main.h_start_pc;
                FastMem.SetPc(pc);

            }
            else
            {
                Process.Call(Main.h_start_pc, 0, 0, 0);
            }

            OS.RestartGame(ZMachine.RESTART_END);

        }/* z_restart */

        /*
         * get_default_name
         *
         * Read a default file name from the memory of the Z-machine and
         * copy it to a string.
         *
         */

        internal static string? GetDefaultName(zword addr)
        {

            if (addr != 0)
            {

                var vsb = new ValueStringBuilder();

                int i;

                FastMem.LowByte(addr, out zbyte len);
                addr++;

                for (i = 0; i < len; i++)
                {
                    FastMem.LowByte(addr, out zbyte c);
                    addr++;

                    if (c >= 'A' && c <= 'Z')
                        c += 'a' - 'A';

                    // default_name[i] = c;
                    vsb.Append((char)c);

                }

                // default_name[i] = 0;
                if (vsb.IndexOf('.') == -1)
                {
                    vsb.Append(".AUX");
                    return vsb.ToString();
                }
                else
                {
                    return AuxilaryName;
                }
            }
            return null;

        }/* get_default_name */

        /*
         * z_restore, restore [a part of] a Z-machine state from disk
         *
         *	zargs[0] = address of area to restore (optional)
         *	zargs[1] = number of bytes to restore
         *	zargs[2] = address of suggested file name
         *	zargs[3] = whether to ask for confirmation of the file name
         *
         */

        internal static void ZRestore()
        {
            zword success = 0;

            if (Process.zargc != 0)
            {
                OS.Fail("Need to implement optional args in z_restore");
                ///* Get the file name */

                //get_default_name (default_name, (FastMem.zargc >= 3) ? FastMem.zargs[2] : 0);

                //if ((FastMem.zargc >= 4) ? FastMem.zargs[3] : 1) {

                //    if (os_read_file_name (new_name, default_name, ZMachine.FILE_LOAD_AUX) == 0)
                //    goto finished;

                //    strcpy (auxilary_name, new_name);

                //} else strcpy (new_name, default_name);

                ///* Open auxilary file */

                //if ((gfp = fopen (new_name, "rb")) == NULL)
                //    goto finished;

                ///* Load auxilary file */

                //success = fread (zmp + zargs[0], 1, zargs[1], gfp);

                ///* Close auxilary file */

                //fclose (gfp);

            }
            else
            {

                /* Get the file name */

                if (!OS.ReadFileName(out var new_name, SaveName, FileTypes.FILE_RESTORE))
                    goto finished;

                SaveName = new_name;

                if (StoryFp == null)
                    throw new InvalidOperationException("StoryFp not initialized.");

                /* Open game file */
                using (var gfp = new System.IO.FileStream(new_name, System.IO.FileMode.Open))
                {
                    if (gfp == null) goto finished;

                    if (Main.option_save_quetzal == true)
                    {
                        success = Quetzal.RestoreQuetzal(gfp, StoryFp);
                    }
                    else
                    {
                        OS.Fail("Need to implement old style save");
                        /* Load game file */

                        //    release = (unsigned) fgetc (gfp) << 8;
                        //    release |= fgetc (gfp);

                        //    (void) fgetc (gfp);
                        //    (void) fgetc (gfp);

                        //    /* Check the release number */

                        //    if (release == h_release) {

                        //    pc = (long) fgetc (gfp) << 16;
                        //    pc |= (unsigned) fgetc (gfp) << 8;
                        //    pc |= fgetc (gfp);

                        //    SET_PC (pc)

                        //    sp = stack + (fgetc (gfp) << 8);
                        //    sp += fgetc (gfp);
                        //    fp = stack + (fgetc (gfp) << 8);
                        //    fp += fgetc (gfp);

                        //    for (i = (int) (sp - stack); i < STACK_SIZE; i++) {
                        //        stack[i] = (unsigned) fgetc (gfp) << 8;
                        //        stack[i] |= fgetc (gfp);
                        //    }

                        //    fseek (story_fp, init_fp_pos, SEEK_SET);

                        //    for (addr = 0; addr < h_dynamic_size; addr++) {
                        //        int skip = fgetc (gfp);
                        //        for (i = 0; i < skip; i++)
                        //        zmp[addr++] = fgetc (story_fp);
                        //        zmp[addr] = fgetc (gfp);
                        //        (void) fgetc (story_fp);
                        //    }

                        //    /* Check for errors */

                        //    if (ferror (gfp) || ferror (story_fp) || addr != h_dynamic_size)
                        //        success = -1;
                        //    else

                        //        /* Success */

                        //        success = 2;

                        //    } else print_string ("Invalid save file\n");
                    }
                }
            }
            if ((short)success >= 0 && success != zword.MaxValue)
            {
                if ((short)success > 0)
                {

                    /* In V3, reset the upper window. */
                    if (Main.h_version == ZMachine.V3)
                        Screen.SplitWindow(0);

                    LowByte(ZMachine.H_SCREEN_ROWS, out zbyte old_screen_rows);
                    LowByte(ZMachine.H_SCREEN_COLS, out zbyte old_screen_cols);

                    /* Reload cached header fields. */
                    RestartHeader();

                    /*
                     * Since QUETZAL files may be saved on many different machines,
                     * the screen sizes may vary a lot. Erasing the status window
                     * seems to cover up most of the resulting badness.
                     */
                    if (Main.h_version > ZMachine.V3 && Main.h_version != ZMachine.V6
                        && (Main.h_screen_rows != old_screen_rows
                        || Main.h_screen_cols != old_screen_cols))
                    {
                        Screen.EraseWindow(1);
                    }
                }
            }
            else
            {
                OS.Fatal("Error reading save file");
            }

        finished:

            if (Main.h_version <= ZMachine.V3)
                Process.Branch(success > 0);
            else
                Process.Store(success);
        }/* z_restore */

        /// <summary>
        ///   Set diff to a Quetzal-like difference between a and b,
        ///   copying a to b as we go.  It is assumed that diff points to a
        ///   buffer which is large enough to hold the diff.
        ///   mem_size is the number of bytes to compare.
        ///   Returns the number of bytes copied to diff.
        /// </summary>
        private static int MemDiff(ReadOnlySpan<zbyte> a, Span<zbyte> b, zword mem_size, Span<zbyte> diff)
        {
            zword size = mem_size;
            int dPtr = 0;
            uint j;
            zbyte c = 0;

            int aPtr = 0;
            int bPtr = 0;

            for (; ; )
            {
                for (j = 0; size > 0 && (c = (zbyte)(a[aPtr++] ^ b[bPtr++])) == 0; j++)
                    size--;
                if (size == 0) break;

                size--;

                if (j > 0x8000)
                {
                    diff[dPtr++] = 0;
                    diff[dPtr++] = 0xff;
                    diff[dPtr++] = 0xff;
                    j -= 0x8000;
                }

                if (j > 0)
                {
                    diff[dPtr++] = 0;
                    j--;

                    if (j <= 0x7f)
                    {
                        diff[dPtr++] = (byte)j;
                    }
                    else
                    {
                        diff[dPtr++] = (byte)((j & 0x7f) | 0x80);
                        diff[dPtr++] = (byte)((j & 0x7f80) >> 7);
                    }
                }
                diff[dPtr++] = c;
                b[bPtr - 1] ^= c;
            }
            return dPtr;

        }

        /// <summary>
        /// Applies a quetzal-like diff to dest
        /// </summary>
        private static void MemUndiff(ReadOnlySpan<zbyte> diff, long diffLength, Span<zbyte> dest)
        {
            zbyte c;
            int diffPtr = 0;
            int destPtr = 0;

            while (diffLength > 0)
            {
                c = diff[diffPtr++];
                diffLength--;
                if (c == 0)
                {
                    uint runlen;

                    if (diffLength == 0) // TODO I'm not sure about this logic
                        return;  /* Incomplete run */
                    runlen = diff[diffPtr++];
                    diffLength--;
                    if ((runlen & 0x80) > 0)
                    {
                        if (diffLength == 0)
                            return; /* Incomplete extended run */
                        c = diff[diffPtr++];
                        diffLength--;
                        runlen = (runlen & 0x7f) | (((uint)c) << 7);
                    }

                    destPtr += (int)runlen + 1;
                }
                else
                {
                    dest[destPtr++] ^= c;
                }
            }

        }

        /*
         * restore_undo
         *
         * This function does the dirty work for z_restore_undo.
         *
         */

        internal static int RestoreUndo()
        {
            if (Main.option_undo_slots == 0)	/* undo feature unavailable */
                return -1;

            if (UndoMem.Count == 0)
                return 0;

            /* undo possible */

            var undo = UndoMem[^1];

            ZMData.AsSpan(..Main.h_dynamic_size).CopyTo(PrevZmp.Span);
            SetPc(undo.Pc);
            Main.sp = undo.Sp;
            Main.fp = undo.FrameOffset;
            Main.frame_count = undo.FrameCount;

            MemUndiff(undo.UndoData.Span, undo.DiffSize, PrevZmp.Span);

            var mainStack = Main.Stack.AsSpan((int)undo.Sp);
            undo.Stack.Span.CopyTo(mainStack);
            //Array.Copy(undo.Stack, 0, Main.Stack, undo.Sp, undo.Stack.Length);
            //Frotz.Other.ArrayCopy.Copy(undo.stack, 0, Main.stack, undo.sp, undo.stack.Length);

            UndoMem.Remove(undo);

            RestartHeader();

            return 2;

        }/* restore_undo */

        /*
         * z_restore_undo, restore a Z-machine state from memory.
         *
         *	no zargs used
         *
         */

        internal static void ZRestoreUndo()
        {
            Process.Store((zword)RestoreUndo());

        }/* z_restore_undo */

        /*
         * z_save, save [a part of] the Z-machine state to disk.
         *
         *	zargs[0] = address of memory area to save (optional)
         *	zargs[1] = number of bytes to save
         *	zargs[2] = address of suggested file name
         *	zargs[3] = whether to ask for confirmation of the file name
         *
         */

        internal static void ZSave()
        {
            string? default_name;

            zword success = 0;

            if (Process.zargc != 0)
            {

                /* Get the file name */

                default_name = GetDefaultName((zword)((Process.zargc >= 3) ? Process.zargs[2] : 0));

                //    if ((zargc >= 4) ? zargs[3] : 1) {

                //        if (os_read_file_name (new_name, default_name, FILE_SAVE_AUX) == 0)
                //        goto finished;

                //        strcpy (auxilary_name, new_name);

                //    } else strcpy (new_name, default_name);

                //    /* Open auxilary file */

                //    if ((gfp = fopen (new_name, "wb")) == NULL)
                //        goto finished;

                //    /* Write auxilary file */

                //    success = fwrite (zmp + zargs[0], zargs[1], 1, gfp);

                //    /* Close auxilary file */

                //    fclose (gfp);
                OS.Fail("need to implement option save arguments");
            }
            else
            {
                if (!OS.ReadFileName(out var new_name, SaveName, FileTypes.FILE_SAVE))
                    goto finished;

                SaveName = new_name;

                if (StoryFp == null)
                    throw new InvalidOperationException("StoryFp not initialized.");

                /* Open game file */

                using (var gfp = new System.IO.FileStream(new_name, System.IO.FileMode.OpenOrCreate))
                {
                    if (Main.option_save_quetzal == true)
                    {
                        success = Quetzal.SaveQuetzal(gfp, StoryFp);

                    }
                    else
                    {
                        OS.Fail("Need to implement old style save");

                        //        /* Write game file */

                        //        fputc ((int) hi (h_release), gfp);
                        //        fputc ((int) lo (h_release), gfp);
                        //        fputc ((int) hi (h_checksum), gfp);
                        //        fputc ((int) lo (h_checksum), gfp);

                        //        GET_PC (pc)

                        //        fputc ((int) (pc >> 16) & 0xff, gfp);
                        //        fputc ((int) (pc >> 8) & 0xff, gfp);
                        //        fputc ((int) (pc) & 0xff, gfp);

                        //        nsp = (int) (sp - stack);
                        //        nfp = (int) (fp - stack);

                        //        fputc ((int) hi (nsp), gfp);
                        //        fputc ((int) lo (nsp), gfp);
                        //        fputc ((int) hi (nfp), gfp);
                        //        fputc ((int) lo (nfp), gfp);

                        //        for (i = nsp; i < STACK_SIZE; i++) {
                        //        fputc ((int) hi (stack[i]), gfp);
                        //        fputc ((int) lo (stack[i]), gfp);
                        //        }

                        //        fseek (story_fp, init_fp_pos, SEEK_SET);

                        //        for (addr = 0, skip = 0; addr < h_dynamic_size; addr++)
                        //        if (zmp[addr] != fgetc (story_fp) || skip == 255 || addr + 1 == h_dynamic_size) {
                        //            fputc (skip, gfp);
                        //            fputc (zmp[addr], gfp);
                        //            skip = 0;
                        //        } else skip++;
                    }
                }
                /* Close game file and check for errors */

                // TODO Not sure what to do with these
                //    if (gfp.Close() ) { // || ferror(story_fp)) {
                //    Text.print_string("Error writing save file\n");
                //    goto finished;
                //}

                /* Success */

                success = 1;

            }

        finished:

            if (Main.h_version <= ZMachine.V3)
                Process.Branch(success > 0);
            else
                Process.Store(success);
        }/* z_save */

        /*
         * save_undo
         *
         * This function does the dirty work for z_save_undo.
         *
         */

        internal static int SaveUndo()
        {
            if (Main.option_undo_slots == 0)		/* undo feature unavailable */
                return -1;

            /* save undo possible */

            if (UndoCount == Main.option_undo_slots)
                FreeUndo(1);

            var diff_size = MemDiff(ZMData, PrevZmp.Span, Main.h_dynamic_size, UndoDiff.Span);
            var stack_size = Main.Stack.Length;

            GetPc(out long pc);
            // p.undo_data = undo_diff;
            zbyte[] undoData = new zbyte[diff_size];
            UndoDiff[..diff_size].CopyTo(undoData);

            zword[] stack = new zword[Main.Stack.Length - Main.sp];
            Array.Copy(Main.Stack, Main.sp, stack, 0, Main.Stack.Length - Main.sp);

            var undo = new UndoStruct(
                pc, diffSize: diff_size, frameCount: Main.frame_count,
                stackSize: (zword)stack_size, frameOffset: (zword)Main.fp, sp: Main.sp,  //    p->frame_offset = fp - stack;
                stack, undoData
            );

            UndoMem.Add(undo);

            return 1;
        }

        /*
         * z_save_undo, save the current Z-machine state for a future undo.
         *
         *	no zargs used
         *
         */

        internal static void ZSaveUndo()
        {
            Process.Store((zword)SaveUndo());
        }/* z_save_undo */

        /*
         * z_verify, check the story file integrity.
         *
         *	no zargs used
         *
         */

        internal static void ZVerify()
        {
            //zword checksum = 0;
            //long i;

            /* Sum all bytes in story file except header bytes */

            //for (i = 64; i < main.story_size; i++)
            //{
            //    checksum += FastMem.ZMData_original[i];
            //}

            //for (i = 64; i < story_size; i++)
            //    checksum += fgetc(story_fp);

            /* Branch if the checksums are equal */

            Process.Branch(ZMData_checksum == Main.h_checksum);

        }/* z_verify */
    }
}
