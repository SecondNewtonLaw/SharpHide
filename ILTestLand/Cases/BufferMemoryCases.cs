using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1400, BogusStates = 6, OpaqueDepth = 3,
    DecryptHashChance = 0.65)]
internal static class BufferMemoryCases {
    public static bool Case0651() { Span<int> values = stackalloc[] { 600, 50, 1 }; var total = 0; foreach (var value in values) total += value; return total == 651; }
    public static bool Case0652() { Span<int> values = [6, 5, 2, 9]; return values[1..3].SequenceEqual(new[] { 5, 2 }); }
    public static bool Case0653() { Span<int> values = [6, 5, 3]; values.Reverse(); return values.SequenceEqual(new[] { 3, 5, 6 }); }
    public static bool Case0654() { Span<int> values = [9, 6, 5, 4]; values.Sort(); return values.SequenceEqual(new[] { 4, 5, 6, 9 }); }
    public static bool Case0655() { ReadOnlySpan<int> values = [600, 620, 640, 655, 700]; return values.BinarySearch(655) == 3; }
    public static bool Case0656() { Span<int> values = stackalloc int[8]; values.Fill(82); var total = 0; foreach (var value in values) total += value; return total == 656; }
    public static bool Case0657() { ReadOnlySpan<byte> source = [6, 5, 7]; Span<byte> target = stackalloc byte[3]; source.CopyTo(target); return target.SequenceEqual(source); }
    public static bool Case0658() { Span<int> values = [1, 2, 3, 4, 5]; values[..4].CopyTo(values[1..]); return values.SequenceEqual(new[] { 1, 1, 2, 3, 4 }); }
    public static bool Case0659() { Span<byte> bytes = stackalloc byte[659]; bytes[0] = 6; bytes[^1] = 59; return bytes.Length == 659 && bytes[0] + bytes[^1] == 65; }
    public static bool Case0660() { var data = new[] { 600, 60, 0 }; Memory<int> memory = data; memory.Span[2] = 60; return data.Sum() == 720 && memory.Length == 3; }
    public static bool Case0661() { ReadOnlyMemory<char> memory = "case-661".AsMemory(); return memory[5..].Span.SequenceEqual("661"); }
    public static bool Case0662() { Span<int> values = [0x01020304, 0x05060708]; var bytes = MemoryMarshal.AsBytes(values); return bytes.Length == 8 && bytes.IndexOf((byte)8) >= 0; }
    public static bool Case0663() { Span<byte> bytes = stackalloc byte[8]; BinaryPrimitives.WriteInt32LittleEndian(bytes, 663); return BinaryPrimitives.ReadInt32LittleEndian(bytes) == 663; }
    public static bool Case0664() { Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(bytes, 0x01020304); return bytes.SequenceEqual(new byte[] { 1, 2, 3, 4 }); }
    public static bool Case0665() { Span<byte> bytes = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(bytes, -665); return BinaryPrimitives.ReadInt64BigEndian(bytes) == -665; }
    public static bool Case0666() { Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteSingleLittleEndian(bytes, 666.25f); return BinaryPrimitives.ReadSingleLittleEndian(bytes) == 666.25f; }
    public static bool Case0667() { Span<byte> bytes = stackalloc byte[8]; BinaryPrimitives.WriteDoubleBigEndian(bytes, 667.125); return BinaryPrimitives.ReadDoubleBigEndian(bytes) == 667.125; }
    public static bool Case0668() { var pool = ArrayPool<byte>.Shared; var buffer = pool.Rent(668); try { buffer.AsSpan(0, 668).Fill(8); return buffer.Length >= 668 && buffer.AsSpan(0, 668).IndexOfAnyExcept((byte)8) < 0; } finally { pool.Return(buffer, true); } }
    public static bool Case0669() { ReadOnlySpan<byte> left = [6, 6, 9]; ReadOnlySpan<byte> right = [6, 6, 9]; return left.SequenceEqual(right); }
    public static bool Case0670() { var values = Enumerable.Range(660, 20).ToArray(); return values[^10] == 670 && values[10..15].SequenceEqual(new[] { 670, 671, 672, 673, 674 }); }
    public static bool Case0671() { ReadOnlySpan<char> text = "prefix:671"; return text[(text.IndexOf(':') + 1)..].SequenceEqual("671"); }
    public static bool Case0672() { ReadOnlySpan<char> text = "672"; return int.TryParse(text, out var value) && value == 672; }
    public static bool Case0673() { Span<byte> bytes = stackalloc byte[16]; var written = Encoding.UTF8.GetBytes("λ673", bytes); return written == 5 && Encoding.UTF8.GetString(bytes[..written]) == "λ673"; }
    public static bool Case0674() { ReadOnlySpan<byte> bytes = "case-674"u8; return Encoding.UTF8.GetString(bytes) == "case-674"; }
    public static bool Case0675() { Span<char> output = stackalloc char[8]; return Convert.TryToHexString(new byte[] { 0x06, 0x75 }, output, out var written) && written == 4 && output[..written].SequenceEqual("0675"); }
    public static bool Case0676() { Span<byte> bytes = stackalloc byte[8]; return BitConverter.TryWriteBytes(bytes, 676L) && BitConverter.ToInt64(bytes) == 676L; }
    public static bool Case0677() { using var stream = new MemoryStream(32); stream.Write("677"u8); return stream.TryGetBuffer(out var segment) && segment.AsSpan(0, 3).SequenceEqual("677"u8); }
    public static bool Case0678() { var source = new[] { 6, 7, 8 }; var bytes = new byte[12]; Buffer.BlockCopy(source, 0, bytes, 0, bytes.Length); var target = new int[3]; Buffer.BlockCopy(bytes, 0, target, 0, bytes.Length); return target.SequenceEqual(source); }
    public static bool Case0679() { var values = new[] { 1, 2, 3, 4, 5 }; Array.Copy(values, 0, values, 1, 4); return values.SequenceEqual(new[] { 1, 1, 2, 3, 4 }); }
    public static bool Case0680() { var values = new[] { 6, 8 }; Array.Resize(ref values, 5); values[4] = 0; return values.Length == 5 && values.Sum() == 14; }
    public static bool Case0681() { var values = Enumerable.Repeat(681, 6).ToArray(); Array.Clear(values, 1, 4); return values.Count(value => value == 681) == 2 && values.Sum() == 1362; }
    public static bool Case0682() { var values = new int[8]; Array.Fill(values, 682, 2, 4); return values.Count(value => value == 682) == 4 && values.Take(2).All(value => value == 0); }
    public static bool Case0683() { var values = new[] { 6, 8, 3, 9 }; var segment = new ArraySegment<int>(values, 1, 2); return segment.Count == 2 && segment.SequenceEqual(new[] { 8, 3 }); }
    public static bool Case0684() { var sequence = new ReadOnlySequence<byte>("case-684"u8.ToArray()); return sequence.IsSingleSegment && sequence.Length == 8 && sequence.FirstSpan[^1] == (byte)'4'; }
    public static bool Case0685() { var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(new byte[] { 6, 8, 5 })); return reader.TryRead(out var first) && first == 6 && reader.Consumed == 1 && reader.Remaining == 2; }
    public static bool Case0686() { var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>("left|686"u8.ToArray())); return reader.TryReadTo(out ReadOnlySpan<byte> token, (byte)'|') && token.SequenceEqual("left"u8) && reader.UnreadSpan.SequenceEqual("686"u8); }
    public static bool Case0687() { var writer = new ArrayBufferWriter<byte>(); var span = writer.GetSpan(3); "687"u8.CopyTo(span); writer.Advance(3); return writer.WrittenSpan.SequenceEqual("687"u8); }
    public static bool Case0688() { var writer = new ArrayBufferWriter<int>(8); writer.Write(new[] { 600, 80, 8 }); var values = writer.WrittenSpan; return writer.WrittenCount == 3 && values[0] + values[1] + values[2] == 688; }
    public static bool Case0689() { var first = new BufferSegment(new byte[] { 6, 8 }); var second = first.Append(new byte[] { 9 }); var sequence = new ReadOnlySequence<byte>(first, 0, second, second.Memory.Length); return sequence.Length == 3 && sequence.ToArray().SequenceEqual(new byte[] { 6, 8, 9 }); }
    public static bool Case0690() { return "  690\t".AsSpan().Trim().SequenceEqual("690"); }
    public static bool Case0691() { ReadOnlySpan<char> value = "a,b,c,d"; var count = 1; foreach (var character in value) if (character == ',') count++; return count == 4; }
    public static bool Case0692() { var digits = SearchValues.Create("0123456789"); return "case692".AsSpan().IndexOfAny(digits) == 4; }
    public static bool Case0693() { ReadOnlySpan<byte> data = [1, 2, 6, 9, 3]; return data.IndexOfAny((byte)6, (byte)9) == 2 && data.LastIndexOfAny((byte)6, (byte)9) == 3; }
    public static bool Case0694() { Span<char> target = stackalloc char[7]; "case694".AsSpan().CopyTo(target); target[4] = '-'; return target.SequenceEqual("case-94"); }
    public static bool Case0695() { var value = string.Create(8, 695, (span, state) => { "case-".AsSpan().CopyTo(span); state.TryFormat(span[5..], out _); }); return value == "case-695"; }
    public static bool Case0696() { ReadOnlyMemory<byte> memory = new byte[] { 6, 9, 6 }; return MemoryMarshal.TryGetArray(memory, out var segment) && segment.Array!.SequenceEqual(new byte[] { 6, 9, 6 }); }
    public static bool Case0697() { var values = GC.AllocateUninitializedArray<int>(697); Array.Clear(values); values[^1] = 7; return values.Length == 697 && values.Sum() == 7; }
    public static bool Case0698() { var values = GC.AllocateArray<byte>(698, pinned: true); values[0] = 6; values[^1] = 98; return values.Length == 698 && values[0] + values[^1] == 104; }
    public static bool Case0699() { return Marshal.SizeOf<Guid>() == 16 && Marshal.SizeOf<long>() == 8; }
    public static bool Case0700() { using var owner = MemoryPool<byte>.Shared.Rent(700); var memory = owner.Memory[..700]; memory.Span.Fill(7); return memory.Length == 700 && memory.Span.IndexOfAnyExcept((byte)7) < 0; }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte> {
        public BufferSegment(ReadOnlyMemory<byte> memory) {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory) {
            var segment = new BufferSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }
}
