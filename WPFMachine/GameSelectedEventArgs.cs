using System;

namespace WPFMachine
{
    public class GameSelectedEventArgs : EventArgs
    {
        public string StoryFileName { get; private init; }
        public Frotz.Blorb.Blorb BlorbFile { get; private init; }

        public GameSelectedEventArgs(string StoryFileName, Frotz.Blorb.Blorb BlorbFile)
        {
            this.StoryFileName = StoryFileName;
            this.BlorbFile = BlorbFile;
        }
    }
}
