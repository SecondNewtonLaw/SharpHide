using System.Buffers;
using System.Text;

internal static class BufferSpanCases
{
    public static bool Case2301() { Span<int> value = stackalloc int[] { 2, 3, 0, 1 }; value.Sort(); return value.SequenceEqual(new[] { 0, 1, 2, 3 }); }
    public static bool Case2302() { Span<byte> value = stackalloc byte[4]; value.Fill(230); return value[3] == 230; }
    public static bool Case2303() { Span<char> value = stackalloc char[] { '2', '3', '0', '3' }; return value.IndexOf('3') == 1; }
    public static bool Case2304() { Span<int> value = stackalloc int[] { 1, 2, 3 }; return value.Contains(2); }
    public static bool Case2305() { Span<int> value = stackalloc int[] { 1, 2, 3 }; return value.IndexOfAny(2, 4) == 1; }
    public static bool Case2306() { Span<byte> value = stackalloc byte[] { 1, 2, 3 }; return value.SequenceEqual(new byte[] { 1, 2, 3 }); }
    public static bool Case2307() { Span<char> value = stackalloc char[] { '2', '3', '0', '7' }; return value.StartsWith("230".AsSpan()); }
    public static bool Case2308() { Span<char> value = stackalloc char[] { '2', '3', '0', '8' }; return value.EndsWith("08".AsSpan()); }
    public static bool Case2309() { var value = new byte[4]; Random.Shared.NextBytes(value); return value.Length == 4; }
    public static bool Case2310() { var value = ArrayPool<byte>.Shared.Rent(8); try { value[0] = 231; return value[0] == 231; } finally { ArrayPool<byte>.Shared.Return(value); } }
    public static bool Case2311() { var value = new StringBuilder(); value.Append("23").Append(11); return value.ToString() == "2311"; }
    public static bool Case2312() { var value = new StringBuilder("2312"); value.Insert(2, ':'); return value.ToString() == "23:12"; }
    public static bool Case2313() { var value = new StringBuilder("2313"); value.Replace("13", "x"); return value.ToString() == "23x"; }
    public static bool Case2314() { var value = new StringBuilder("2314"); value.Remove(2, 2); return value.ToString() == "23"; }
    public static bool Case2315() { var value = Encoding.UTF8.GetBytes("2315"); return Encoding.UTF8.GetString(value) == "2315"; }
    public static bool Case2316() { var value = Encoding.Unicode.GetBytes("2316"); return Encoding.Unicode.GetString(value) == "2316"; }
    public static bool Case2317() { var value = Encoding.ASCII.GetBytes("2317"); return value.Length == 4; }
    public static bool Case2318() { return Encoding.UTF8.GetByteCount("é") == 2; }
    public static bool Case2319() { return Encoding.UTF8.GetCharCount(new byte[] { 50, 51, 49, 57 }) == 4; }
    public static bool Case2320() { var value = new MemoryStream(); value.WriteByte(232); value.Position = 0; return value.ReadByte() == 232; }
    public static bool Case2321() { using var value = new MemoryStream(Encoding.UTF8.GetBytes("2321")); return new StreamReader(value).ReadToEnd() == "2321"; }
    public static bool Case2322() { using var value = new MemoryStream(); using (var writer = new StreamWriter(value, leaveOpen: true)) writer.Write("2322"); return value.Length > 0; }
    public static bool Case2323() { using var value = new MemoryStream(new byte[] { 1, 2, 3 }); value.Seek(1, SeekOrigin.Begin); return value.ReadByte() == 2; }
    public static bool Case2324() { using var value = new MemoryStream(); value.Write(new byte[] { 2, 3, 2, 4 }); return value.Position == 4; }
    public static bool Case2325() { using var value = new MemoryStream(new byte[] { 2, 3, 2, 5 }); return value.ToArray().Distinct().Count() == 3; }
    public static bool Case2326() { var value = new byte[4]; Buffer.BlockCopy(new byte[] { 2, 3, 2, 6 }, 0, value, 0, 4); return value[3] == 6; }
    public static bool Case2327() { var value = new int[2]; Array.Clear(value); return value.All(x => x == 0); }
    public static bool Case2328() { return Buffer.ByteLength(new[] { 1, 2, 3 }) == 12; }
    public static bool Case2329() { return MemoryExtensions.Equals("2329".AsSpan(), "2329".AsSpan(), StringComparison.Ordinal); }
    public static bool Case2330() { return "2330".AsSpan().CompareTo("2330".AsSpan(), StringComparison.Ordinal) == 0; }
    public static bool Case2331() { ReadOnlySpan<char> value = "2331"; return value[1] == '3'; }
    public static bool Case2332() { ReadOnlyMemory<char> value = "2332".AsMemory(); return value.Span.Length == 4; }
    public static bool Case2333() { var value = new ReadOnlyMemory<byte>(new byte[] { 2, 3, 3, 3 }); return value.Span[2] == 3; }
    public static bool Case2334() { var value = new Memory<int>(new[] { 2, 3, 3, 4 }); value.Span[3] = 9; return value.Span[3] == 9; }
    public static bool Case2335() { var value = new ArraySegment<int>(new[] { 2, 3, 3, 5 }, 1, 2); return value.Count == 2 && value[0] == 3; }
    public static bool Case2336() { var value = new ArraySegment<byte>(new byte[] { 2, 3, 3, 6 }, 1, 2); return value.ToArray().SequenceEqual(new byte[] { 3, 3 }); }
    public static bool Case2337() { var value = new WeakReference<string>("2337"); return value.TryGetTarget(out var target) && target == "2337"; }
    public static bool Case2338() { var value = new GCHandleHolder(2338); return value.Value == 2338; }
    public static bool Case2339() { var value = GC.GetAllocatedBytesForCurrentThread(); return GC.GetAllocatedBytesForCurrentThread() >= value; }
    public static bool Case2340() { GC.KeepAlive("2340"); return true; }
    public static bool Case2341() { return GC.GetGeneration(new object()) >= 0; }
    public static bool Case2342() { return GC.MaxGeneration >= 0; }
    public static bool Case2343() { return GC.GetTotalMemory(false) > 0; }
    public static bool Case2344() { var value = new byte[2344]; return value.AsMemory().Length == 2344; }
    public static bool Case2345() { var value = new int[5]; value.AsSpan().Fill(2345); return value[4] == 2345; }
    public static bool Case2346() { return new[] { 2, 3, 4, 6 }.AsSpan().Slice(1, 2).SequenceEqual(new[] { 3, 4 }); }
    public static bool Case2347() { var value = new byte[] { 2, 3, 4, 7 }; return value.AsSpan(1).IndexOf((byte)4) == 1; }
    public static bool Case2348() { return "2348".AsSpan().ToString() == "2348"; }
    public static bool Case2349() { return "2349".AsSpan().Trim().Length == 4; }
    public static bool Case2350() { return " 2350 ".AsSpan().Trim().SequenceEqual("2350".AsSpan()); }

    private sealed class GCHandleHolder
    {
        public GCHandleHolder(int value) { Value = value; }
        public int Value { get; }
    }
}
