/* math.c - Arithmetic, compare and logical opcodes
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

using zword = System.UInt16;

namespace Frotz.Generic
{
    internal static class Math
    {
        /*
         * z_add, 16bit addition.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZAdd() 
            => Process.Store((zword)((short)Process.zargs[0] + (short)Process.zargs[1])); /* z_add */

        /*
         * z_and, bitwise AND operation.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZAnd() 
            => Process.Store((zword)(Process.zargs[0] & Process.zargs[1])); /* z_and */

        /*
         * z_art_shift, arithmetic SHIFT operation.
         *
         *	zargs[0] = value
         *	zargs[1] = #positions to shift left (positive) or right
         *
         */

        internal static void ZArtShift()
        {
            // TODO This code has never been hit... I need to find something that will hit it
            if ((short)Process.zargs[1] > 0)
                Process.Store((zword)((short)Process.zargs[0] << (short)Process.zargs[1]));
            else
                Process.Store((zword)((short)Process.zargs[0] >> -(short)Process.zargs[1]));

        }/* z_art_shift */

        /*
         * z_div, signed 16bit division.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZDiv()
        {
            if (Process.zargs[1] == 0)
                Err.RuntimeError(ErrorCodes.ERR_DIV_ZERO);

            Process.Store((zword)((short)Process.zargs[0] / (short)Process.zargs[1]));
        }/* z_div */

        /*
         * z_je, branch if the first value equals any of the following.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value (optional)
         *	...
         *	zargs[3] = fourth value (optional)
         *
         */
        internal static void ZJe()
        {
            Process.Branch(
                Process.zargc > 1 && (Process.zargs[0] == Process.zargs[1] || (
                Process.zargc > 2 && (Process.zargs[0] == Process.zargs[2] || (
                Process.zargc > 3 && (Process.zargs[0] == Process.zargs[3]))))));

        } /* z_je */

        /*
         * z_jg, branch if the first value is greater than the second.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZJg() 
            => Process.Branch((short)Process.zargs[0] > (short)Process.zargs[1]); /* z_jg */

        /*
         * z_jl, branch if the first value is less than the second.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZJl() 
            => Process.Branch((short)Process.zargs[0] < (short)Process.zargs[1]); /* z_jl */

        /*
         * z_jz, branch if value is zero.
         *
         * 	zargs[0] = value
         *
         */
        internal static void ZJz() 
            => Process.Branch((short)Process.zargs[0] == 0); /* z_jz */

        /*
         * z_log_shift, logical SHIFT operation.
         *
         * 	zargs[0] = value
         *	zargs[1] = #positions to shift left (positive) or right (negative)
         *
         */
        internal static void ZLogShift()
        {
            if ((short)Process.zargs[1] > 0)
                Process.Store((zword)(Process.zargs[0] << (short)Process.zargs[1]));
            else
                Process.Store((zword)(Process.zargs[0] >> -(short)Process.zargs[1]));
        } /* z_log_shift */

        /*
         * z_mod, remainder after signed 16bit division.
         *
         * 	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZMod()
        {
            if (Process.zargs[1] == 0)
                Err.RuntimeError(ErrorCodes.ERR_DIV_ZERO);

            Process.Store((zword)((short)Process.zargs[0] % (short)Process.zargs[1]));
        } /* z_mod */

        /*
         * z_mul, 16bit multiplication.
         *
         * 	zargs[0] = first value
         *	zargs[1] = second value
         *
         */
        internal static void ZMul() 
            => Process.Store((zword)((short)Process.zargs[0] * (short)Process.zargs[1])); /* z_mul */

        /*
         * z_not, bitwise NOT operation.
         *
         * 	zargs[0] = value
         *
         */
        internal static void ZNot() 
            => Process.Store((zword)~Process.zargs[0]); /* z_not */

        /*
         * z_or, bitwise OR operation.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */

        internal static void ZOr()
            => Process.Store((zword)(Process.zargs[0] | Process.zargs[1])); /* z_or */

        /*
         * z_sub, 16bit substraction.
         *
         *	zargs[0] = first value
         *	zargs[1] = second value
         *
         */

        internal static void ZSub()
            => Process.Store((zword)((short)Process.zargs[0] - (short)Process.zargs[1])); /* z_sub */

        /*
         * z_test, branch if all the flags of a bit mask are set in a value.
         *
         *	zargs[0] = value to be examined
         *	zargs[1] = bit mask
         *
         */

        internal static void ZTest()
            => Process.Branch((Process.zargs[0] & Process.zargs[1]) == Process.zargs[1]); /* z_test */
    }
}