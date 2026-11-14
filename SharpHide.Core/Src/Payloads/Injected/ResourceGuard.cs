using System.IO;

namespace SharpHide.Core.Payloads;

// Decrypts resource streams at rewritten call sites.
public static class ResourceGuard {
    private static readonly byte[] Key = {
        0x51, 0x4E, 0x44, 0x52, 0x67, 0x1D, 0xC0, 0x9B, 0x3A, 0x7F, 0x88, 0x2C, 0xE5, 0x14, 0x6D, 0xB0,
    };

    public static Stream? Unwrap(Stream? stream) {
        if (stream is null)
            return null;

        var data = new byte[stream.Length];
        stream.ReadExactly(data);
        stream.Dispose();

        for (var i = 0; i < data.Length; i++)
            data[i] ^= Key[i % Key.Length];

        return new MemoryStream(data);
    }

    public static byte[] Scramble(byte[] data) {
        var copy = (byte[])data.Clone();
        for (var i = 0; i < copy.Length; i++)
            copy[i] ^= Key[i % Key.Length];
        return copy;
    }
}
