using System;
using System.IO;
using System.Windows.Controls;

namespace WPFMachine
{
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

            string temp = null;

            // TODO: Really? There must be a better way.
            for (int i = 0; i < 1000 && temp == null; i++)
            {
                try
                {
                    temp = $"{Path.GetTempPath()}\\{i}.aiff";

                    using var fs = new FileStream(temp, FileMode.Create);
                    fs.Write(sound);
                }
                catch (IOException)
                {
                    i++;
                    temp = null;
                }
            }

            if (temp != null)
            {
                _element.Source = new Uri("file:///" + temp);
            }
        }

        public void PlaySound() => _element.Play();

        public void StopSound() => _element.Stop();
    }
}
