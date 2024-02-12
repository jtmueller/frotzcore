/* err.c - Runtime error reporting functions
 *	Written by Jim Dunleavy <jim.dunleavy@erha.ie>
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

namespace Frotz.Generic;

internal static class Err
{
    /* Define stuff for stricter Z-code error checking, for the generic
       Unix/DOS/etc terminal-window interface. Feel free to change the way
       player prefs are specified, or replace report_zstrict_error() 
       completely if you want to change the way errors are reported. */

    internal static int ErrorReportMode = ErrorCodes.ERR_DEFAULT_REPORT_MODE;
    private static readonly int[] error_count = new int[ErrorCodes.ERR_NUM_ERRORS];
    private static readonly string[] err_messages = [
        "Text buffer overflow",
        "Store out of dynamic memory",
        "Division by zero",
        "Illegal object",
        "Illegal attribute",
        "No such property",
        "Stack overflow",
        "Call to illegal address",
        "Call to non-routine",
        "Stack underflow",
        "Illegal opcode",
        "Bad stack frame",
        "Jump to illegal address",
        "Can't save while in interrupt",
        "Nesting stream #3 too deep",
        "Illegal window",
        "Illegal window property",
        "Print at illegal address",
        "Illegal dictionary word length",
        "@jin called with object 0",
        "@get_child called with object 0",
        "@get_parent called with object 0",
        "@get_sibling called with object 0",
        "@get_prop_addr called with object 0",
        "@get_prop called with object 0",
        "@put_prop called with object 0",
        "@clear_attr called with object 0",
        "@set_attr called with object 0",
        "@test_attr called with object 0",
        "@move_object called moving object 0",
        "@move_object called moving into object 0",
        "@remove_object called with object 0",
        "@get_next_prop called with object 0"
    ];

    //static void print_long (unsigned long value, int base);

    /*
     * init_err
     *
     * Initialise error reporting.
     *
     */

    internal static void InitErr() =>
        /* Initialize the counters. */
        error_count.AsSpan(..ErrorCodes.ERR_NUM_ERRORS).Clear();

    /*
     * runtime_error
     *
     * An error has occurred. Ignore it, pass it to os_fatal or report
     * it according to err_report_mode.
     *
     * errnum : Numeric code for error (1 to ERR_NUM_ERRORS)
     *
     */

    internal static void RuntimeError(int errnum)
    {
        bool wasfirst;

        if (errnum is <= 0 or > ErrorCodes.ERR_NUM_ERRORS)
            return;

        if (ErrorReportMode == ErrorCodes.ERR_REPORT_FATAL
        || (Main.option_ignore_errors == false && errnum <= ErrorCodes.ERR_MAX_FATAL))
        {
            Buffer.FlushBuffer();
            OS.Fatal(err_messages[errnum - 1]);
            return;
        }

        wasfirst = (error_count[errnum - 1] == 0);
        error_count[errnum - 1]++;

        if ((ErrorReportMode == ErrorCodes.ERR_REPORT_ALWAYS)
        || (ErrorReportMode == ErrorCodes.ERR_REPORT_ONCE && wasfirst))
        {

            FastMem.GetPc(out long pc);
            Text.PrintString("Warning: ");
            Text.PrintString(err_messages[errnum - 1]);
            Text.PrintString(" (PC = ");
            PrintLong(pc, 16);
            Buffer.PrintChar(')');

            if (ErrorReportMode == ErrorCodes.ERR_REPORT_ONCE)
            {
                Text.PrintString(" (will ignore further occurrences)");
            }
            else
            {
                Text.PrintString(" (occurrence ");
                PrintLong(error_count[errnum - 1], 10);
                Buffer.PrintChar(')');
            }
            Buffer.NewLine();
        }

    } /* report_error */

    /*
     * print_long
     *
     * Print an unsigned 32bit number in decimal or hex.
     *
     */

    private static void PrintLong(long value, int base_val)
    {
        string s = string.Empty;
        switch (base_val)
        {
            case 10: s = value.ToString(); break;
            case 16: s = value.ToString("X"); break;
            default: OS.Fail("Unsupported print_long base"); break;
        }
        Text.PrintString(s);
    }/* print_long */
}
