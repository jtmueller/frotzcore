using System.IO;
using System.Windows.Controls;

namespace WPFMachine;

public class FrotzSound : UserControl
{
    private readonly MediaElement _element;

    public FrotzSound()
    {
        _element = new MediaElement
        {
            LoadedBehavior = MediaState.Manual
        };
        Content = _element;
    }

    public void LoadSound(Span<byte> sound)
    {
        _element.Source = null;

        string tempFile = Path.GetTempFileName();
        using var fs = File.Create(tempFile);
        fs.Write(sound);

        _element.Source = new Uri("file:///" + tempFile);
    }

    public void PlaySound() => _element.Play();

    public void StopSound() => _element.Stop();
}
