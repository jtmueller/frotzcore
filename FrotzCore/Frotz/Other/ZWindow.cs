using zword = System.UInt16;

namespace Frotz.Other
{
    public class ZWindow // TODO I'd like to make this private again
    { // Making this a class so pointers will work
        public zword YPos;
        public zword XPos;
        public zword YSize;
        public zword XSize;
        public zword y_cursor;
        public zword x_cursor;
        public zword left;
        public zword right;
        public zword nl_routine;
        public zword nl_countdown;
        public zword style;
        public zword colour;
        public zword font;
        public zword font_size;
        public zword attribute;
        public zword line_count;
        public zword true_fore;
        public zword true_back;
        public int index; // szurgot

        public zword this[int index]
        {
            get => index switch
            {
                0 => YPos,
                1 => XPos,
                2 => YSize,
                3 => XSize,
                4 => y_cursor,
                5 => x_cursor,
                6 => left,
                7 => right,
                8 => nl_routine,
                9 => nl_countdown,
                10 => style,
                11 => colour,
                12 => font,
                13 => font_size,
                14 => attribute,
                15 => line_count,
                16 => true_fore,
                17 => true_back,
                _ => 0,
            };

            set
            {
                switch (index)
                {
                    case 0: YPos = value; break;
                    case 1: XPos = value; break;
                    case 2: YSize = value; break;
                    case 3: XSize = value; break;
                    case 4: y_cursor = value; break;
                    case 5: x_cursor = value; break;
                    case 6: left = value; break;
                    case 7: right = value; break;
                    case 8: nl_routine = value; break;
                    case 9: nl_countdown = value; break;
                    case 10: style = value; break;
                    case 11: colour = value; break;
                    case 12: font = value; break;
                    case 13: font_size = value; break;
                    case 14: attribute = value; break;
                    case 15: line_count = value; break;
                    case 16: true_fore = value; break;
                    case 17: true_back = value; break;
                }
            }
        }

        public override string ToString() 
            => $"Window: {index} Pos: {XPos}:{YPos} Size: {XSize}:{YSize} Cursor:{x_cursor}:{y_cursor}";
    }
}