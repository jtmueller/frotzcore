using System;
using System.Xml;

namespace Frotz.Other
{
    public class ZMetaData
    {
        public string RawMetaData { get; private set; }
        public string? RawBiblographic { get; private set; } // TODO Remove this

        public ZMetaData(string metadata)
        {
            RawMetaData = metadata;

            var doc = new XmlDocument();
            doc.LoadXml(metadata);
            var elements = doc.GetElementsByTagName("bibliographic");
            //Console.WriteLine("NODE:" + elements.Count);

            if (elements.Count > 0)
            {
                RawBiblographic = elements[0].InnerXml;
            }
        }
    }
}
