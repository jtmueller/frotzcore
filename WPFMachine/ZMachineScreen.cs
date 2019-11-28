using Frotz.Screen;
using System;

namespace WPFMachine
{
    internal interface IZMachineScreen
    {
        void AddInput(char inputKeyPressed); // TODO This could be named better
        void SetCharsAndLines();
        ScreenMetrics Metrics { get; }
        void SetFontInfo();
        void Focus();
        event EventHandler<GameSelectedEventArgs> GameSelected;
        void Reset();
    }
}
