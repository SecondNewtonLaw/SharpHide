using System.Reflection;

namespace SharpHide.Core.Payloads;

public static class CipherPasses {
    public sealed record Pass(Action<byte[]> Forward, MethodInfo Inverse, int? Seed = null);

    private static MethodInfo Method(string name) =>
        typeof(CipherPasses).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;

    public static readonly Pass Junk = new(_ => { },
        typeof(JunkPass).GetMethod(nameof(JunkPass.Bloat), BindingFlags.Public | BindingFlags.Static)!);

    private static Func<Random, Pass> Fixed(Action<byte[]> forward, string inverseName) {
        var inverse = Method(inverseName);
        return _ => new Pass(forward, inverse);
    }

    private static Func<Random, Pass> Keyed(string forwardName, string inverseName) {
        var forward = Method(forwardName);
        var inverse = Method(inverseName);
        return rng => {
            var seed = rng.Next();
            return new Pass(b => forward.Invoke(null, new object[] { b, seed }), inverse, seed);
        };
    }

    public static readonly IReadOnlyList<Func<Random, Pass>> Pool = new[] {
        Fixed(XorRoll, nameof(XorRoll)),
        Fixed(AddIndex, nameof(SubIndex)),
        Fixed(NibbleXorForward, nameof(NibbleXorInverse)),
        Fixed(Reverse, nameof(Reverse)),
        Fixed(Rc4, nameof(Rc4)),
        Fixed(NotBytes, nameof(NotBytes)),
        Fixed(XorSquare, nameof(XorSquare)),
        Fixed(SwapAdjacent, nameof(SwapAdjacent)),
        Fixed(Rot3Forward, nameof(Rot3Inverse)),
        Fixed(MulOddForward, nameof(MulOddInverse)),
        Fixed(ChainXorForward, nameof(ChainXorInverse)),
        Keyed(nameof(KeyedXor), nameof(KeyedXor)),
        Keyed(nameof(KeyedAdd), nameof(KeyedSub)),
        Keyed(nameof(KeyedAffine), nameof(KeyedAffineInverse)),
        Keyed(nameof(KeyedRc4), nameof(KeyedRc4)),
        Keyed(nameof(KeyedRounds), nameof(KeyedRoundsInverse)),
    };

    public static void XorRoll(byte[] buffer) {
        byte key = 0x5A;
        for (var i = 0; i < buffer.Length; i++) {
            buffer[i] ^= key;
            key = (byte)(key * 31 + 7 + i);
        }
    }

    public static void AddIndex(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] + (byte)(i * 7 + 13));
    }

    public static void SubIndex(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] - (byte)(i * 7 + 13));
    }

    public static void NibbleXorForward(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++) {
            var x = (byte)(buffer[i] ^ 0xA7);
            buffer[i] = (byte)((x << 4) | (x >> 4));
        }
    }

    public static void NibbleXorInverse(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++) {
            var x = (byte)((buffer[i] >> 4) | (buffer[i] << 4));
            buffer[i] = (byte)(x ^ 0xA7);
        }
    }

    public static void Reverse(byte[] buffer) => Array.Reverse(buffer);

    public static void NotBytes(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)~buffer[i];
    }

    public static void XorSquare(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] ^= (byte)(i * i + 0x3B);
    }

    public static void SwapAdjacent(byte[] buffer) {
        for (var i = 0; i + 1 < buffer.Length; i += 2)
            (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
    }

    public static void Rot3Forward(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)((buffer[i] << 3) | (buffer[i] >> 5));
    }

    public static void Rot3Inverse(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)((buffer[i] << 5) | (buffer[i] >> 3));
    }

    // 163 is the inverse of 11 modulo 256.
    public static void MulOddForward(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] * 11);
    }

    public static void MulOddInverse(byte[] buffer) {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] * 163);
    }

    // Forward walks up; inverse walks down.
    public static void ChainXorForward(byte[] buffer) {
        for (var i = 1; i < buffer.Length; i++)
            buffer[i] ^= buffer[i - 1];
    }

    public static void ChainXorInverse(byte[] buffer) {
        for (var i = buffer.Length - 1; i >= 1; i--)
            buffer[i] ^= buffer[i - 1];
    }

    public static void Rc4(byte[] buffer) {
        ReadOnlySpan<byte> key = stackalloc byte[] { 0x13, 0x37, 0xC0, 0xDE, 0x42 };
        Span<byte> s = stackalloc byte[256];
        for (var i = 0; i < 256; i++)
            s[i] = (byte)i;
        var j = 0;
        for (var i = 0; i < 256; i++) {
            j = (j + s[i] + key[i % key.Length]) & 255;
            (s[i], s[j]) = (s[j], s[i]);
        }
        int a = 0, b = 0;
        for (var k = 0; k < buffer.Length; k++) {
            a = (a + 1) & 255;
            b = (b + s[a]) & 255;
            (s[a], s[b]) = (s[b], s[a]);
            buffer[k] ^= s[(s[a] + s[b]) & 255];
        }
    }

    public static void KeyedXor(byte[] buffer, int seed) {
        var state = (uint)seed;
        for (var i = 0; i < buffer.Length; i++) {
            state = state * 1664525u + 1013904223u;
            buffer[i] ^= (byte)(state >> 24);
        }
    }

    public static void KeyedAdd(byte[] buffer, int seed) {
        var state = (uint)seed;
        for (var i = 0; i < buffer.Length; i++) {
            state = state * 1664525u + 1013904223u;
            buffer[i] = (byte)(buffer[i] + (byte)(state >> 24));
        }
    }

    public static void KeyedSub(byte[] buffer, int seed) {
        var state = (uint)seed;
        for (var i = 0; i < buffer.Length; i++) {
            state = state * 1664525u + 1013904223u;
            buffer[i] = (byte)(buffer[i] - (byte)(state >> 24));
        }
    }

    public static void KeyedAffine(byte[] buffer, int seed) {
        var a = (byte)((seed & 0x7F) * 2 + 1);
        var b = (byte)(seed >> 8);
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(buffer[i] * a + b);
    }

    public static void KeyedAffineInverse(byte[] buffer, int seed) {
        var a = (uint)((seed & 0x7F) * 2 + 1);
        var b = (byte)(seed >> 8);
        var ai = a;
        for (var k = 0; k < 5; k++)
            ai *= 2u - a * ai;
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)((buffer[i] - b) * ai);
    }

    public static void KeyedRc4(byte[] buffer, int seed) {
        Span<byte> key = stackalloc byte[] { (byte)seed, (byte)(seed >> 8), (byte)(seed >> 16), (byte)(seed >> 24) };
        Span<byte> s = stackalloc byte[256];
        for (var i = 0; i < 256; i++)
            s[i] = (byte)i;
        var j = 0;
        for (var i = 0; i < 256; i++) {
            j = (j + s[i] + key[i % key.Length]) & 255;
            (s[i], s[j]) = (s[j], s[i]);
        }
        int a = 0, b = 0;
        for (var k = 0; k < buffer.Length; k++) {
            a = (a + 1) & 255;
            b = (b + s[a]) & 255;
            (s[a], s[b]) = (s[b], s[a]);
            buffer[k] ^= s[(s[a] + s[b]) & 255];
        }
    }

    public static void KeyedRounds(byte[] buffer, int seed) {
        var state = (uint)seed | 1u;
        var a = (byte)((seed & 0x7F) * 2 + 1);
        var prev = (byte)(seed >> 16);
        for (var i = 0; i < buffer.Length; i++) {
            state = state * 1664525u + 1013904223u;
            var k = (byte)(state >> 24);
            var x = (byte)(buffer[i] * a);
            x = (byte)(x + k);
            x = (byte)((x << 3) | (x >> 5));
            x ^= prev;
            buffer[i] = x;
            prev = x;
        }
    }

    public static void KeyedRoundsInverse(byte[] buffer, int seed) {
        var state = (uint)seed | 1u;
        var a = (uint)((seed & 0x7F) * 2 + 1);
        var ai = a;
        for (var k2 = 0; k2 < 5; k2++)
            ai *= 2u - a * ai;
        var prev = (byte)(seed >> 16);
        for (var i = 0; i < buffer.Length; i++) {
            state = state * 1664525u + 1013904223u;
            var k = (byte)(state >> 24);
            var c = buffer[i];
            var x = (byte)(c ^ prev);
            x = (byte)((x << 5) | (x >> 3));
            x = (byte)(x - k);
            x = (byte)(x * ai);
            buffer[i] = x;
            prev = c;
        }
    }
}
