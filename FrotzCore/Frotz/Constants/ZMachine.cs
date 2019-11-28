
using zword = System.UInt16;

namespace Frotz.Constants
{
    public enum FileTypes : byte
    {
        /*** File types ***/

        FILE_RESTORE = 0,
        FILE_SAVE = 1,
        FILE_SCRIPT = 2,
        FILE_PLAYBACK = 3,
        FILE_RECORD = 4,
        FILE_LOAD_AUX = 5,
        FILE_SAVE_AUX = 6,
    }

    public enum Story
    {
        BEYOND_ZORK,
        SHERLOCK,
        ZORK_ZERO,
        SHOGUN,
        ARTHUR,
        JOURNEY,
        LURKING_HORROR,
        AMFV,
        UNKNOWN
    }

    public class CharCodes
    {
        /*** Character codes ***/
        public const zword ZC_TIME_OUT = 0x00;
        public const zword ZC_NEW_STYLE = 0x01;
        public const zword ZC_NEW_FONT = 0x02;
        public const zword ZC_BACKSPACE = 0x08;
        public const zword ZC_INDENT = 0x09;
        public const zword ZC_GAP = 0x0b;
        public const zword ZC_RETURN = 0x0d;
        public const zword ZC_HKEY_MIN = 0x0e;
        public const zword ZC_HKEY_RECORD = 0x0e;
        public const zword ZC_HKEY_PLAYBACK = 0x0f;
        public const zword ZC_HKEY_SEED = 0x10;
        public const zword ZC_HKEY_UNDO = 0x11;
        public const zword ZC_HKEY_RESTART = 0x12;
        public const zword ZC_HKEY_QUIT = 0x13;
        public const zword ZC_HKEY_DEBUG = 0x14;
        public const zword ZC_HKEY_HELP = 0x15;
        public const zword ZC_HKEY_MAX = 0x15;
        public const zword ZC_ESCAPE = 0x1b;
        public const zword ZC_ASCII_MIN = 0x20;
        public const zword ZC_ASCII_MAX = 0x7e;
        public const zword ZC_BAD = 0x7f;
        public const zword ZC_ARROW_MIN = 0x81;
        public const zword ZC_ARROW_UP = 0x81;
        public const zword ZC_ARROW_DOWN = 0x82;
        public const zword ZC_ARROW_LEFT = 0x83;
        public const zword ZC_ARROW_RIGHT = 0x84;
        public const zword ZC_ARROW_MAX = 0x84;
        public const zword ZC_FKEY_MIN = 0x85;
        public const zword ZC_FKEY_MAX = 0x90;
        public const zword ZC_NUMPAD_MIN = 0x91;
        public const zword ZC_NUMPAD_MAX = 0x9a;
        public const zword ZC_SINGLE_CLICK = 0x9b;
        public const zword ZC_DOUBLE_CLICK = 0x9c;
        public const zword ZC_MENU_CLICK = 0x9d;
        public const zword ZC_LATIN1_MIN = 0xa0;
        public const zword ZC_LATIN1_MAX = 0xff;
    }

    public class ZColor
    {
        public const byte BLACK_COLOUR = 2;
        public const byte RED_COLOUR = 3;
        public const byte GREEN_COLOUR = 4;
        public const byte YELLOW_COLOUR = 5;
        public const byte BLUE_COLOUR = 6;
        public const byte MAGENTA_COLOUR = 7;
        public const byte CYAN_COLOUR = 8;
        public const byte WHITE_COLOUR = 9;
        public const byte GREY_COLOUR = 10;		/* INTERP_MSDOS only */
        public const byte LIGHTGREY_COLOUR = 10; 	/* INTERP_AMIGA only */
        public const byte MEDIUMGREY_COLOUR = 11; 	/* INTERP_AMIGA only */
        public const byte DARKGREY_COLOUR = 12; 	/* INTERP_AMIGA only */
        public const byte TRANSPARENT_COLOUR = 15; /* ZSpec 1.1 */
    }

    public class ZFont
    {
        public const byte TEXT_FONT = 1;
        public const byte PICTURE_FONT = 2;
        public const byte GRAPHICS_FONT = 3;
        public const byte FIXED_WIDTH_FONT = 4;
    }

    public class ZStyles
    {
        public const byte NORMAL_STYLE = 0; // szurgot
        public const byte REVERSE_STYLE = 1;
        public const byte BOLDFACE_STYLE = 2;
        public const byte EMPHASIS_STYLE = 4;
        public const byte FIXED_WIDTH_STYLE = 8;
    }

    public static class ZMachine
    {
        /*** Story file header format ***/

        public const byte H_VERSION = 0;
        public const byte H_CONFIG = 1;
        public const byte H_RELEASE = 2;
        public const byte H_RESIDENT_SIZE = 4;
        public const byte H_START_PC = 6;
        public const byte H_DICTIONARY = 8;
        public const byte H_OBJECTS = 10;
        public const byte H_GLOBALS = 12;
        public const byte H_DYNAMIC_SIZE = 14;
        public const byte H_FLAGS = 16;
        public const byte H_SERIAL = 18;
        public const byte H_ABBREVIATIONS = 24;
        public const byte H_FILE_SIZE = 26;
        public const byte H_CHECKSUM = 28;
        public const byte H_INTERPRETER_NUMBER = 30;
        public const byte H_INTERPRETER_VERSION = 31;
        public const byte H_SCREEN_ROWS = 32;
        public const byte H_SCREEN_COLS = 33;
        public const byte H_SCREEN_WIDTH = 34;
        public const byte H_SCREEN_HEIGHT = 36;
        public const byte H_FONT_HEIGHT = 38; /* this is the font width in V5 */
        public const byte H_FONT_WIDTH = 39; /* this is the font height in V5 */
        public const byte H_FUNCTIONS_OFFSET = 40;
        public const byte H_STRINGS_OFFSET = 42;
        public const byte H_DEFAULT_BACKGROUND = 44;
        public const byte H_DEFAULT_FOREGROUND = 45;
        public const byte H_TERMINATING_KEYS = 46;
        public const byte H_LINE_WIDTH = 48;
        public const byte H_STANDARD_HIGH = 50;
        public const byte H_STANDARD_LOW = 51;
        public const byte H_ALPHABET = 52;
        public const byte H_EXTENSION_TABLE = 54;
        public const byte H_USER_NAME = 56;

        public const byte HX_TABLE_SIZE = 0;
        public const byte HX_MOUSE_X = 1;
        public const byte HX_MOUSE_Y = 2;
        public const byte HX_UNICODE_TABLE = 3;
        public const byte HX_FLAGS = 4;
        public const byte HX_FORE_COLOUR = 5;
        public const byte HX_BACK_COLOUR = 6;

        /*** Various Z-machine constants ***/

        public const byte V1 = 1;
        public const byte V2 = 2;
        public const byte V3 = 3;
        public const byte V4 = 4;
        public const byte V5 = 5;
        public const byte V6 = 6;
        public const byte V7 = 7;
        public const byte V8 = 8;

        public const byte CONFIG_BYTE_SWAPPED = 0x01; /* Story file is byte swapped         - V3  */
        public const byte CONFIG_TIME = 0x02; /* Status line displays time          - V3  */
        public const byte CONFIG_TWODISKS = 0x04; /* Story file occupied two disks      - V3  */
        public const byte CONFIG_TANDY = 0x08; /* Tandy licensed game                - V3  */
        public const byte CONFIG_NOSTATUSLINE = 0x10; /* Interpr can't support status lines - V3  */
        public const byte CONFIG_SPLITSCREEN = 0x20; /* Interpr supports split screen mode - V3  */
        public const byte CONFIG_PROPORTIONAL = 0x40; /* Interpr uses proportional font     - V3  */

        public const byte CONFIG_COLOUR = 0x01; /* Interpr supports colour            - V5+ */
        public const byte CONFIG_PICTURES = 0x02; /* Interpr supports pictures	       - V6  */
        public const byte CONFIG_BOLDFACE = 0x04; /* Interpr supports boldface style    - V4+ */
        public const byte CONFIG_EMPHASIS = 0x08; /* Interpr supports emphasis style    - V4+ */
        public const byte CONFIG_FIXED = 0x10; /* Interpr supports fixed width style - V4+ */
        public const byte CONFIG_SOUND = 0x20; /* Interpr supports sound             - V6  */
        public const byte CONFIG_TIMEDINPUT = 0x80; /* Interpr supports timed input       - V4+ */

        public const zword SCRIPTING_FLAG = 0x0001; /* Outputting to transscription file  - V1+ */
        public const byte FIXED_FONT_FLAG = 0x0002; /* Use fixed width font               - V3+ */
        public const byte REFRESH_FLAG = 0x0004; /* Refresh the screen                 - V6  */
        public const byte GRAPHICS_FLAG = 0x0008; /* Game wants to use graphics         - V5+ */
        public const byte OLD_SOUND_FLAG = 0x0010; /* Game wants to use sound effects    - V3  */
        public const byte UNDO_FLAG = 0x0010; /* Game wants to use UNDO feature     - V5+ */
        public const byte MOUSE_FLAG = 0x0020; /* Game wants to use a mouse          - V5+ */
        public const byte COLOUR_FLAG = 0x0040; /* Game wants to use colours          - V5+ */
        public const byte SOUND_FLAG = 0x0080; /* Game wants to use sound effects    - V5+ */
        public const ushort MENU_FLAG = 0x0100; /* Game wants to use menus            - V6  */

        public const byte TRANSPARENT_FLAG = 0x0001; /* Game wants to use transparency     - V6  */

        public const byte INTERP_DEC_20 = 1;
        public const byte INTERP_APPLE_IIE = 2;
        public const byte INTERP_MACINTOSH = 3;
        public const byte INTERP_AMIGA = 4;
        public const byte INTERP_ATARI_ST = 5;
        public const byte INTERP_MSDOS = 6;
        public const byte INTERP_CBM_128 = 7;
        public const byte INTERP_CBM_64 = 8;
        public const byte INTERP_APPLE_IIC = 9;
        public const byte INTERP_APPLE_IIGS = 10;
        public const byte INTERP_TANDY = 11;

        /*** Constants for os_restart_game */

        public static byte RESTART_BEGIN = 0;
        public static byte RESTART_WPROP_SET = 1;
        public static byte RESTART_END = 2;

        /*** Constants for os_menu */

        public static byte MENU_NEW = 0;
        public static byte MENU_ADD = 1;
        public static byte MENU_REMOVE = 2;


    }
}
