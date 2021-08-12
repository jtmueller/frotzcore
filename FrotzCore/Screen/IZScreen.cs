namespace Frotz.Screen;

using System.Diagnostics.CodeAnalysis;
using zword = System.UInt16;

public interface IZScreen
{
    [DoesNotReturn]
    void HandleFatalError(string message);
    ScreenMetrics GetScreenMetrics();
    void DisplayChar(char c);
    void RefreshScreen(); // TODO Need to make this a little different
    void SetCursorPosition(int x, int y);
    void ScrollLines(int top, int height, int lines);
    event EventHandler<ZKeyPressEventArgs> KeyPressed;
    void SetTextStyle(int new_style);
    void Clear();
    void ClearArea(int top, int left, int bottom, int right);

    string OpenExistingFile(string defaultName, string title, string filter);
    string OpenNewOrExistingFile(string defaultName, string title, string filter, string defaultExtension);

    (string FileName, MemoryOwner<byte> FileData)? SelectGameFile();

    ZSize GetImageInfo(byte[] image);

    void ScrollArea(int top, int bottom, int left, int right, int units);

    void DrawPicture(int picture, byte[] image, int y, int x);

    void SetFont(int font);

    void DisplayMessage(string message, string caption);

    int GetStringWidth(string s, CharDisplayInfo font);

    void RemoveChars(int count);

    bool GetFontData(int font, ref zword height, ref zword width);

    void GetColor(out int foreground, out int background);
    void SetColor(int new_foreground, int new_background);

    zword PeekColor();

    void FinishWithSample(int number);
    void PrepareSample(int number);
    void StartSample(int number, int volume, int repeats, zword eos);
    void StopSample(int number);

    void SetInputMode(bool inputMode, bool cursorVisibility);

    void SetInputColor();
    void AddInputChar(char c);

    void StoryStarted(string storyName, Blorb.Blorb? blorbFile);

    ZPoint GetCursorPosition();

    void SetActiveWindow(int win);
    void SetWindowSize(int win, int top, int left, int height, int width);

    bool ShouldWrap();

}
