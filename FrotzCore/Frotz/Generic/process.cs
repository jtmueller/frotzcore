/* process.c - Interpreter loop and program control
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
using System;
using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class Process
    {
        internal static readonly zword[] zargs = new zword[8];
        internal static int zargc;

        internal static int finished = 0;

        public delegate void ZInstruction();

        internal static readonly ZInstruction[] op0_opcodes = new ZInstruction[0x10] 
        {
            new(ZRTrue),
            new(ZRFalse),
            new(Text.ZPrint),
            new(Text.ZPrintRet),
            new(ZNoop),
            new(FastMem.ZSave),
            new(FastMem.ZRestore),
            new(FastMem.ZRestart),
            new(ZRetPopped),
            new(ZCatch),
            new(ZQuit),
            new(Text.ZNewLine),
            new(Screen.ZShowStatus),
            new(FastMem.ZVerify), // Not Tested or Implemented
            new(__extended__),
            new(Main.ZPiracy)
        };

        internal static readonly ZInstruction[] op1_opcodes = new ZInstruction[0x10] 
        {
            new(Math.ZJz),
            new(CObject.ZGetSibling),
            new(CObject.ZGetChild),
            new(CObject.ZGetParent),
            new(CObject.ZGetPropLen),
            new(Variable.ZInc),
            new(Variable.ZDec),
            new(Text.ZPrintAddr),
            new(ZCallS),
            new(CObject.ZRemoveObj),
            new(Text.ZPrintObj),
            new(ZRet),
            new(ZJump),
            new(Text.ZPrintPaddr),
            new(Variable.ZLoad),
            new(ZCallN),
        };

        internal static readonly ZInstruction[] var_opcodes = new ZInstruction[0x40] 
        {
            new(__illegal__),
            new(Math.ZJe),
            new(Math.ZJl),
            new(Math.ZJg),
            new(Variable.ZDecChk),
            new(Variable.ZIncChk),
            new(CObject.ZJin),
            new(Math.ZTest),
            new(Math.ZOr),
            new(Math.ZAnd),
            new(CObject.ZTestAttr),
            new(CObject.ZSetAttr),
            new(CObject.ZClearAttr),
            new(Variable.ZStore),
            new(CObject.ZInsertObj),
            new(Table.ZLoadW),
            new(Table.ZLoadB),
            new(CObject.ZGetProp),
            new(CObject.ZGetPropAddr),
            new(CObject.ZGetNextProp),
            new(Math.ZAdd),
            new(Math.ZSub),
            new(Math.ZMul),
            new(Math.ZDiv),
            new(Math.ZMod),
            new(ZCallS),
            new(ZCallN),
            new(Screen.ZSetColor),
            new(ZThrow),
            new(__illegal__),
            new(__illegal__),
            new(__illegal__),
            new(ZCallS),
            new(Table.ZStoreW),
            new(Table.ZStoreB),
            new(CObject.ZPutProp),
            new(Input.ZRead),
            new(Text.ZPrintChar),
            new(Text.ZPrintNum),
            new(Random.ZRandom),
            new(Variable.ZPush),
            new(Variable.ZPull),
            new(Screen.ZSplitWindow),
            new(Screen.ZSetWindow),
            new(ZCallS),
            new(Screen.ZEraseWindow),
            new(Screen.ZEraseLine),
            new(Screen.ZSetCursor),
            new(Screen.ZGetCursor),
            new(Screen.ZSetTextStyle),
            new(Screen.ZBufferMode),
            new(Stream.ZOutputStream),
            new(Stream.ZIputStream),
            new(Sound.ZSoundEffect),
            new(Input.ZReadChar),
            new(Table.ZScanTable),
            new(Math.ZNot),
            new(ZCallN),
            new(ZCallN),
            new(Text.ZTokenise),
            new(Text.ZEncodeText),
            new(Table.ZCopyTable),
            new(Screen.ZPrintTable),
            new(ZCheckArgCount),
        };

        internal static readonly ZInstruction[] ext_opcodes = new ZInstruction[0x1e]
        {
            new(FastMem.ZSave),
            new(FastMem.ZRestore),
            new(Math.ZLogShift),
            new(Math.ZArtShift), // TODO Not tested
            new(Screen.ZSetFont),
            new(Screen.ZDrawPicture),
            new(Screen.ZPictureData),
            new(Screen.ZErasePicture),
            new(Screen.ZSetMargins),
            new(FastMem.ZSaveUndo),
            new(FastMem.ZRestoreUndo),//    z_restore_undo, // 10
            new(Text.ZPrintUnicode),
            new(Text.ZCheckUnicode),
            new(Screen.ZSetTrueColor),	/* spec 1.1 */
            new(__illegal__),
            new(__illegal__),
            new(Screen.ZMoveWindow),
            new(Screen.ZWindowSize),
            new(Screen.ZWindowStyle),
            new(Screen.ZGetWindProp),
            new(Screen.ZScrollWindow), // 20
            new(Variable.ZPopStack),
            new(Input.ZReadMouse),//    z_read_mouse,
            new(Screen.ZMouseWindow),
            new(Variable.ZPushStack),
            new(Screen.ZPutWindProp),
            new(Text.ZPrintForm),//    z_print_form,
            new(Input.ZMakeMenu),//    z_make_menu,
            new(Screen.ZPictureTable),
            new(Screen.ZBufferScreen),   /* spec 1.1 */
        };
        private static int invokeCount = 0;
        private static void PrivateInvoke(ZInstruction instruction, string array, int index, int opcode)
        {
            DebugState.LastCallMade = instruction.Method.Name + ":" + opcode;
            DebugState.Output(false, "Invoking: {0:X} -> {1} -> {2}", opcode, instruction.Method.Name, invokeCount);
            instruction.Invoke();
            invokeCount++;
        }

        /*
         * init_process
         *
         * Initialize process variables.
         *
         */

        internal static void InitProcess() => finished = 0;

        /*
         * load_operand
         *
         * Load an operand, either a variable or a constant.
         *
         */

        private static void LoadOperand(zbyte type)
        {
            zword value;

            if ((type & 2) > 0)
            { 			/* variable */

                FastMem.CodeByte(out zbyte variable);

                if (variable == 0)
                {
                    value = Main.Stack[Main.sp++];
                }
                else if (variable < 16)
                {
                    value = Main.Stack[Main.fp - variable];
                }
                else
                {
                    zword addr = (zword)(Main.h_globals + 2 * (variable - 16)); // TODO Make sure this logic
                    FastMem.LowWord(addr, out value);
                }

            }
            else if ((type & 1) > 0)
            { 		/* small constant */

                FastMem.CodeByte(out zbyte bvalue);
                value = bvalue;
            }
            else
            {
                FastMem.CodeWord(out value);      /* large constant */
            }

            zargs[zargc++] = value;

            DebugState.Output("  Storing operand: {0} -> {1}", zargc - 1, value);

        }/* load_operand */

        /*
         * load_all_operands
         *
         * Given the operand specifier byte, load all (up to four) operands
         * for a VAR or EXT opcode.
         *
         */

        internal static void LoadAllOperands(zbyte specifier)
        {
            int i;

            for (i = 6; i >= 0; i -= 2)
            {

                zbyte type = (zbyte)((specifier >> i) & 0x03); // TODO Check this conversion

                if (type == 3)
                    break;

                LoadOperand(type);

            }

        }/* load_all_operands */

        /*
         * interpret
         *
         * Z-code interpreter main loop
         *
         */

        internal static void Interpret()
        {
            do
            {
                FastMem.CodeByte(out zbyte opcode);

                DebugState.Output("CODE: {0} -> {1:X}", FastMem.Pcp - 1, opcode);

                if (Main.AbortGameLoop)
                {
                    Main.AbortGameLoop = false;
                    return;
                }

                zargc = 0;
                if (opcode < 0x80)
                {			/* 2OP opcodes */
                    LoadOperand((zbyte)((opcode & 0x40) > 0 ? 2 : 1));
                    LoadOperand((zbyte)((opcode & 0x20) > 0 ? 2 : 1));

                    PrivateInvoke(var_opcodes[opcode & 0x1f], "2OP", (opcode & 0x1f), opcode);
                }
                else if (opcode < 0xb0)
                {	/* 1OP opcodes */
                    LoadOperand((zbyte)(opcode >> 4));
                    PrivateInvoke(op1_opcodes[opcode & 0x0f], "1OP", (opcode & 0x0f), opcode);
                }
                else if (opcode < 0xc0)
                {	/* 0OP opcodes */
                    PrivateInvoke(op0_opcodes[opcode - 0xb0], "0OP", (opcode - 0xb0), opcode);
                }
                else
                {	/* VAR opcodes */
                    zbyte specifier1;

                    if (opcode == 0xec || opcode == 0xfa)
                    {	/* opcodes 0xec */
                        FastMem.CodeByte(out specifier1);                  /* and 0xfa are */
                        FastMem.CodeByte(out zbyte specifier2);                  /* call opcodes */
                        LoadAllOperands(specifier1);		/* with up to 8 */
                        LoadAllOperands(specifier2);         /* arguments    */
                    }
                    else
                    {
                        FastMem.CodeByte(out specifier1);
                        LoadAllOperands(specifier1);
                    }

                    PrivateInvoke(var_opcodes[opcode - 0xc0], "VAR", (opcode - 0xc0), opcode);
                }

                OS.Tick();
            } while (finished == 0);

            finished--;
        }/* interpret */

        /*
         * call
         *
         * Call a subroutine. Save PC and FP then load new PC and initialise
         * new stack frame. Note that the caller may legally provide less or
         * more arguments than the function actually has. The call type "ct"
         * can be 0 (z_call_s), 1 (z_call_n) or 2 (direct call).
         *
         */
        internal static void Call(zword routine, int argc, int args_offset, int ct)
        {
            zword value;
            int i;

            if (Main.sp < 4)//if (sp - stack < 4)
                Err.RuntimeError(ErrorCodes.ERR_STK_OVF);

            FastMem.GetPc(out long pc);

            Main.Stack[--Main.sp] = (zword)(pc >> 9);
            Main.Stack[--Main.sp] = (zword)(pc & 0x1ff);
            Main.Stack[--Main.sp] = (zword)(Main.fp - 1); // *--sp = (zword) (fp - stack - 1);
            Main.Stack[--Main.sp] = (zword)(argc | (ct << (Main.option_save_quetzal == true ? 12 : 8)));

            Main.fp = Main.sp;
            Main.frame_count++;

            DebugState.Output("Added Frame: {0} -> {1}:{2}:{3}:{4}",
                Main.frame_count,
                Main.Stack[Main.sp + 0],
                Main.Stack[Main.sp + 1],
                Main.Stack[Main.sp + 2],
                Main.Stack[Main.sp + 3]);

            /* Calculate byte address of routine */

            pc = Main.h_version <= ZMachine.V3
                ? (long)routine << 1
                : Main.h_version <= ZMachine.V5
                    ? (long)routine << 2
                    : Main.h_version <= ZMachine.V7 
                        ? ((long)routine << 2) + ((long)Main.h_functions_offset << 3) : (long)routine << 3;

            if (pc >= Main.StorySize)
                Err.RuntimeError(ErrorCodes.ERR_ILL_CALL_ADDR);

            FastMem.SetPc(pc);

            /* Initialise local variables */

            FastMem.CodeByte(out zbyte count);

            if (count > 15)
                Err.RuntimeError(ErrorCodes.ERR_CALL_NON_RTN);
            if (Main.sp < count)
                Err.RuntimeError(ErrorCodes.ERR_STK_OVF);

            if (Main.option_save_quetzal == true)
                Main.Stack[Main.fp] |= (zword)(count << 8);	/* Save local var count for Quetzal. */

            value = 0;

            for (i = 0; i < count; i++)
            {

                if (Main.h_version <= ZMachine.V4)		/* V1 to V4 games provide default */
                    FastMem.CodeWord(out value);		/* values for all local variables */

                Main.Stack[--Main.sp] = (argc-- > 0) ? zargs[args_offset + i] : value;
                //*--sp = (zword) ((argc-- > 0) ? args[i] : value);
            }

            /* Start main loop for direct calls */

            if (ct == 2)
                Interpret();
        }/* call */

        /*
         * ret
         *
         * Return from the current subroutine and restore the previous stack
         * frame. The result may be stored (0), thrown away (1) or pushed on
         * the stack (2). In the latter case a direct call has been finished
         * and we must exit the interpreter loop.
         *
         */

        internal static void Ret(zword value)
        {
            long pc;
            int ct;

            if (Main.sp > Main.fp)
                Err.RuntimeError(ErrorCodes.ERR_STK_UNDF);

            Main.sp = Main.fp;

            DebugState.Output("Removing Frame: {0}", Main.frame_count);

            ct = Main.Stack[Main.sp++] >> (Main.option_save_quetzal == true ? 12 : 8);
            Main.frame_count--;
            Main.fp = 1 + Main.Stack[Main.sp++]; // fp = stack + 1 + *sp++;
            pc = Main.Stack[Main.sp++];
            pc = (Main.Stack[Main.sp++] << 9) | (int)pc; // TODO Really don't trust casting PC to int

            FastMem.SetPc(pc);

            /* Handle resulting value */

            if (ct == 0)
                Store(value);
            if (ct == 2)
                Main.Stack[--Main.sp] = value;

            /* Stop main loop for direct calls */

            if (ct == 2)
                finished++;

        }/* ret */

        /*
         * branch
         *
         * Take a jump after an instruction based on the flag, either true or
         * false. The branch can be short or long; it is encoded in one or two
         * bytes respectively. When bit 7 of the first byte is set, the jump
         * takes place if the flag is true; otherwise it is taken if the flag
         * is false. When bit 6 of the first byte is set, the branch is short;
         * otherwise it is long. The offset occupies the bottom 6 bits of the
         * first byte plus all the bits in the second byte for long branches.
         * Uniquely, an offset of 0 means return false, and an offset of 1 is
         * return true.
         *
         */
        internal static void Branch(bool flag)
        {
            FastMem.CodeByte(out zbyte specifier);

            zbyte off1 = (zbyte)(specifier & 0x3f);

            if (!flag)
                specifier ^= 0x80;

            zword offset;
            if ((specifier & 0x40) == 0)
            { // if (!(specifier & 0x40)) {		/* it's a long branch */

                if ((off1 & 0x20) > 0)		/* propagate sign bit */
                    off1 |= 0xc0;

                FastMem.CodeByte(out zbyte off2);

                offset = (zword)((off1 << 8) | off2);
            }
            else
            {
                offset = off1;        /* it's a short branch */
            }

            if ((specifier & 0x80) > 0)
            {

                if (offset > 1)
                {		/* normal branch */
                    FastMem.GetPc(out long pc);
                    pc += (short)offset - 2;
                    FastMem.SetPc(pc);
                }
                else
                {
                    Ret(offset);      /* special case, return 0 or 1 */
                }
            }
        }/* branch */

        /*
         * store
         *
         * Store an operand, either as a variable or pushed on the stack.
         *
         */
        internal static void Store(zword value)
        {
            FastMem.CodeByte(out zbyte variable);

            if (variable == 0)
            {
                Main.Stack[--Main.sp] = value; // *--sp = value;
                DebugState.Output("  Storing {0} on stack at {1}", value, Main.sp);
            }
            else if (variable < 16)
            {
                Main.Stack[Main.fp - variable] = value;  // *(fp - variable) = value;
                DebugState.Output("  Storing {0} on stack as Variable {1} at {2}", value, variable, Main.sp);
            }
            else
            {
                zword addr = (zword)(Main.h_globals + 2 * (variable - 16));
                FastMem.SetWord(addr, value);
                DebugState.Output("  Storing {0} at {1}", value, addr);
            }

        }/* store */

        /*
         * direct_call
         *
         * Call the interpreter loop directly. This is necessary when
         *
         * - a sound effect has been finished
         * - a read instruction has timed out
         * - a newline countdown has hit zero
         *
         * The interpreter returns the result value on the stack.
         *
         */
        internal static int DirectCall(zword addr)
        {
            Span<zword> saved_zargs = stackalloc zword[8];
            int saved_zargc;
            int i;

            /* Calls to address 0 return false */

            if (addr == 0)
                return 0;

            /* Save operands and operand count */

            for (i = 0; i < 8; i++)
                saved_zargs[i] = zargs[i];

            saved_zargc = zargc;

            /* Call routine directly */

            Call(addr, 0, 0, 2);

            /* Restore operands and operand count */

            for (i = 0; i < 8; i++)
                zargs[i] = saved_zargs[i];

            zargc = saved_zargc;

            /* Resulting value lies on top of the stack */

            return (short)Main.Stack[Main.sp++];

        }/* direct_call */

        /*
         * __extended__
         *
         * Load and execute an extended opcode.
         *
         */

        private static void __extended__()
        {
            FastMem.CodeByte(out zbyte opcode);
            FastMem.CodeByte(out zbyte specifier);

            LoadAllOperands(specifier);

            if (opcode < 0x1e)			/* extended opcodes from 0x1e on */
                // ext_opcodes[opcode] ();		/* are reserved for future spec' */
                PrivateInvoke(ext_opcodes[opcode], "Extended", opcode, opcode);

        }/* __extended__ */

        /*
         * __illegal__
         *
         * Exit game because an unknown opcode has been hit.
         *
         */

        private static void __illegal__()
        {

            Err.RuntimeError(ErrorCodes.ERR_ILL_OPCODE);

        }/* __illegal__ */

        /*
         * z_catch, store the current stack frame for later use with z_throw.
         *
         *	no zargs used
         *
         */

        internal static void ZCatch() => 
            Process.Store((zword)(Main.option_save_quetzal == true ? Main.frame_count : Main.fp));/* z_catch */

        /*
         * z_throw, go back to the given stack frame and return the given value.
         *
         *	zargs[0] = value to return
         *	zargs[1] = stack frame
         *
         */

        internal static void ZThrow()
        {
            // TODO This has never been tested
            if (Main.option_save_quetzal == true)
            {
                if (zargs[1] > Main.frame_count)
                    Err.RuntimeError(ErrorCodes.ERR_BAD_FRAME);

                /* Unwind the stack a frame at a time. */
                for (; Main.frame_count > zargs[1]; --Main.frame_count)
                    //fp = stack + 1 + fp[1];
                    Main.fp = 1 + Main.Stack[Main.fp + 1]; // TODO I think this is correct
            }
            else
            {
                if (zargs[1] > General.STACK_SIZE)
                    Err.RuntimeError(ErrorCodes.ERR_BAD_FRAME);

                Main.fp = zargs[1]; // fp = stack + zargs[1];
            }

            Ret(zargs[0]);

        }/* z_throw */

        /*
         * z_call_n, call a subroutine and discard its result.
         *
         * 	zargs[0] = packed address of subroutine
         *	zargs[1] = first argument (optional)
         *	...
         *	zargs[7] = seventh argument (optional)
         *
         */

        internal static void ZCallN()
        {

            if (Process.zargs[0] != 0)
                Process.Call(zargs[0], zargc - 1, 1, 1);

        }/* z_call_n */

        /*
         * z_call_s, call a subroutine and store its result.
         *
         * 	zargs[0] = packed address of subroutine
         *	zargs[1] = first argument (optional)
         *	...
         *	zargs[7] = seventh argument (optional)
         *
         */

        internal static void ZCallS()
        {

            if (zargs[0] != 0)
                Call(zargs[0], zargc - 1, 1, 0); // TODO Was "call (zargs[0], zargc - 1, zargs + 1, 0);"
            else
                Store(0);

        }/* z_call_s */

        /*
         * z_check_arg_count, branch if subroutine was called with >= n arg's.
         *
         * 	zargs[0] = number of arguments
         *
         */

        internal static void ZCheckArgCount()
        {

            if (Main.fp == General.STACK_SIZE)
                Branch(zargs[0] == 0);
            else
                Branch(zargs[0] <= (zword)(Main.Stack[Main.fp] & 0xff)); //   (*fp & 0xff));

        }/* z_check_arg_count */

        /*
         * z_jump, jump unconditionally to the given address.
         *
         *	zargs[0] = PC relative address
         *
         */

        internal static void ZJump()
        {

            FastMem.GetPc(out long pc);

            pc += (short)zargs[0] - 2; // TODO This actually counts on an overflow to work

            if (pc >= Main.StorySize)
                Err.RuntimeError(ErrorCodes.ERR_ILL_JUMP_ADDR);

            FastMem.SetPc(pc);

        }/* z_jump */

        /*
         * z_nop, no operation.
         *
         *	no zargs used
         *
         */

        internal static void ZNoop()
        {

            /* Do nothing */

        }/* z_nop */

        /*
         * z_quit, stop game and exit interpreter.
         *
         *	no zargs used
         *
         */

        internal static void ZQuit()
        {

            finished = 9999;

        }/* z_quit */

        /*
         * z_ret, return from a subroutine with the given value.
         *
         *	zargs[0] = value to return
         *
         */

        internal static void ZRet()
        {

            Ret(zargs[0]);

        }/* z_ret */

        /*
         * z_ret_popped, return from a subroutine with a value popped off the stack.
         *
         *	no zargs used
         *
         */

        internal static void ZRetPopped()
        {

            Ret(Main.Stack[Main.sp++]);
            // ret (*sp++);

        }/* z_ret_popped */

        /*
         * z_rfalse, return from a subroutine with false (0).
         *
         * 	no zargs used
         *
         */

        internal static void ZRFalse()
        {

            Ret(0);

        }/* z_rfalse */

        /*
         * z_rtrue, return from a subroutine with true (1).
         *
         * 	no zargs used
         *
         */

        internal static void ZRTrue()
        {

            Ret(1);

        }/* z_rtrue */
    }
}