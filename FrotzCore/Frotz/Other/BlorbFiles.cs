using Collections.Pooled;
using Frotz.Constants;
using Frotz.Screen;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Frotz.Blorb
{
    public class Blorb
    {
        internal Blorb()
        {
            Pictures = new Dictionary<int, BlorbPicture>();
            Sounds = new Dictionary<int, byte[]>();
            AdaptivePalatte = new List<int>();

            ReleaseNumber = 0;

            StandardSize = ZSize.Empty;
            MinSize = ZSize.Empty;
            MaxSize = ZSize.Empty;
            ZCode = Array.Empty<byte>();
            MetaData = string.Empty;
            StoryName = string.Empty;
            IFhd = Array.Empty<byte>();
        }

        public Dictionary<int, BlorbPicture> Pictures { get; private init; }
        public Dictionary<int, byte[]> Sounds { get; private init; }
        public byte[] ZCode { get; set; }
        public string MetaData { get; set; }
        public string StoryName { get; set; }
        public byte[] IFhd { get; set; }
        public int ReleaseNumber { get; set; }

        public ZSize StandardSize { get; set; }
        public ZSize MaxSize { get; set; }
        public ZSize MinSize { get; set; }

        public List<int> AdaptivePalatte { get; private init; }
    }

    public class BlorbPicture
    {
        public byte[] Image { get; private set; }

        internal double StandardRatio { get; set; }
        internal double MinRatio { get; set; }
        internal double MaxRatio { get; set; }

        internal BlorbPicture(byte[] image)
        {
            Image = image;
        }
    }


    public class BlorbReader
    {
        //private class Resource
        //{
        //    public int Id;
        //    public string Usage;
        //    public byte[] Data;

        //    public Resource(int id, string usage, byte[] data)
        //    {
        //        Id = id;
        //        Usage = usage;
        //        Data = data;
        //    }
        //}

        private readonly struct Chunk
        {
            public readonly BlorbUsage Usage;
            public readonly int Number;
            public readonly int Start;

            public Chunk(BlorbUsage usage, int number, int start)
            {
                Usage = usage;
                Number = number;
                Start = start;
            }
        }

        private enum BlorbUsage : byte
        {
            Unknown, Exec, Pict, Snd
        }

        private static int _level = 0;
        private static readonly PooledDictionary<int, Chunk> _chunks = new();
        //private static readonly PooledDictionary<int, Resource> _resources = new PooledDictionary<int, Resource>();

        private static void HandleForm(Blorb blorb, Stream stream, int start, int length)
        {
            _level++;
            Span<char> type = stackalloc char[4];
            while (stream.Position < start + length)
            {
                ReadChars(stream, type);
                int len = ReadInt(stream);
                // ReadBuffer(len);

                ReadChunk(blorb, stream, (int)stream.Position, len, type);
            }
            _level--;
        }

        private static void ReadChunk(Blorb blorb, Stream stream, int start, int length, ReadOnlySpan<char> type)
        {
            byte[]? rentedFromPool = null;
            Span<byte> buffer = length > 0xff
                ? (rentedFromPool = ArrayPool<byte>.Shared.Rent(length))
                : stackalloc byte[length];
            try
            {
                int bytesRead = stream.Read(buffer[..length]);
                buffer = buffer[..bytesRead];
                if (_chunks.ContainsKey(start - 8))
                {
                    var c = _chunks[start - 8];
                    switch (c.Usage)
                    {
                        case BlorbUsage.Exec:
                            blorb.ZCode = buffer.ToArray();
                            break;
                        case BlorbUsage.Pict:
                            blorb.Pictures[c.Number] = new BlorbPicture(buffer.ToArray());
                            break;
                        case BlorbUsage.Snd:
                            {
                                if (buffer[..4].Matches("AIFF"))
                                {
                                    byte[] temp = new byte[buffer.Length + 8];

                                    General.FormBytes.CopyTo(temp.AsSpan(..4));
                                    BinaryPrimitives.WriteInt32BigEndian(temp.AsSpan(4..8), buffer.Length);
                                    buffer.CopyTo(temp.AsSpan(8));

                                    blorb.Sounds[c.Number] = temp;
                                }
                                else
                                {
                                    OS.Fatal("Unhandled sound type in blorb file");
                                }

                                break;
                            }

                        default:
                            OS.Fatal("Unknown usage chunk in blorb file:" + c.Usage);
                            break;
                    }
                }
                else
                {
                    if (type.SequenceEqual("FORM"))
                    {
                        Span<char> chars = stackalloc char[4];
                        ReadChars(stream, chars);
                        HandleForm(blorb, stream, start, length);
                    }
                    else if (type.SequenceEqual("RIdx"))
                    {
                        stream.Position = start;
                        int numResources = ReadInt(stream);
                        Span<char> chars = stackalloc char[4];

                        for (int i = 0; i < numResources; i++)
                        {
                            ReadChars(stream, chars);
                            var usage = GetBlorbUsage(chars);
                            var c = new Chunk(usage, ReadInt(stream), ReadInt(stream));
                            _chunks.Add(c.Start, c);
                        }
                    }
                    else if (type.SequenceEqual("IFmd")) // Metadata
                    {
                        blorb.MetaData = Encoding.UTF8.GetString(buffer);
                        if (blorb.MetaData[0] != '<')
                        {
                            // TODO Make sure that this is being handled correctly
                            int index = blorb.MetaData.IndexOf('<');
                            blorb.MetaData = blorb.MetaData[index..];
                        }
                    }
                    else if (type.SequenceEqual("Fspc"))
                    {
                        stream.Position = start;
                        ReadInt(stream);
                    }
                    else if (type.SequenceEqual("SNam"))
                    {
                        // TODO It seems that when it gets the story name, it is actually stored as 2 byte words,
                        // not one byte chars
                        blorb.StoryName = Encoding.UTF8.GetString(buffer);
                    }
                    else if (type.SequenceEqual("APal"))
                    {
                        int len = buffer.Length / 4;
                        for (int i = 0; i < len; i++)
                        {
                            int pos = i * 4;

                            uint result = Other.ZMath.MakeInt(buffer.Slice(pos, 4));
                            blorb.AdaptivePalatte.Add((int)result);
                        }
                    }
                    else if (type.SequenceEqual("IFhd"))
                    {
                        blorb.IFhd = buffer.ToArray();
                    }
                    else if (type.SequenceEqual("RelN"))
                    {
                        blorb.ReleaseNumber = BinaryPrimitives.ReadInt16BigEndian(buffer);
                    }
                    else if (type.SequenceEqual("Reso"))
                    {
                        stream.Position = start;
                        int px = ReadInt(stream);
                        int py = ReadInt(stream);
                        int minx = ReadInt(stream);
                        int miny = ReadInt(stream);
                        int maxx = ReadInt(stream);
                        int maxy = ReadInt(stream);

                        blorb.StandardSize = (py, px);
                        blorb.MinSize = (miny, minx);
                        blorb.MaxSize = (maxy, maxx);

                        while (stream.Position < start + length)
                        {
                            int number = ReadInt(stream);
                            int ratnum = ReadInt(stream);
                            int ratden = ReadInt(stream);
                            int minnum = ReadInt(stream);
                            int minden = ReadInt(stream);
                            int maxnum = ReadInt(stream);
                            int maxden = ReadInt(stream);

                            if (ratden != 0) blorb.Pictures[number].StandardRatio = ratnum / ratden;
                            if (minden != 0) blorb.Pictures[number].MinRatio = minnum / minden;
                            if (maxden != 0) blorb.Pictures[number].MaxRatio = maxnum / maxden;
                        }
                    }
                    else if (type.SequenceEqual("Plte"))
                    {
                        Debug.WriteLine("Palette");
                    }
                    else
                    {
                        // unhandled: Loop, AUTH, ANNO, "(c) "...
                        Debug.WriteLine("{0," + _level + "}:Type:{1}:{2}", ' ', type.ToString(), length);
                    }
                }
                if (stream.Position % 2 == 1) stream.Position++;
            }
            finally
            {
                if (rentedFromPool != null)
                    ArrayPool<byte>.Shared.Return(rentedFromPool);
            }
        }

        private static BlorbUsage GetBlorbUsage(ReadOnlySpan<char> chars)
        {
            if (chars.SequenceEqual("Exec"))
                return BlorbUsage.Exec;
            if (chars.SequenceEqual("Pict"))
                return BlorbUsage.Pict;
            if (chars.SequenceEqual("Snd "))
                return BlorbUsage.Snd;

            OS.Fatal("Unknown usage chunk in blorb file: " + chars.ToString());
            return BlorbUsage.Unknown;
        }

        internal static Blorb ReadBlorbFile(byte[] storyData)
        {
            using var stream = new MemoryStream(storyData);
            return ReadBlorbFile(stream);
        }

        internal static Blorb ReadBlorbFile(Stream stream)
        {
            var blorb = new Blorb();
            _chunks.Clear();
            //_resources.Clear();

            Span<char> chars = stackalloc char[4];

            ReadChars(stream, chars);
            if (!chars.SequenceEqual("FORM"))
            {
                throw new Exception("Not a FORM");
            }

            int len = ReadInt(stream);
            ReadChars(stream, chars);

            if (!chars.SequenceEqual("IFRS"))
            {
                throw new Exception("Not an IFRS FORM");
            }

            HandleForm(blorb, stream, (int)stream.Position - 4, len); // Backup over the Form ID so that handle form can read it

            if (!string.IsNullOrEmpty(blorb.MetaData))
            {
                try
                {
                    var doc = XDocument.Parse(blorb.MetaData);
                    var r = doc.Root;

                    if (r is not null)
                    {
                        var n = XName.Get("title", r.Name.NamespaceName);
                        foreach (var e in doc.Descendants(n))
                        {
                            blorb.StoryName = e.Value;
                        }
                    }
                }
                catch (Exception)
                {
                    blorb.MetaData = string.Empty;
                    throw;
                }
            }

            return blorb;
        }

        private static int ReadChars(Stream stream, Span<char> destination)
        {
            if (destination.Length < 4)
                throw new ArgumentException("Destination must hold at least four characters.", nameof(destination));

            Span<byte> buffer = stackalloc byte[4];
            int read = stream.Read(buffer);

            if (read < buffer.Length)
                throw new InvalidOperationException("Not enough bytes available in stream.");

            return Encoding.UTF8.GetChars(buffer, destination);
        }

        private static int ReadInt(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            int read = stream.Read(buffer);

            if (read < buffer.Length)
                throw new InvalidOperationException("Not enough bytes available in stream.");

            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }
    }
}

