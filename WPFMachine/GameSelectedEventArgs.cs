using System;

namespace WPFMachine
{
    public class GameSelectedEventArgs : EventArgs
    {
        public string StoryFileName { get; private set; }
        public Frotz.Blorb.Blorb BlorbFile { get; private set; }

        public GameSelectedEventArgs(string StoryFileName, Frotz.Blorb.Blorb BlorbFile)
        {
            this.StoryFileName = StoryFileName;
            this.BlorbFile = BlorbFile;
        }
    }
}
