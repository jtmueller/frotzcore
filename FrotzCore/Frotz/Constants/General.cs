namespace Frotz.Constants
{
    // typedef unsigned char zbyte;
    // typedef unsigned short zword;

    public static class General
    {
        /*** Constants that may be set at compile time ***/
        public const int TEXT_BUFFER_SIZE = 2000;
        public const int MAX_FILE_NAME = 256;
        public const int INPUT_BUFFER_SIZE = 200;
        public const int STACK_SIZE = 32768;
        
        public const int MAX_UNDO_SLOTS = 500;
        
        public const string DEFAULT_SAVE_NAME = "story.sav";
        public const string DEFAULT_SCRIPT_NAME = "story.scr";
        public const string DEFAULT_COMMAND_NAME = "story.rec";
        public const string DEFAULT_AUXILARY_NAME = "story.aux";
        
        public const string DEFAULT_SAVE_DIR = ".frotz-saves";
    }
}