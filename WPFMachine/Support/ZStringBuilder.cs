using System;
using System.Text;

namespace WPFMachine.Support
{
    public class ZStringBuilder
    {
        private readonly StringBuilder _builder;

        public ZStringBuilder()
        {
            _builder = new StringBuilder();
        }

        public void Append(char c)
        {
            if (_builder.Length < _currentPosition + 1)
            {
                _builder.Length = _currentPosition + 1;
            }
            _builder[_currentPosition++] = c;
        }

        public void Clear()
        {
            _builder.Clear();
            _currentPosition = 0;
        }

        public int Length => _builder.Length;

        public void Remove(int startIndex, int length) => _builder.Remove(startIndex, length);

        public override string ToString() => _builder.ToString();

        public void SetCurrentPosition(int position)
        {
            if ((uint)position > (uint)_builder.Length)
                throw new ArgumentOutOfRangeException(nameof(position));

            _currentPosition = position;
        }

        private int _currentPosition = 0;
    }
}
