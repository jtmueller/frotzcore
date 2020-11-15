using System;
using System.Linq;
using System.Windows.Documents;

namespace WPFMachine.RTBSubclasses
{
    public class ZParagraph : Paragraph
    {
        public ZParagraph()
        {

        }

        public double Top
        {
            get;
            set;
        }

        public double DetermineWidth(double pixelsPerDip = 1.0)
        {
            return Inlines.Sum(x => x switch
            {
                ZRun run => run.DetermineWidth(pixelsPerDip),
                _ => 0.0
            });
        }

        public new InlineCollection Inlines => throw new ArgumentException("Please use Add/Clear functions");

        public void AddInline(ZRun run) => base.Inlines.Add(run);

        public void RemoveInline(ZRun run) => base.Inlines.Remove(run);

        public void ClearInlines() => base.Inlines.Clear();
    }

}
