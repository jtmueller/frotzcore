﻿using Frotz.Screen;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace Frotz.Blorb;

public class Blorb
{
    public Dictionary<int, BlorbPicture> Pictures { get; } = [];
    public Dictionary<int, byte[]> Sounds { get; } = [];
    public byte[] ZCode { get; set; } = [];
    public string MetaData { get; set; } = string.Empty;
    public string StoryName { get; set; } = string.Empty;
    public byte[] IFhd { get; set; } = [];
    public int ReleaseNumber { get; set; }

    public ZSize StandardSize { get; set; } = ZSize.Empty;
    public ZSize MaxSize { get; set; } = ZSize.Empty;
    public ZSize MinSize { get; set; } = ZSize.Empty;

    public List<int> AdaptivePalette { get; } = [];
}

public class BlorbPicture
{
    public byte[] Image { get; }

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

    private readonly record struct Chunk(BlorbUsage Usage, int Number, int Start);

    private enum BlorbUsage : byte
    {
        Unknown, Exec, Pict, Snd
    }

    private static int _level = 0;

    private static void HandleForm(Blorb blorb, Stream stream, int start, int length, IDictionary<int, Chunk> chunks)
    {
        _level++;
        Span<byte> type = stackalloc byte[4];
        while (stream.Position < start + length)
        {
            ReadBytes(stream, type);
            int len = ReadInt(stream);

            ReadChunk(blorb, stream, (int)stream.Position, len, type, chunks);
        }
        _level--;
    }

    private static void ReadChunk(Blorb blorb, Stream stream, int start, int length, ReadOnlySpan<byte> type, IDictionary<int, Chunk> chunks)
    {
        byte[]? rentedFromPool = null;
        Span<byte> buffer = length > 0xff
            ? (rentedFromPool = ArrayPool<byte>.Shared.Rent(length))
            : stackalloc byte[length];
        try
        {
            int bytesRead = stream.Read(buffer[..length]);
            buffer = buffer[..bytesRead];
            if (chunks.TryGetValue(start - 8, out var c))
            {
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
                            if (buffer[..4].SequenceEqual("AIFF"u8))
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
                if (type.SequenceEqual("FORM"u8))
                {
                    stream.Seek(4, SeekOrigin.Current);
                    //Span<char> chars = stackalloc char[4];
                    //ReadChars(stream, chars);
                    HandleForm(blorb, stream, start, length, chunks);
                }
                else if (type.SequenceEqual("RIdx"u8))
                {
                    stream.Position = start;
                    int numResources = ReadInt(stream);
                    Span<byte> chars = stackalloc byte[4];

                    for (int i = 0; i < numResources; i++)
                    {
                        ReadBytes(stream, chars);
                        var usage = GetBlorbUsage(chars);
                        var chunk = new Chunk(usage, ReadInt(stream), ReadInt(stream));
                        chunks.Add(c.Start, chunk);
                    }
                }
                else if (type.SequenceEqual("IFmd"u8)) // Metadata
                {
                    blorb.MetaData = Encoding.UTF8.GetString(buffer);
                    if (blorb.MetaData[0] != '<')
                    {
                        // TODO Make sure that this is being handled correctly
                        int index = blorb.MetaData.IndexOf('<');
                        blorb.MetaData = blorb.MetaData[index..];
                    }
                }
                else if (type.SequenceEqual("Fspc"u8))
                {
                    stream.Position = start;
                    ReadInt(stream);
                }
                else if (type.SequenceEqual("SNam"u8))
                {
                    // TODO It seems that when it gets the story name, it is actually stored as 2 byte words,
                    // not one byte chars
                    blorb.StoryName = Encoding.UTF8.GetString(buffer);
                }
                else if (type.SequenceEqual("APal"u8))
                {
                    int len = buffer.Length / 4;
                    for (int i = 0; i < len; i++)
                    {
                        int pos = i * 4;

                        uint result = Other.ZMath.MakeInt(buffer.Slice(pos, 4));
                        blorb.AdaptivePalette.Add((int)result);
                    }
                }
                else if (type.SequenceEqual("IFhd"u8))
                {
                    blorb.IFhd = buffer.ToArray();
                }
                else if (type.SequenceEqual("RelN"u8))
                {
                    blorb.ReleaseNumber = BinaryPrimitives.ReadInt16BigEndian(buffer);
                }
                else if (type.SequenceEqual("Reso"u8))
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
                else if (type.SequenceEqual("Plte"u8))
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
            if (rentedFromPool is not null)
                ArrayPool<byte>.Shared.Return(rentedFromPool);
        }
    }

    private static BlorbUsage GetBlorbUsage(ReadOnlySpan<byte> chars)
    {
        if (chars.SequenceEqual("Exec"u8))
            return BlorbUsage.Exec;
        if (chars.SequenceEqual("Pict"u8))
            return BlorbUsage.Pict;
        if (chars.SequenceEqual("Snd "u8))
            return BlorbUsage.Snd;

        OS.Fatal("Unknown usage chunk in blorb file: " + chars.ToString());
        return BlorbUsage.Unknown;
    }

    internal static Blorb ReadBlorbFile(IMemoryOwner<byte> storyData)
    {
        using var stream = storyData.AsStream();
        return ReadBlorbFile(stream);
    }

    internal static Blorb ReadBlorbFile(Stream stream)
    {
        Blorb blorb = new();
        using PooledDictionary<int, Chunk> chunks = [];
        //_resources.Clear();

        Span<byte> chars = stackalloc byte[4];

        ReadBytes(stream, chars);
        if (!chars.SequenceEqual("FORM"u8))
        {
            ThrowHelper.ThrowInvalidDataException("Not a FORM");
        }

        int len = ReadInt(stream);
        ReadBytes(stream, chars);

        if (!chars.SequenceEqual("IFRS"u8))
        {
            ThrowHelper.ThrowInvalidDataException("Not an IFRS FORM");
        }

        HandleForm(blorb, stream, (int)stream.Position - 4, len, chunks); // Backup over the Form ID so that handle form can read it

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
        Guard.HasSizeGreaterThanOrEqualTo(destination, 4, nameof(destination));

        Span<byte> buffer = stackalloc byte[4];
        int read = stream.Read(buffer);

        if (read < buffer.Length)
            ThrowHelper.ThrowInvalidOperationException("Not enough bytes available in stream.");

        return Encoding.UTF8.GetChars(buffer, destination);
    }

    private static void ReadBytes(Stream stream, Span<byte> destination)
    {
        int read = stream.Read(destination);

        if (read < destination.Length)
            ThrowHelper.ThrowInvalidOperationException("Not enough bytes available in stream.");
    }

    private static int ReadInt(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        int read = stream.Read(buffer);

        if (read < buffer.Length)
            ThrowHelper.ThrowInvalidOperationException("Not enough bytes available in stream.");

        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }
}

