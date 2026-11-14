using System.IO;
using System.IO.Compression;

namespace SharpHide.Core.Payloads;

public static class ConstantPool {
    private static readonly byte[] Key = {
        0x9E, 0x37, 0x79, 0xB9, 0x7F, 0x4A, 0x7C, 0x15, 0xC2, 0xB5, 0xA6, 0x30, 0x11, 0x8D, 0xE4, 0x22,
    };

    private static object[]? pool;
    public static long Key0;
    public static long Key1;
    public static int Rounds;

    public static object Get(int index) => (pool ??= Load(""))[index];

    private static object[] Load(string resourceName) {
        var assembly = typeof(ConstantPool).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)!;

        var raw = new byte[stream.Length];
        stream.ReadExactly(raw);
        Transform(raw, unchecked((ulong)Key0), unchecked((ulong)Key1), Rounds);
        for (var i = 0; i < raw.Length; i++)
            raw[i] ^= Key[i % Key.Length];

        using var memory = new MemoryStream(raw);
        using var gzip = new GZipStream(memory, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip);

        var count = reader.ReadInt32();
        var values = new object[count];
        for (var i = 0; i < count; i++) {
            var tag = reader.ReadByte();
            values[i] = tag switch {
                0 => reader.ReadInt32(),
                1 => reader.ReadInt64(),
                _ => reader.ReadString(),
            };
        }

        return values;
    }

    public static byte[] Encode(IReadOnlyList<object> values) {
        using var memory = new MemoryStream();
        using (var writer = new BinaryWriter(memory)) {
            writer.Write(values.Count);
            foreach (var value in values)
                switch (value) {
                    case int i:
                        writer.Write((byte)0);
                        writer.Write(i);
                        break;
                    case long l:
                        writer.Write((byte)1);
                        writer.Write(l);
                        break;
                    default:
                        writer.Write((byte)2);
                        writer.Write((string)value);
                        break;
                }
        }

        return memory.ToArray();
    }

    public static byte[] Scramble(byte[] data) {
        var copy = (byte[])data.Clone();
        for (var i = 0; i < copy.Length; i++)
            copy[i] ^= Key[i % Key.Length];
        return copy;
    }

    public static byte[] Encrypt(byte[] data, long key0, long key1, int rounds) {
        var copy = (byte[])data.Clone();
        Transform(copy, unchecked((ulong)key0), unchecked((ulong)key1), rounds);
        return copy;
    }

    private static void Transform(byte[] data, ulong key0, ulong key1, int rounds) {
        if ((uint)rounds > 64)
            Environment.FailFast(null);
        if (rounds == 0)
            return;
        var keys = new[] { (uint)key0, (uint)(key0 >> 32), (uint)key1, (uint)(key1 >> 32) };
        const uint delta = 0x9E3779B9;
        for (var offset = 0; offset < data.Length; offset += 8) {
            var block = (ulong)(offset / 8);
            var left = (uint)block;
            var right = (uint)(block >> 32) ^ (uint)data.Length;
            uint sum = 0;
            for (var round = 0; round < rounds; round++) {
                left += ((right << 4 ^ right >> 5) + right) ^ (sum + keys[sum & 3]);
                sum += delta;
                right += ((left << 4 ^ left >> 5) + left) ^ (sum + keys[sum >> 11 & 3]);
            }
            var stream = left | (ulong)right << 32;
            var count = Math.Min(8, data.Length - offset);
            for (var index = 0; index < count; index++)
                data[offset + index] ^= (byte)(stream >> (index * 8));
        }
    }
}
