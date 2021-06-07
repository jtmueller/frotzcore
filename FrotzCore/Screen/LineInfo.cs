using Collections.Pooled;
using Microsoft.Toolkit.Diagnostics;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections.Generic;

namespace Frotz.Screen
{
    public class LineInfo : IDisposable
    {
        private readonly MemoryOwner<char> _chars;
        private readonly MemoryOwner<CharDisplayInfo> _styles;
        private readonly object _lockObj = new();
        private PooledList<FontChanges>? _changes;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; }
        public int LastCharSet { get; private set; }

        public LineInfo(int lineWidth)
        {
            _chars = MemoryOwner<char>.Allocate(lineWidth);
            _styles = MemoryOwner<CharDisplayInfo>.Allocate(lineWidth);

            _chars.Span.Fill(' ');
            _styles.Span.Fill(default);

            Width = lineWidth;

            LastCharSet = -1;
        }

        public void SetChar(int pos, char c, CharDisplayInfo FandS = default)
        {
            if ((uint)pos >= (uint)Width)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(pos));

            lock (_lockObj)
            {
                _chars.Span[pos] = c;
                _styles.Span[pos] = FandS;
                LastCharSet = Math.Max(pos, LastCharSet);

                _changes?.Dispose();
                _changes = null;
            }
        }

        public void SetChars(int pos, ReadOnlySpan<char> chars, CharDisplayInfo FandS = default)
        {
            if ((uint)pos >= (uint)Width)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(pos));

            if ((uint)pos + chars.Length >= (uint)Width)
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(chars), "Too many chars to fit in line.");

            lock (_lockObj)
            {
                chars.CopyTo(_chars.Span[pos..]);
                _styles.Span[pos..].Fill(FandS);
                LastCharSet = Math.Max(pos + chars.Length, LastCharSet);

                _changes?.Dispose();
                _changes = null;
            }
        }

        public void AddChar(char c, CharDisplayInfo FandS) => SetChar(++LastCharSet, c, FandS);

        public void ClearLine()
        {
            lock (_lockObj)
            {
                for (int i = 0; i < Width; i++)
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

        public ReadOnlySpan<char> CurrentChars => _chars.Span[..(LastCharSet + 1)];

        public void Replace(int start, ReadOnlySpan<char> newString)
        {
            SetChars(start, newString);
        }

        public IReadOnlyList<FontChanges> GetTextWithFontInfo()
        {
            if (_changes == null)
            {
                lock (_lockObj)
                {
                    if (_changes == null)
                    {
                        _changes = new PooledList<FontChanges>(Width);
                        var chars = CurrentChars;

                        var fc = new FontChanges(-1, 0, new CharDisplayInfo(-1, 0, 0, 0));
                        var styles = _styles.Span;
                        for (int i = 0; i < Width; i++)
                        {
                            if (!styles[i].Equals(fc.FontAndStyle))
                            {
                                fc = new FontChanges(i, Width, styles[i]);
                                fc.AddChar(chars[i]);
                                _changes.Add(fc);
                            }
                            else
                            {
                                fc.AddChar(chars[i]);
                            }
                        }
                    }
                }
            }

            return _changes;
        }

        public ReadOnlySpan<char> GetChars() => _chars.Span[..Width];

        public ReadOnlySpan<char> GetChars(int start, int length) => _chars.Span.Slice(start, length);

        public override string ToString() => GetChars().ToString();

        public CharDisplayInfo GetFontAndStyle(int column) => _styles.Span[column];

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _styles.Dispose();
            _chars.Dispose();
            _changes?.Dispose();
        }
    }
}
