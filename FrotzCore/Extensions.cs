
using System;
using System.Text;

namespace Frotz
{
    public static class Extensions
    {
        public static StringBuilder Remove(this StringBuilder sb, Range range)
        {
            var (offset, length) = range.GetOffsetAndLength(sb.Length);
            sb.Remove(offset, length);
            return sb;
        }
    }
}
