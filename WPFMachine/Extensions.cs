using System;
using System.Windows.Controls;

namespace WPFMachine
{
    internal static class Extensions
    {
        public static void RemoveAt(this UIElementCollection collection, Index index)
            => collection.RemoveAt(index.GetOffset(collection.Count));

        public static void RemoveRange(this UIElementCollection collection, Range range)
        {
            var (offset, len) = range.GetOffsetAndLength(collection.Count);
            collection.RemoveRange(offset, len);
        }
    }
}
