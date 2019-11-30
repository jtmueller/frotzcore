// This is a very quick hack to allow me to use the Adaptive Palatte stuff

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Frotz.Other
{
    public class PNGChunk
    {
        public PNGChunk(string type, ReadOnlyMemory<byte> data, ulong crc)
        {
            Type = type;
            Data = data;
            CRC = crc;
        }

        public string Type { get; set; }
        public ReadOnlyMemory<byte> Data { get; }
        public ulong CRC { get; set; }
    }


    public class PNG
    {
        // compiler optimizes this to much faster than static array field
        private static ReadOnlySpan<byte> Header => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private readonly List<string> _chunkOrder = new List<string>();
        public Dictionary<string, PNGChunk> Chunks { get; } = new Dictionary<string, PNGChunk>();

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
                throw new ArgumentException("Not a valid PNG file");
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
            ulong crc = ReadInt(stream);

            if (crc != CalcCRC(type, buffer))
            {
                Console.WriteLine("CRC Don't match! {0} {1:X}:{2:X}", type, crc, CalcCRC(type, buffer));
            }

            var pc = new PNGChunk(type, buffer, crc);
            Chunks.Add(pc.Type, pc);

            _chunkOrder.Add(pc.Type);
        }

        private static ulong CalcCRC(string type, Span<byte> buffer)
        {
            int len = buffer.Length + 4;
            byte[]? pooled = buffer.Length > 0xff ? ArrayPool<byte>.Shared.Rent(len) : null;
            try
            {
                Span<byte> bytes = pooled ?? stackalloc byte[len];
                Encoding.UTF8.GetBytes(type, bytes);
                buffer.CopyTo(bytes[4..]);

                return CRC.Calculate(bytes[..len]);
            }
            finally
            {
                if (pooled != null)
                    ArrayPool<byte>.Shared.Return(pooled);
            }
        }

        private static string ReadType(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        private static ulong ReadInt(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[4];
            stream.Read(buffer);

            ulong a = buffer[0];
            ulong b = buffer[1];
            ulong c = buffer[2];
            ulong d = buffer[3];

            return (a << 24) | (b << 16) | (c << 8) | d;
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

                WriteLong(stream, (ulong)chunk.Data.Length);
                stream.WriteByte((byte)type[0]);
                stream.WriteByte((byte)type[1]);
                stream.WriteByte((byte)type[2]);
                stream.WriteByte((byte)type[3]);

                stream.Write(chunk.Data.Span);

                WriteLong(stream, chunk.CRC);
            }

            stream.Close();
        }

        private static void WriteLong(Stream s, ulong l)
        {
            s.WriteByte((byte)(l >> 24));
            s.WriteByte((byte)(l >> 16));
            s.WriteByte((byte)(l >> 8));
            s.WriteByte((byte)(l));
        }

        public PNG(Stream stream)
        {
            ParsePng(stream);
        }
    }
}
