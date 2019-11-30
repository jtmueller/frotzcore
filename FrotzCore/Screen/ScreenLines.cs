using Collections.Pooled;
using System;
using System.Collections.Generic;
using System.Text;

namespace Frotz.Screen
{
    public class ScreenLines : IDisposable
    {
        private readonly PooledList<LineInfo> _lines;

        public ScreenLines(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;

            _lines = new PooledList<LineInfo>(rows);
            for (int i = 0; i < rows; i++)
            {
                _lines.Add(new LineInfo(columns * 3));
            }
        }

        public int Rows { get; }
        public int Columns { get; }

        public void SetChar(int row, int col, char c, CharDisplayInfo FandS)
        {
            // TODO Check boundaries
            _lines[row].SetChar(col, c, FandS);
        }

        public void ScrollLines(int top, int numlines)
        {
            // TODO Check boundaries
            for (int i = 0; i < numlines; i++)
            {
                if (_lines.Count > 0)
                {
                    if (_lines.Count > top)
                    {
                        _lines.RemoveAt(top);
                    }
                    else
                    {
                        _lines.RemoveAt(0);
                    }
                }
            }

            // TODO Fix up this so I don't have to add lines back in
            AddLines();
        }

        public void ScrollArea(int top, int bottom, int left, int right, int units)
        {
            // TODO Do something with units
            // TODO Check Boundaries
            int numchars = right - left + 1;
            Span<char> replace = numchars > 0xff ? new char[numchars] : stackalloc char[numchars];
            Span<char> temp = numchars > 0xff ? new char[numchars] : stackalloc char[numchars];
            replace.Fill(' ');

            for (int i = bottom - 1; i >= top; i--)
            {
                _lines[i].GetChars(left, numchars).CopyTo(temp);
                _lines[i].Replace(left, replace);
                temp.CopyTo(replace);
            }
        }

        public void Clear() => ClearArea(0, 0, Rows, Columns * 3);

        public void ClearArea(int top, int left, int bottom, int right)
        {
            // TODO Check this boundary
            for (int i = top; i < bottom; i++)
            {
                for (int j = left; j < right; j++)
                {
                    _lines[i].ClearChar(j);
                }
            }
        }

        private void AddLines()
        {
            while (_lines.Count <= Rows * 2)
            {
                lock (_lines)
                {
                    _lines.Add(new LineInfo(Columns * 3));
                }
            }
        }

        public void RemoveChars(int row, int col, int count)
        {
            // TODO Check this boundary
            _lines[row].RemoveChars(count);
        }

        public string GetText(out List<FontChanges> changes) => GetTextToLine(Rows, out changes);

        public ReadOnlySpan<LineInfo> GetLines() => _lines.Span;

        public string GetTextToLine(int line, out List<FontChanges> changes)
        {
            int pos = 0;

            changes = new List<FontChanges>();
            var sb = new StringBuilder();
            for (int i = 0; i < line; i++)
            {
                sb.Append(_lines[i].GetChars());
                sb.Append(Environment.NewLine);

                // Start col needs to stay per line, and there needs to be pos offset per line
                var tempChanges = _lines[i].GetTextWithFontInfo();
                foreach (var c in tempChanges)
                {
                    c.Offset += pos;
                    c.Line = i;
                }

                changes.AddRange(tempChanges);

                pos = sb.Length;
            }

            return sb.ToString();
        }

        public ReadOnlySpan<char> GetTextAtLine(int line) => _lines[line].GetChars();

        public CharDisplayInfo GetFontAndStyle(int row, int col) => _lines[row].GetFontAndStyle(col);

        public void Dispose()
        {
            if (_lines != null)
            {
                _lines.ForEach(x => x?.Dispose());
                _lines.Dispose();
            }
        }
    }
}
