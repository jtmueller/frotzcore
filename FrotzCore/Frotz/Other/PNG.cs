// This is a very quick hack to allow me to use the Adaptive Palatte stuff

namespace Frotz.Other;

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

public class PNGChunk
{
    public PNGChunk(string type, ReadOnlyMemory<byte> data, uint crc)
    {
        Type = type;
        Data = data;
        CRC = crc;
    }

    public string Type { get; set; }
    public ReadOnlyMemory<byte> Data { get; }
    public uint CRC { get; set; }
}


public class PNG
{
    // compiler optimizes this to much faster than static array field
    private static ReadOnlySpan<byte> Header => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private readonly List<string> _chunkOrder = new();
    public Dictionary<string, PNGChunk> Chunks { get; } = new();

    public PNG(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        ParsePng(fs);
    }

    private void ParsePng(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        stream.Read(buffer);

        if (!buffer.SequenceEqual(Header))
        {
            ThrowHelper.ThrowArgumentException("Not a valid PNG file");
        }

        while (stream.Position < stream.Length)
        {
            ReadChunk(stream);
        }
    }

    private void ReadChunk(Stream stream)
    {
        int len = (int)ReadInt(stream);
        string type = ReadType(stream);
        byte[] buffer = new byte[len];
        stream.Read(buffer, 0, len);
        uint crc = ReadInt(stream);

        if (crc != CalcCRC(type, buffer))
        {
            Console.WriteLine("CRC doesn't match! {0} {1:X}:{2:X}", type, crc, CalcCRC(type, buffer));
        }

        var pc = new PNGChunk(type, buffer, crc);
        Chunks.Add(pc.Type, pc);

        _chunkOrder.Add(pc.Type);
    }

    private static ulong CalcCRC(string type, Span<byte> buffer)
    {
        int len = buffer.Length + 4;
        byte[]? pooled = null;
        try
        {
            Span<byte> bytes = buffer.Length > 0xff
                ? (pooled = ArrayPool<byte>.Shared.Rent(len))
                : stackalloc byte[len];
            Encoding.UTF8.GetBytes(type, bytes);
            buffer.CopyTo(bytes[4..]);

            return CRC.Calculate(bytes[..len]);
        }
        finally
        {
            if (pooled is not null)
                ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    private static string ReadType(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.Read(buffer);
        return StringPool.Shared.GetOrAdd(buffer, Encoding.UTF8);
    }

    private static uint ReadInt(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        stream.Read(buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    private static void WriteInt(Stream s, uint num)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, num);
        s.Write(bytes);
    }

    public void Save(string fileName)
    {
        using var fs = new FileStream(fileName, FileMode.Create);
        Save(fs);
    }

    public void Save(Stream stream)
    {
        stream.Write(Header);

        foreach (string type in _chunkOrder)
        {
            var chunk = Chunks[type];

            WriteInt(stream, (uint)chunk.Data.Length);
            stream.WriteByte((byte)type[0]);
            stream.WriteByte((byte)type[1]);
            stream.WriteByte((byte)type[2]);
            stream.WriteByte((byte)type[3]);

            stream.Write(chunk.Data.Span);

            WriteInt(stream, chunk.CRC);
        }

        stream.Close();
    }

    public PNG(Stream stream)
    {
        ParsePng(stream);
    }
}
