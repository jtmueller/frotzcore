using Collections.Pooled;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Frotz.Screen
{
    public class LineInfo : IDisposable
    {
        private char[] _chars;
        private CharDisplayInfo[] _styles;
        private readonly int _width;
        private readonly object _lockObj = new object();
        private PooledList<FontChanges>? _changes;

        public int X { get; set; }
        public int Y { get; set; }
        public int LastCharSet { get; private set; }

        public LineInfo(int lineWidth)
        {
            _chars = ArrayPool<char>.Shared.Rent(lineWidth);
            _styles = ArrayPool<CharDisplayInfo>.Shared.Rent(lineWidth);

            for (int i = 0; i < lineWidth; i++)
            {
                _chars[i] = ' ';
                _styles[i] = default;
            }

            _width = lineWidth;

            LastCharSet = -1;
        }

        public void SetChar(int pos, char c, CharDisplayInfo FandS = default)
        {
            if ((uint)pos >= (uint)_width)
                throw new IndexOutOfRangeException(nameof(pos));

            lock (_lockObj)
            {
                _chars[pos] = c;
                _styles[pos] = FandS;
                LastCharSet = Math.Max(pos, LastCharSet);

                _changes = null;
            }
        }

        public void AddChar(char c, CharDisplayInfo FandS) => SetChar(++LastCharSet, c, FandS);

        public void ClearLine()
        {
            lock (_lockObj)
            {
                for (int i = 0; i < _width; i++)
                {
                    ClearChar(i);
                }
                LastCharSet = -1;
            }
        }

        public void RemoveChars(int count)
        {
            lock (_lockObj)
            {
                LastCharSet -= count;
            }
        }

        public void ClearChar(int pos) => SetChar(pos, ' ');

        public ReadOnlySpan<char> CurrentChars => _chars.AsSpan(0, LastCharSet + 1);

        public void Replace(int start, ReadOnlySpan<char> newString)
        {
            lock (_lockObj)
            {
                for (int i = 0; i < newString.Length; i++)
                {
                    SetChar(start + i, newString[i]);
                }
            }
        }

        public IReadOnlyList<FontChanges> GetTextWithFontInfo()
        {
            if (_changes == null)
            {
                lock (_lockObj)
                {
                    if (_changes == null)
                    {
                        _changes = new PooledList<FontChanges>(_width);
                        var chars = CurrentChars;

                        var fc = new FontChanges(-1, 0, new CharDisplayInfo(-1, 0, 0, 0));
                        for (int i = 0; i < _width; i++)
                        {
                            if (!_styles[i].Equals(fc.FontAndStyle))
                            {
                                fc = new FontChanges(i, 1, _styles[i]);
                                fc.AddChar(chars[i]);
                                _changes.Add(fc);
                            }
                            else
                            {
                                fc.Count++;
                                fc.AddChar(chars[i]);
                            }
                        }
                    }
                }
            }

            return _changes;
        }

        public ReadOnlySpan<char> GetChars() => _chars.AsSpan(0, _width);

        public ReadOnlySpan<char> GetChars(int start, int length) => _chars.AsSpan(start, length);

        public override string ToString() => GetChars().ToString();

        public CharDisplayInfo GetFontAndStyle(int column) => _styles[column];

        public void Dispose()
        {
            if (_styles.Length > 0)
            {
                ArrayPool<CharDisplayInfo>.Shared.Return(_styles);
                _styles = Array.Empty<CharDisplayInfo>();
            }

            if (_chars.Length > 0)
            {
                ArrayPool<char>.Shared.Return(_chars);
                _chars = Array.Empty<char>();
            }

            _changes?.Dispose();
        }
    }
}
