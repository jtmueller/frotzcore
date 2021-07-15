using Frotz.Generic;
using Frotz.Other;
using Frotz.Screen;
using Microsoft.IO;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using zbyte = System.Byte;
using zword = System.UInt16;

namespace Frotz
{
    public static class OS
    {
        public static readonly RecyclableMemoryStreamManager StreamManger = new();

        private const int MaxStack = 0xff;
        private static int HistoryPos = 0;
        // TODO This really needs to get wired up when a new game is started
        private static readonly List<string> History = new();

        private static long ReadLong(ReadOnlySpan<byte> buffer) => BinaryPrimitives.ReadInt64BigEndian(buffer);

        public static Blorb.Blorb? BlorbFile = null; // TODO Make this static again, or something

        private static IZScreen? Screen;

        public static void SetScreen(IZScreen screen)
        {
            if (OS.Screen != null)
            {
                OS.Screen.KeyPressed -= new EventHandler<ZKeyPressEventArgs>(Screen_KeyPressed);
            }

            OS.Screen = screen;
            OS.Screen.KeyPressed += new EventHandler<ZKeyPressEventArgs>(Screen_KeyPressed);
        }

        private static void AddTestKeys()
        {
            //entries.Enqueue(' ');
            //enqueue_word("ne");
            //enqueue_word("look at sundial");
        }

        private static void Screen_KeyPressed(object? sender, ZKeyPressEventArgs e) => Entries.Enqueue(e.KeyPressed);

        private static void OnFatalError(string message) { Screen?.HandleFatalError(message); }

        private static void EnqueueWord(string word)
        {
            foreach (char c in word)
            {
                Entries.Enqueue(c);
            }
            Entries.Enqueue(CharCodes.ZC_RETURN);
        }

        public static readonly PooledQueue<zword> Entries = new();

        public static void Fail(string message) => Screen?.HandleFatalError(message);

        /////////////////////////////////////////////////////////////////////////////
        // Interface to the Frotz core
        /////////////////////////////////////////////////////////////////////////////

        /*
         * os_beep
         *
         * Play a beep sound. Ideally, the sound should be high- (number == 1)
         * or low-pitched (number == 2).
         *
         */
        public static void Beep(int number)
        {
            if (OperatingSystem.IsWindows())
            {
                if (number == 1)
                {
                    Console.Beep(800, 350);
                }
                else
                {
                    Console.Beep(392, 350);
                }
            }
        }

        /*
         * os_display_char
         *
         * Display a character of the current font using the current colours and
         * text style. The cursor moves to the next position. Printable codes are
         * all ASCII values from 32 to 126, ISO Latin-1 characters from 160 to
         * 255, ZC_GAP (gap between two sentences) and ZC_INDENT (paragraph
         * indentation), and Unicode characters above 255. The screen should not
         * be scrolled after printing to the bottom right corner.
         *
         */
        public static void DisplayChar(zword c)
        {
            if (c == CharCodes.ZC_INDENT)
            {
                DisplayChar(' ');
                DisplayChar(' ');
                DisplayChar(' ');
            }
            else if (c == CharCodes.ZC_GAP)
            {
                DisplayChar(' ');
                DisplayChar(' ');
            }
            else if (IsValidChar(c))
            {
                Screen?.DisplayChar((char)c);
            }
        }

        /*
         * os_display_string
         *
         * Pass a string of characters to os_display_char.
         *
         */
        public static void DisplayString(ReadOnlySpan<zword> chars)
        {
            zword c;

            for (int i = 0; i < chars.Length && chars[i] != 0; i++)
            {
                c = chars[i];
                if (c is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                {
                    int arg = chars[++i];
                    if (c == CharCodes.ZC_NEW_FONT)
                    {
                        SetFont(arg);
                    }
                    else if (c == CharCodes.ZC_NEW_STYLE)
                    {
                        SetTextStyle(arg);
                    }
                }
                else
                {
                    DisplayChar(c);
                }
            }
        }

        public static void DisplayString(ReadOnlySpan<char> s)
        {
            zword[]? pooled = null;
            int len = s.Length;
            Span<zword> word = len <= MaxStack ? stackalloc zword[len] : (pooled = ArrayPool<zword>.Shared.Rent(len));
            try
            {
                for (int i = 0; i < len; i++)
                {
                    word[i] = s[i];
                }
                DisplayString(word[..len]);
            }
            finally
            {
                if (pooled is object)
                    ArrayPool<zword>.Shared.Return(pooled);
            }
        }

        /*
         * os_erase_area
         *
         * Fill a rectangular area of the screen with the current background
         * colour. Top left coordinates are (1,1). The cursor does not move.
         *
         * The final argument gives the window being changed, -1 if only a
         * portion of a window is being erased, or -2 if the whole screen is
         * being erased.
         *
         */
        public static void EraseArea(int top, int left, int bottom, int right, int win)
        {
            if (win == -2)
            {
                Screen?.Clear();
            }
            else if (win == 1)
            {
                Screen?.ClearArea(top, left, bottom, right);
            }
            else
            {
                Screen?.ClearArea(top, left, bottom, right);
            }
        }

        /*
         * os_fatal
         *
         * Display error message and stop interpreter.
         *
         */
        [DoesNotReturn]
#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return.
        public static void Fatal(string s) => OnFatalError(s);
#pragma warning restore CS8763 // A method marked [DoesNotReturn] should not return.

        /*
         * os_font_data
         *
         * Return true if the given font is available. The font can be
         *
         *    TEXT_FONT
         *    PICTURE_FONT
         *    GRAPHICS_FONT
         *    FIXED_WIDTH_FONT
         *
         * The font size should be stored in "height" and "width". If
         * the given font is unavailable then these values must _not_
         * be changed.
         *
         */
        public static bool FontData(int font, ref zword height, ref zword width) =>
            Screen?.GetFontData(font, ref height, ref width) ?? false;

        /*
         * os_read_file_name
         *
         * Return the name of a file. Flag can be one of:
         *
         *    FILE_SAVE     - Save game file
         *    FILE_RESTORE  - Restore game file
         *    FILE_SCRIPT   - Transcript file
         *    FILE_RECORD   - Command file for recording
         *    FILE_PLAYBACK - Command file for playback
         *    FILE_SAVE_AUX - Save auxiliary ("preferred settings") file
         *    FILE_LOAD_AUX - Load auxiliary ("preferred settings") file
         *
         * The length of the file name is limited by MAX_FILE_NAME. Ideally
         * an interpreter should open a file requester to ask for the file
         * name. If it is unable to do that then this function should call
         * print_string and read_string to ask for a file name.
         *
         */
        public static bool ReadFileName([NotNullWhen(true)] out string? file_name, string default_name, FileTypes flag)
        {
            switch (flag)
            {
                case FileTypes.FILE_SAVE:
                    file_name = Screen?.OpenNewOrExistingFile(FastMem.SaveName, "Choose save game file", "Save Files (*.sav)|*.sav", ".sav");
                    break;
                case FileTypes.FILE_RESTORE:
                    file_name = Screen?.OpenExistingFile(FastMem.SaveName, "Choose save game to restore", "Save Files (*.sav)|*.sav");
                    break;
                case FileTypes.FILE_SCRIPT:
                    file_name = Screen?.OpenNewOrExistingFile(General.DEFAULT_SCRIPT_NAME, "Choose Script File", "Script File (*.scr)|*.scr", ".scr");
                    break;
                case FileTypes.FILE_RECORD:
                    file_name = Screen?.OpenNewOrExistingFile(default_name, "Choose File to Record To", "Record File(*.rec)|*.rec", ".rec");
                    break;
                case FileTypes.FILE_PLAYBACK:
                    file_name = Screen?.OpenExistingFile(default_name, "Choose File to playback from", "Record File(*.rec)|*.rec");
                    break;
                case FileTypes.FILE_SAVE_AUX:
                case FileTypes.FILE_LOAD_AUX:
                default:
                    Fail("Need to implement other types of files");
                    file_name = null;
                    break;
            }

            return file_name != null;
        }

        /*
         * os_init_screen
         *
         * Initialise the IO interface. Prepare screen and other devices
         * (mouse, sound card). Set various OS depending story file header
         * entries:
         *
         *     h_config (aka flags 1)
         *     h_flags (aka flags 2)
         *     h_screen_cols (aka screen width in characters)
         *     h_screen_rows (aka screen height in lines)
         *     h_screen_width
         *     h_screen_height
         *     h_font_height (defaults to 1)
         *     h_font_width (defaults to 1)
         *     h_default_foreground
         *     h_default_background
         *     h_interpreter_number
         *     h_interpreter_version
         *     h_user_name (optional; not used by any game)
         *
         * Finally, set reserve_mem to the amount of memory (in bytes) that
         * should not be used for multiple undo and reserved for later use.
         *
         */
        public static void InitScreen()
        {
            // TODO Really need to clean this up

            Main.h_interpreter_number = 4;

            // Set the configuration
            if (Main.h_version == ZMachine.V3)
            {
                Main.h_config |= ZMachine.CONFIG_SPLITSCREEN;
                Main.h_config |= ZMachine.CONFIG_PROPORTIONAL;
                // TODO Set Tandy bit here if appropriate
            }
            if (Main.h_version >= ZMachine.V4)
            {
                Main.h_config |= ZMachine.CONFIG_BOLDFACE;
                Main.h_config |= ZMachine.CONFIG_EMPHASIS;
                Main.h_config |= ZMachine.CONFIG_FIXED;
                Main.h_config |= ZMachine.CONFIG_TIMEDINPUT;
            }
            if (Main.h_version >= ZMachine.V5)
            {
                Main.h_config |= ZMachine.CONFIG_COLOUR;
            }
            if (Main.h_version == ZMachine.V6)
            {
                if (BlorbFile != null)
                {
                    Main.h_config |= ZMachine.CONFIG_PICTURES;
                    Main.h_config |= ZMachine.CONFIG_SOUND;
                }
            }
            //theApp.CopyUsername();

            Main.h_interpreter_version = (byte)'F';
            if (Main.h_version == ZMachine.V6)
            {
                Main.h_default_background = ZColor.BLACK_COLOUR;
                Main.h_default_foreground = ZColor.WHITE_COLOUR;
                // TODO Get the defaults from the application itself
            }
            else
            {
                Main.h_default_foreground = 1;
                Main.h_default_background = 1;
            }

            // Clear out the input queue incase a quit left characters
            Entries.Clear();

            // TODO Set font to be default fixed width font

            _metrics = Screen?.GetScreenMetrics() ?? default;
            Debug.WriteLine("Metrics: {0}:{1}", _metrics.WindowSize.Height, _metrics.WindowSize.Width);

            // TODO Make these numbers match the types (remove the casts)

            Main.h_screen_width = (zword)_metrics.WindowSize.Width;
            Main.h_screen_height = (zword)_metrics.WindowSize.Height;

            Main.h_screen_cols = (zbyte)_metrics.Columns;
            Main.h_screen_rows = (zbyte)_metrics.Rows;

            Main.h_font_width = (zbyte)_metrics.FontSize.Width;
            Main.h_font_height = (zbyte)_metrics.FontSize.Height;

            // Check for sound
            if ((Main.h_version == ZMachine.V3) && ((Main.h_flags & ZMachine.OLD_SOUND_FLAG) != 0))
            {
                // TODO Config sound here if appropriate
            }
            else if ((Main.h_version >= ZMachine.V4) && ((Main.h_flags & ZMachine.SOUND_FLAG) != 0))
            {
                // TODO Config sound here if appropriate
            }

            if (Main.h_version >= ZMachine.V5)
            {
                ushort mask = 0;
                if (Main.h_version == ZMachine.V6) mask |= ZMachine.TRANSPARENT_FLAG;

                // Mask out any unsupported bits in the extended flags
                Main.hx_flags &= mask;

                // TODO Set fore & back color here if apporpriate
                //  hx_fore_colour = 
                //  hx_back_colour = 
            }


            string name = Main.StoryName ?? "UNKNOWN";
            // Set default filenames

            FastMem.SaveName = $"{name}.sav";
            Files.ScriptName = $"{name}.log";
            Files.CommandName = $"{name}.rec";
            FastMem.AuxilaryName = $"{name}.aux";

            AddTestKeys();
        }

        /*
         * os_more_prompt
         *
         * Display a MORE prompt, wait for a keypress and remove the MORE
         * prompt from the screen.
         *
         */
        public static void MorePrompt()
        {
            if (Screen is null) return;

            DisplayString("[MORE]");
            Screen.RefreshScreen();

            while (Entries.Count == 0)
            {
                Thread.Sleep(100);
            }
            Entries.Dequeue();

            Screen.RemoveChars(6);
            Screen.RefreshScreen();
        }

        /*
         * os_process_arguments
         *
         * Handle command line switches. Some variables may be set to activate
         * special features of Frotz:
         *
         *     option_attribute_assignment
         *     option_attribute_testing
         *     option_context_lines
         *     option_object_locating
         *     option_object_movement
         *     option_left_margin
         *     option_right_margin
         *     option_ignore_errors
         *     option_piracy
         *     option_undo_slots
         *     option_expand_abbreviations
         *     option_script_cols
         *
         * The global pointer "story_name" is set to the story file name.
         *
         */
        public static bool ProcessArguments(ReadOnlySpan<string> args)
        {
            Main.StoryData?.Dispose();

            if (args.Length == 0)
            {
                var file = Screen?.SelectGameFile();
                if (!file.HasValue)
                    return false;

                (Main.StoryName, Main.StoryData) = file.GetValueOrDefault();
            }
            else
            {
                Main.StoryName = args[0];
                using var fs = new FileStream(args[0], FileMode.Open);
                var data = MemoryOwner<byte>.Allocate((int)fs.Length);
                fs.Read(data.Span);
                Main.StoryData = data;
            }

            Err.ErrorReportMode = ErrorCodes.ERR_REPORT_NEVER;

            //'
            //// Set default filenames
            //String filename = main.story_name;
            //var fi = new System.IO.FileInfo(main.story_name);
            //if (fi.Exists)
            //{
            //    String name = fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length);

            //    FastMem.SaveName = String.Format("{0}.sav", name);
            //    Files.ScriptName = String.Format("{0}.log", name);
            //    Files.CommandName = String.Format("{0}.rec", name);
            //    FastMem.AuxilaryName = String.Format("{0}.aux", name);
            //}

            return true;
        }

        /*
         * os_read_line
         *
         * Read a line of input from the keyboard into a buffer. The buffer
         * may already be primed with some text. In this case, the "initial"
         * text is already displayed on the screen. After the input action
         * is complete, the function returns with the terminating key value.
         * The length of the input should not exceed "max" characters plus
         * an extra 0 terminator.
         *
         * Terminating keys are the return key (13) and all function keys
         * (see the Specification of the Z-machine) which are accepted by
         * the is_terminator function. Mouse clicks behave like function
         * keys except that the mouse position is stored in global variables
         * "mouse_x" and "mouse_y" (top left coordinates are (1,1)).
         *
         * Furthermore, Frotz introduces some special terminating keys:
         *
         *     ZC_HKEY_PLAYBACK (Alt-P)
         *     ZC_HKEY_RECORD (Alt-R)
         *     ZC_HKEY_SEED (Alt-S)
         *     ZC_HKEY_UNDO (Alt-U)
         *     ZC_HKEY_RESTART (Alt-N, "new game")
         *     ZC_HKEY_QUIT (Alt-X, "exit game")
         *     ZC_HKEY_DEBUG (Alt-D)
         *     ZC_HKEY_HELP (Alt-H)
         *
         * If the timeout argument is not zero, the input gets interrupted
         * after timeout/10 seconds (and the return value is 0).
         *
         * The complete input line including the cursor must fit in "width"
         * screen units.
         *
         * The function may be called once again to continue after timeouts,
         * misplaced mouse clicks or hot keys. In this case the "continued"
         * flag will be set. This information can be useful if the interface
         * implements input line history.
         *
         * The screen is not scrolled after the return key was pressed. The
         * cursor is at the end of the input line when the function returns.
         *
         * Since Frotz 2.2 the helper function "completion" can be called
         * to implement word completion (similar to tcsh under Unix).
         *
         */
        public static zword ReadLine(int max, Span<zword> buf, int timeout, int width, bool continued)
        {
            if (Screen is null) ThrowHelper.ThrowInvalidOperationException("Screen has not been set.");

            //        ZC_SINGLE_CLICK || ZC_DOUBLE_CLICK

            //        case VK_DELETE:
            //        case VK_HOME:
            //        case VK_END:
            //        case VK_TAB:

            using var buffer = new PooledList<BufferChar>();

            Screen.RefreshScreen();

            buf.Clear();

            Screen.GetColor(out int foreground, out int background);
            Screen.SetInputColor();

            Screen.SetInputMode(true, true);

            var p = Screen.GetCursorPosition();

            try
            {
                while (true)
                {
                    if (Main.AbortGameLoop)
                    {
                        return CharCodes.ZC_RETURN;
                    }

                    while (Entries.Count == 0)
                    {
                        if (Main.AbortGameLoop)
                        {
                            return CharCodes.ZC_RETURN;
                        }

                        System.Threading.Thread.Sleep(10);
                    }
                    zword c = Entries.Dequeue();

                    switch (c)
                    {
                        case CharCodes.ZC_HKEY_HELP:
                        case CharCodes.ZC_HKEY_DEBUG:
                        case CharCodes.ZC_HKEY_PLAYBACK:
                        case CharCodes.ZC_HKEY_RECORD:

                        case CharCodes.ZC_HKEY_SEED:
                        case CharCodes.ZC_HKEY_UNDO:
                        case CharCodes.ZC_HKEY_RESTART:
                        case CharCodes.ZC_HKEY_QUIT:
                            return c;
                    }

                    if (c is CharCodes.ZC_SINGLE_CLICK or CharCodes.ZC_DOUBLE_CLICK)
                    {
                        // Just discard mouse clicks here
                        continue;
                    }
                    else if (c == CharCodes.ZC_ARROW_UP)
                    {
                        ClearInputAndShowHistory(1, buffer);
                    }
                    else if (c == CharCodes.ZC_ARROW_DOWN)
                    {
                        ClearInputAndShowHistory(-1, buffer);
                    }
                    else if (c == CharCodes.ZC_ARROW_LEFT)
                    {
                    }
                    else if (c == CharCodes.ZC_ARROW_RIGHT)
                    {
                    }
                    else if (c == CharCodes.ZC_RETURN)
                    {
                        using var sb = new ValueStringBuilder(buffer.Count);
                        foreach (var bc in buffer.Span)
                        {
                            sb.Append(bc);
                        }
                        History.Insert(0, sb.ToString());
                        HistoryPos = 0;
                        return CharCodes.ZC_RETURN;
                    }
                    else if (c == CharCodes.ZC_BACKSPACE)
                    {
                        if (buffer.Count > 0)
                        {
                            var bc = buffer[^1];
                            buffer.RemoveAt(^1);

                            p = p with { X = p.X - bc.Width };
                            Screen.SetCursorPosition(p.X, p.Y);

                            Screen.RemoveChars(1);
                            Screen.RefreshScreen();
                        }
                    }
                    else if (c == '\t')
                    {
                        HandleTabCompletion(buffer);
                    }
                    else
                    {
                        // buf[pos++] = c;

                        int w = Screen.GetStringWidth(((char)c).ToString(), new CharDisplayInfo(ZFont.TEXT_FONT, ZStyles.NORMAL_STYLE, -1, -1));
                        p = p with { X = p.X + w };
                        Screen.SetCursorPosition(p.X, p.Y);

                        buffer.Add(new(c, w));

                        Screen.AddInputChar((char)c);

                        Screen.RefreshScreen();
                    }
                }
            }
            finally
            {
                Screen.SetColor(foreground, background);
                Screen.SetInputMode(false, false);

                for (int i = 0; i < buffer.Count; i++)
                {
                    buf[i] = buffer[i].Char;
                }
            }
        }

        private static void HandleTabCompletion(PooledList<BufferChar> buffer)
        {
            Span<char> chars = buffer.Count > 0xff ? new char[buffer.Count] : stackalloc char[buffer.Count];

            for (int i = 0; i < buffer.Count; i++)
            {
                chars[i] = buffer[i];
            }

            int result = Text.Completion(chars, out string word);
            if (result == 0)
            {
                foreach (char c1 in word)
                {
                    Entries.Enqueue(c1);
                }
                Entries.Enqueue(' ');
            }
            else if (result == 1)
            {
                Beep(0);
            }
            else
            {
                Beep(1);
            }
        }

        private static void ClearInputAndShowHistory(int direction, IList<BufferChar> buffer)
        {
            if (direction > 0)
            {
                if (HistoryPos + direction > History.Count)
                {
                    Beep(0);
                    return;
                }
                HistoryPos++;
            }

            if (direction < 0)
            {
                if (HistoryPos + direction < 0)
                {
                    Beep(0);
                    return;
                }
                HistoryPos--;
            }

            // TODO Check if it's in bounds, and show history. If it would be out of bounds, beep!
            for (int i = 0; i < buffer.Count; i++)
            {
                Entries.Enqueue(CharCodes.ZC_BACKSPACE);
            }

            if (HistoryPos > 0)
            {
                string temp = History[HistoryPos - 1];
                foreach (char c1 in temp)
                {
                    Entries.Enqueue(c1);
                }
            }
        }

        private static bool SetCursorPositionCalled = false;
        private static ZPoint NewCursorPosition = (0, 0);

        /*
         * os_read_key
         *
         * Read a single character from the keyboard (or a mouse click) and
         * return it. Input aborts after timeout/10 seconds.
         *
         */
        public static zword ReadKey(int timeout, bool cursor)
        {
            if (Screen is null) ThrowHelper.ThrowInvalidOperationException("Screen has not been set.");

            Screen.RefreshScreen();

            Screen.SetInputMode(true, true);

            if (!SetCursorPositionCalled)
            {
                SetCursor(NewCursorPosition.Y, NewCursorPosition.X);
            }

            var (x, y) = Screen.GetCursorPosition();

            try
            {
                var sw = Stopwatch.StartNew();
                while (true)
                {
                    do
                    {
                        if (Main.AbortGameLoop)
                        {
                            return CharCodes.ZC_RETURN;
                        }

                        lock (Entries)
                        {
                            if (Entries.Count > 0)
                                break;
                            if (sw.Elapsed.TotalSeconds > timeout / 10 && timeout > 0)
                                return CharCodes.ZC_TIME_OUT;
                        }
                        Thread.Sleep(10);

                    } while (true);

                    lock (Entries)
                    {
                        SetCursorPositionCalled = false;
                        zword c = Entries.Dequeue();

                        int width = Screen.GetStringWidth(((char)c).ToString(),
                            new CharDisplayInfo(ZFont.FIXED_WIDTH_FONT, ZStyles.NORMAL_STYLE, 1, 1));
                        // _screen.SetCursorPosition(x + width, y);

                        NewCursorPosition = (x + width, y);

                        return c;
                    }
                }
            }
            finally
            {
                Screen.SetInputMode(false, false);
            }
        }

        /*
         * os_read_mouse
         *
         * Store the mouse position in the global variables "mouse_x" and
         * "mouse_y", the code of the last clicked menu in "menu_selected"
         * and return the mouse buttons currently pressed.
         *
         */
        internal static zword ReadMouse()
        {
            // return 0;
            OS.Fail("Need to implement mouse handling");

            return 0;
        }

        /*
         * os_menu
         *
         * Add to or remove a menu item. Action can be:
         *     MENU_NEW    - Add a new menu with the given title
         *     MENU_ADD    - Add a new menu item with the given text
         *     MENU_REMOVE - Remove the menu at the given index
         *
         */
        public static void Menu(int action, int menu, zword[] text) => Fail("os_menu not yet handled");


        /*
         * os_reset_screen
         *
         * Reset the screen before the program ends.
         *
         */
        public static void ResetScreen()
        {
            if (Screen is null) return;

            Screen.Clear();

            SetTextStyle(0);

            Screen.RefreshScreen();
        }

        /*
         * os_scroll_area
         *
         * Scroll a rectangular area of the screen up (units > 0) or down
         * (units < 0) and fill the empty space with the current background
         * colour. Top left coordinates are (1,1). The cursor stays put.
         *
         */
        public static void ScrollArea(int top, int left, int bottom, int right, int units)
        {
            if (Screen is null) return;

            // TODO This version can scroll better

            if (left > 1 || right < _metrics.Rows)
            {
                Screen.ScrollArea(top, bottom, left, right, units);
            }
            else
            {
                Debug.Assert(units > 0);
                Screen.ScrollLines(top, bottom - top + 1, units);
            }
        }

        /*
         * os_set_colour
         *
         * Set the foreground and background colours which can be:
         *
         *     1
         *     BLACK_COLOUR
         *     RED_COLOUR
         *     GREEN_COLOUR
         *     YELLOW_COLOUR
         *     BLUE_COLOUR
         *     MAGENTA_COLOUR
         *     CYAN_COLOUR
         *     WHITE_COLOUR
         *     TRANSPARENT_COLOUR
         *
         *     Amiga only:
         *
         *     LIGHTGREY_COLOUR
         *     MEDIUMGREY_COLOUR
         *     DARKGREY_COLOUR
         *
         * There may be more colours in the range from 16 to 255; see the
         * remarks about os_peek_colour.
         *
         */
        public static void SetColor(int new_foreground, int new_background) => Screen?.SetColor(new_foreground, new_background);

        /*
         * os_from_true_culour
         *
         * Given a true colour, return an appropriate colour index.
         *
         */
        public static zword FromTrueColor(zword colour) => TrueColorStuff.GetColourIndex(TrueColorStuff.RGB5ToTrue(colour));

        /*
         * os_to_true_colour
         *
         * Given a colour index, return the appropriate true colour.
         *
         */
        public static zword ToTrueColor(int index) => TrueColorStuff.TrueToRGB5(TrueColorStuff.GetColor(index));

        /*
         * os_set_cursor
         *
         * Place the text cursor at the given coordinates. Top left is (1,1).
         *
         */
        public static void SetCursor(int row, int col)
        {
            SetCursorPositionCalled = true;
            // TODO Need to migrate these variables to a better location
            Screen?.SetCursorPosition(col, row);
        }

        /*
         * os_set_font
         *
         * Set the font for text output. The interpreter takes care not to
         * choose fonts which aren't supported by the interface.
         *
         */
        public static void SetFont(int newFont) => Screen?.SetFont(newFont);

        /*
         * os_set_text_style
         *
         * Set the current text style. Following flags can be set:
         *
         *     REVERSE_STYLE
         *     BOLDFACE_STYLE
         *     EMPHASIS_STYLE (aka underline aka italics)
         *     FIXED_WIDTH_STYLE
         *
         */
        public static void SetTextStyle(int newStyle) => Screen?.SetTextStyle(newStyle);

        /*
         * os_string_width
         *
         * Calculate the length of a word in screen units. Apart from letters,
         * the word may contain special codes:
         *
         *    ZC_NEW_STYLE - next character is a new text style
         *    ZC_NEW_FONT  - next character is a new font
         *
         */
        public static int StringWidth(ReadOnlySpan<zword> s)
        {
            if (Screen is null) ThrowHelper.ThrowInvalidOperationException("Screen not initialized.");

            using var sb = new ValueStringBuilder();
            int font = -1;
            int style = -1;

            bool lateChange = false; // TODO This is testing code to determine if there are ever font changes mid word
            zword c;
            int width = 0;
            for (int i = 0; i < s.Length && s[i] != 0; i++)
            {
                c = s[i];
                if (c is CharCodes.ZC_NEW_FONT or CharCodes.ZC_NEW_STYLE)
                {
                    i++;
                    if (width == 0)
                    {
                        if (c == CharCodes.ZC_NEW_FONT) font = s[i];
                        if (c == CharCodes.ZC_NEW_STYLE) style = s[i];
                    }
                    else
                    {
                        lateChange = true;
                    }
                }
                else
                {
                    sb.Append((char)c);
                    width++;
                    if (lateChange == true)
                    {
                        // _screen.DisplayMessage("Characters after a late change!", "Message");
                    }
                }
            }

            return Screen.GetStringWidth(sb.ToString(), new CharDisplayInfo(font, style, 0, 0));
        }


        /*
         * os_char_width
         *
         * Return the length of the character in screen units.
         *
         */
        public static int CharWidth(zword c) => _metrics.FontSize.Width;

        /*
         * os_check_unicode
         *
         * Return with bit 0 set if the Unicode character can be
         * displayed, and bit 1 if it can be input.
         * 
         *
         */
        public static zword CheckUnicode(int font, zword c) =>
            // TODO Just return 1 since almost all modern fonts should be able to handle this
            1;

        /*
         * os_peek_color
         *
         * Return the color of the screen unit below the cursor. (If the
         * interface uses a text mode, it may return the background colour
         * of the character at the cursor position instead.) This is used
         * when text is printed on top of pictures. Note that this coulor
         * need not be in the standard set of Z-machine colours. To handle
         * this situation, Frotz entends the colour scheme: Colours above
         * 15 (and below 256) may be used by the interface to refer to non
         * standard colours. Of course, os_set_colour must be able to deal
         * with these colours.
         *
         */
        public static zword PeekColor()
        {
            if (Screen is null) ThrowHelper.ThrowInvalidOperationException("Screen has not been set.");
            return Screen.PeekColor();
        }

        /*
         * os_picture_data
         *
         * Return true if the given picture is available. If so, store the
         * picture width and height in the appropriate variables. Picture
         * number 0 is a special case: Write the highest legal picture number
         * and the picture file release number into the height and width
         * variables respectively when this picture number is asked for.
         *
         */
        public static bool PictureData(int picture, out int height, out int width)
        {
            if (BlorbFile != null)
            {
                if (picture == 0)
                {
                    height = -1;
                    width = -BlorbFile.ReleaseNumber;
                    foreach (int p in BlorbFile.Pictures.Keys)
                    {
                        if (p > height)
                        {
                            height = p;
                            width = BlorbFile.ReleaseNumber;
                        }
                    }

                    return true;
                }
                else
                {
                    byte[] buffer = BlorbFile.Pictures[picture].Image;
                    if (buffer.Length == 8)
                    {
                        // TODO This is a bit of a hack, it would be better to handle this upfront so there is no guess work
                        width = (int)ReadLong(buffer) * _metrics.Scale;
                        height = (int)ReadLong(buffer.AsSpan(4)) * _metrics.Scale;
                    }
                    else
                    {
                        if (Screen is null) ThrowHelper.ThrowInvalidOperationException("Screen has not been set.");
                        (height, width) = Screen.GetImageInfo(buffer);
                    }

                    return true;
                }
            }
            height = 0;
            width = 0;
            return false;
        }

        /*
         * os_draw_picture
         *
         * Display a picture at the given coordinates.
         *
         */
        public static void DrawPicture(int picture, int y, int x)
        {
            if (BlorbFile != null && BlorbFile.Pictures.ContainsKey(picture))
            {
                Screen?.DrawPicture(picture, BlorbFile.Pictures[picture].Image, y, x);
            }
        }

        /*
         * os_random_seed
         *
         * Return an appropriate random seed value in the range from 0 to
         * 32767, possibly by using the current system time.
         *
         */
        public static int RandomSeed()
        {
            if (DebugState.IsActive)
            {
                return DebugState.RandomSeed();
            }
            else
            {
                var r = new System.Random();
                return r.Next() & 32767;
            }
        }

        /*
         * os_restart_game
         *
         * This routine allows the interface to interfere with the process of
         * restarting a game at various stages:
         *
         *     RESTART_BEGIN - restart has just begun
         *     RESTART_WPROP_SET - window properties have been initialised
         *     RESTART_END - restart is complete
         *
         */
        public static void RestartGame(int stage)
        {
            // Show Beyond Zork's title screen
            if ((stage == ZMachine.RESTART_BEGIN) && (Main.StoryId == Story.BEYOND_ZORK))
            {
                if (OS.PictureData(1, out int _, out int _))
                {
                    OS.DrawPicture(1, 1, 1);
                    OS.ReadKey(0, false);
                }
            }
        }

        /*
         * os_path_open
         *
         * Open a file in the current directory.
         * -- Szurgot: Changed this to return a Memory stream, and also has Blorb Logic.. May need to refine
         * -- Changed this again to take a byte[] to allow the data to be loaded further up the chain
         */
        public static System.IO.Stream PathOpen(MemoryOwner<byte> story_data)
        {
            Guard.HasSizeGreaterThanOrEqualTo(story_data.Span, 4, nameof(story_data));

            if (story_data.Span[..4].SequenceEqual(General.FormBytes))
            {
                BlorbFile = Blorb.BlorbReader.ReadBlorbFile(story_data);

                return new MemoryStream(BlorbFile.ZCode);
            }
            else
            {
                string? temp = Path.ChangeExtension(Main.StoryName, "blb");
                BlorbFile = null;

                if (!string.IsNullOrEmpty(temp) && File.Exists(temp))
                {
                    using var fs = File.OpenRead(temp);
                    BlorbFile = Blorb.BlorbReader.ReadBlorbFile(fs);

                    return new MemoryStream(BlorbFile.ZCode);
                }

                return story_data.AsStream();
            }
        }

        /*
         * os_finish_with_sample
         *
         * Remove the current sample from memory (if any).
         *
         */
        public static void FinishWithSample(int number) => Screen?.FinishWithSample(number);

        /*
         * os_prepare_sample
         *
         * Load the given sample from the disk.
         *
         */
        public static void PrepareSample(int number) => Screen?.PrepareSample(number);

        /*
         * os_start_sample
         *
         * Play the given sample at the given volume (ranging from 1 to 8 and
         * 255 meaning a default volume). The sound is played once or several
         * times in the background (255 meaning forever). The end_of_sound
         * function is called as soon as the sound finishes, passing in the
         * eos argument.
         *
         */
        public static void StartSample(int number, int volume, int repeats, zword eos)
        {
            // TODO Refine this a little better to wait for the end and then trigger
            Screen?.StartSample(number, volume, repeats, eos);

            Sound.EndOfSound(eos);
        }

        /*
         * os_stop_sample
         *
         * Turn off the current sample.
         *
         */
        public static void StopSample(int number) => Screen?.StopSample(number);

        /*
         * os_scrollback_char
         *
         * Write a character to the scrollback buffer.
         *
         */
        public static void ScrollbackChar(zword c)
        {
            // TODO Implement scrollback
        }

        /*
         * os_scrollback_erase
         *
         * Remove characters from the scrollback buffer.
         *
         */
        public static void ScrollbackErase(int erase)
        {
            // TODO Implement scrollback
        }

        /*
         * os_tick
         *
         * Called after each opcode.
         *
         */
        private static int osTickCount = 0;
        public static void Tick()
        {
            // Check for completed sounds
            if (++osTickCount > 1000)
            {
                osTickCount = 0;
                // TODO Implement sound at some point :)
            }
        }

        /*
         * os_buffer_screen
         *
         * Set the screen buffering mode, and return the previous mode.
         * Possible values for mode are:
         *
         *     0 - update the display to reflect changes when possible
         *     1 - do not update the display
         *    -1 - redraw the screen, do not change the mode
         *
         */
        public static int BufferScreen(int mode)
        {
            Fail("os_buffer_screen is not yet implemented");
            return 0;
        }

        /*
         * os_wrap_window
         *
         * Return non-zero if the window should have text wrapped.
         *
         */
        public static int WrapWindow(int win)
        {
            bool shouldWrap = Screen?.ShouldWrap() ?? false;
            return shouldWrap ? 1 : 0;
        }

        /*
         * os_window_height
         *
         * Called when the height of a window is changed.
         *
         */
        public static void SetWindowSize(int win, ZWindow wp)
            => Screen?.SetWindowSize(win, wp.YPos, wp.XPos, wp.YSize, wp.XSize);

        /*
         * set_active_window
         * Called to set the output window (I hope)
         * 
         */
        public static void SetActiveWindow(int win)
        {
            Debug.WriteLine("Setting Window:" + win);
            Screen?.SetActiveWindow(win);
        }

        // New Variables go here //
        private static ScreenMetrics _metrics;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidChar(zword c)
        {
            if (c is >= CharCodes.ZC_ASCII_MIN and <= CharCodes.ZC_ASCII_MAX)
                return true;
            if (c is >= CharCodes.ZC_LATIN1_MIN and <= CharCodes.ZC_LATIN1_MAX)
                return true;
            return c >= 0x100;
        }

        public static void GameStarted()
        {
            if (Screen is null || Main.StoryName is null)
                ThrowHelper.ThrowInvalidOperationException("Game not properly initialized.");

            Screen.StoryStarted(Main.StoryName, BlorbFile);
        }

        public static void MouseMoved(int x, int y)
        {
            Main.MouseX = (zword)x;
            Main.MouseY = (zword)y;
        }
    }

    internal record struct BufferChar(zword Char, int Width)
    {
        public static implicit operator char(BufferChar bc) => (char)bc.Char;
    }
}