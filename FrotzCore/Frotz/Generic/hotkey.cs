/* hotkey.c - Hot key functions
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
    internal static class Hotkey
    {

        /*
         * hot_key_debugging
         *
         * ...allows user to toggle cheating options on/off.
         *
         */

        internal static bool HotkeyDebugging()
        {

            Text.PrintString("Debugging options\n");

            Main.option_attribute_assignment = Input.ReadYesOrNo("Watch attribute assignment");
            Main.option_attribute_testing = Input.ReadYesOrNo("Watch attribute testing");
            Main.option_object_movement = Input.ReadYesOrNo("Watch object movement");
            Main.option_object_locating = Input.ReadYesOrNo("Watch object locating");

            return false;

        }/* hot_key_debugging */

        /*
         * hot_key_help
         *
         * ...displays a list of all hot keys.
         *
         */

        private static bool HotkeyHelp()
        {

            Text.PrintString("Help\n");

            Text.PrintString(
            "\n" +
            "Alt-D  debugging options\n" +
            "Alt-H  help\n" +
            "Alt-N  new game\n" +
            "Alt-P  playback on\n" +
            "Alt-R  recording on/off\n" +
            "Alt-S  seed random numbers\n" +
            "Alt-U  undo one turn\n" +
            "Alt-X  exit game\n");

            return false;

        }/* hot_key_help */

        /*
         * hot_key_playback
         *
         * ...allows user to turn playback on.
         *
         */

        internal static bool HotkeyPlayback()
        {
            Text.PrintString("Playback on\n");

            if (!Main.istream_replay)
                Files.ReplayOpen();

            return false;

        }/* hot_key_playback */

        /*
         * hot_key_recording
         *
         * ...allows user to turn recording on/off.
         *
         */

        internal static bool HotkeyRecording()
        {

            if (Main.istream_replay)
            {
                Text.PrintString("Playback off\n");
                Files.ReplayClose();
            }
            else if (Main.ostream_record)
            {
                Text.PrintString("Recording off\n");
                Files.RecordClose();
            }
            else
            {
                Text.PrintString("Recording on\n");
                Files.RecordOpen();
            }

            return false;

        }/* hot_key_recording */

        /*
         * hot_key_seed
         *
         * ...allows user to seed the random number seed.
         *
         */

        internal static bool HitkeySeed()
        {

            Text.PrintString("Seed random numbers\n");

            Text.PrintString("Enter seed value (or return to randomize): ");
            Random.SeedRandom(Input.ReadNumber());

            return false;

        }/* hot_key_seed */

        /*
         * hot_key_undo
         *
         * ...allows user to undo the previous turn.
         *
         */

        public static bool HotkeyUndo()
        {
            Text.PrintString("Undo one turn\n");

            if (FastMem.RestoreUndo() > 0)
            {

                if (Main.h_version >= ZMachine.V5)
                {       /* for V5+ games we must */
                    Process.Store(2);           /* store 2 (for success) */
                    return true;        /* and abort the input   */
                }

                if (Main.h_version <= ZMachine.V3)
                {       /* for V3- games we must */
                    Screen.ZShowStatus();       /* draw the status line  */
                    return false;       /* and continue input    */
                }

            }
            else
            {
                Text.PrintString("No more undo information available.\n");
            }

            return false;

        }/* hot_key_undo */

        /*
         * hot_key_restart
         *
         * ...allows user to start a new game.
         *
         */

        public static bool HotkeyRestart()
        {

            Text.PrintString("New game\n");

            if (Input.ReadYesOrNo("Do you wish to restart"))
            {

                FastMem.ZRestart();
                return true;

            }
            else
            {
                return false;
            }
        }/* hot_key_restart */

        /*
         * hot_key_quit
         *
         * ...allows user to exit the game.
         *
         */

        private static bool HotkeyQuit()
        {

            Text.PrintString("Exit game\n");

            if (Input.ReadYesOrNo("Do you wish to quit"))
            {

                Process.ZQuit();
                return true;

            }
            else
            {
                return false;
            }
        }/* hot_key_quit */

        /*
         * handle_hot_key
         *
         * Perform the action associated with a so-called hot key. Return
         * true to abort the current input action.
         *
         */

        public static bool HandleHotkey(zword key)
        {

            if (Main.cwin == 0)
            {
                Text.PrintString("\nHot key -- ");

                bool aborting = key switch
                {
                    CharCodes.ZC_HKEY_RECORD   => HotkeyRecording(),
                    CharCodes.ZC_HKEY_PLAYBACK => HotkeyPlayback(),
                    CharCodes.ZC_HKEY_SEED     => HitkeySeed(),
                    CharCodes.ZC_HKEY_UNDO     => HotkeyUndo(),
                    CharCodes.ZC_HKEY_RESTART  => HotkeyRestart(),
                    CharCodes.ZC_HKEY_QUIT     => HotkeyQuit(),
                    CharCodes.ZC_HKEY_DEBUG    => HotkeyDebugging(),
                    CharCodes.ZC_HKEY_HELP     => HotkeyHelp(),
                    _ => false,
                };

                if (aborting)
                    return false;

                Text.PrintString("\nContinue input...\n");

            }

            return false;

        }/* handle_hot_key */
    }
}