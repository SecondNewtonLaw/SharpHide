using System.IO.Compression;
using System.Buffers;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class CompressionArchiveCases
{
    public static bool Case1651() { using var stream = new MemoryStream(); using (var zip = new DeflateStream(stream, CompressionMode.Compress, true)) zip.Write("1651"u8); return stream.Length > 0; }
    public static bool Case1652() { using var stream = new MemoryStream(); using (var zip = new GZipStream(stream, CompressionMode.Compress, true)) zip.Write("1652"u8); return stream.Length > 0; }
    public static bool Case1653() { using var stream = new MemoryStream(); using (var zip = new BrotliStream(stream, CompressionMode.Compress, true)) zip.Write("1653"u8); return stream.Length > 0; }
    public static bool Case1654() { var bytes = Compress("1654", CompressionLevel.Fastest); return Decompress(bytes, CompressionKind.Deflate) == "1654"; }
    public static bool Case1655() { var bytes = Compress("1655", CompressionLevel.Optimal); return Decompress(bytes, CompressionKind.Deflate) == "1655"; }
    public static bool Case1656() { var bytes = CompressGzip("1656"); return Decompress(bytes, CompressionKind.Gzip) == "1656"; }
    public static bool Case1657() { var bytes = CompressBrotli("1657"); return Decompress(bytes, CompressionKind.Brotli) == "1657"; }
    public static bool Case1658() { using var archive = new ZipArchive(new MemoryStream(), ZipArchiveMode.Create); var entry = archive.CreateEntry("1658.txt"); return entry.Name == "1658.txt"; }
    public static bool Case1659() { using var stream = new MemoryStream(); using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true)) { using var writer = new StreamWriter(archive.CreateEntry("1659").Open()); writer.Write("ok"); } return stream.Length > 0; }
    public static bool Case1660() { using var stream = new MemoryStream(); using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true)) archive.CreateEntry("a"); stream.Position = 0; using var read = new ZipArchive(stream, ZipArchiveMode.Read); return read.Entries.Single().FullName == "a"; }
    public static bool Case1661() { var value = new StringReader("1661\nline"); return value.ReadLine() == "1661"; }
    public static bool Case1662() { var value = new StringReader("1662"); return value.ReadToEnd() == "1662"; }
    public static bool Case1663() { using var value = new MemoryStream(Encoding.UTF8.GetBytes("1663")); using var reader = new StreamReader(value, Encoding.UTF8); return reader.ReadLine() == "1663"; }
    public static bool Case1664() { using var value = new MemoryStream(); using (var writer = new StreamWriter(value, Encoding.UTF8, leaveOpen: true)) writer.WriteLine("1664"); value.Position = 0; using var reader = new StreamReader(value); return reader.ReadLine() == "1664"; }
    public static bool Case1665() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value, Encoding.UTF8, true); writer.Write((short)1665); value.Position = 0; using var reader = new BinaryReader(value); return reader.ReadInt16() == 1665; }
    public static bool Case1666() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value, Encoding.UTF8, true); writer.Write(1666L); value.Position = 0; using var reader = new BinaryReader(value); return reader.ReadInt64() == 1666L; }
    public static bool Case1667() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value, Encoding.UTF8, true); writer.Write(1667.5); value.Position = 0; using var reader = new BinaryReader(value); return reader.ReadDouble() == 1667.5; }
    public static bool Case1668() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value, Encoding.UTF8, true); writer.Write("1668"); value.Position = 0; using var reader = new BinaryReader(value); return reader.ReadString() == "1668"; }
    public static bool Case1669() { var value = new MemoryStream([1, 2, 3]); value.Position = 1; return value.ReadByte() == 2; }
    public static bool Case1670() { var value = new MemoryStream(); value.WriteByte(1670 % 256); return value.ToArray()[0] == 134; }
    public static bool Case1671() { var value = new MemoryStream(); value.Write([1, 6, 7, 1]); return value.Length == 4; }
    public static bool Case1672() { var value = new MemoryStream([1, 6, 7, 2]); var buffer = new byte[2]; value.ReadExactly(buffer); return buffer.SequenceEqual(new byte[] { 1, 6 }); }
    public static bool Case1673() { var value = new MemoryStream([1, 6, 7, 3]); value.Position = 2; return value.ReadByte() == 7; }
    public static bool Case1674() { var value = new MemoryStream(); value.Seek(1674, SeekOrigin.Begin); return value.Position == 1674; }
    public static bool Case1675() { var value = new MemoryStream(); value.SetLength(1675); value.Position = 100; value.SetLength(10); return value.Position == 10; }
    public static bool Case1676() { var value = new MemoryStream(); value.Capacity = 1676; return value.Capacity >= 1676; }
    public static bool Case1677() { var value = new MemoryStream(); value.Write("1677"u8); value.TryGetBuffer(out var buffer); return buffer.Count == 4; }
    public static bool Case1678() { using var value = new MemoryStream(); return value.CanRead && value.CanSeek && value.CanWrite; }
    public static bool Case1679() { var value = new MemoryStream(); value.Dispose(); return !value.CanRead; }
    public static bool Case1680() { var value = new MemoryStream(); value.Write("1680"u8); var copy = value.ToArray(); return Encoding.UTF8.GetString(copy) == "1680"; }
    public static bool Case1681() { var value = new byte[4]; Buffer.BlockCopy(Encoding.UTF8.GetBytes("1681"), 0, value, 0, 4); return Encoding.UTF8.GetString(value) == "1681"; }
    public static bool Case1682() { var value = new byte[4]; Array.Copy("1682"u8.ToArray(), value, 4); return value[3] == (byte)'2'; }
    public static bool Case1683() { var value = new byte[4]; value.AsSpan().CopyTo(new byte[4]); return value.Length == 4; }
    public static bool Case1684() { var value = new byte[] { 1, 2, 3, 4 }; return value.AsSpan(1, 2).SequenceEqual(new byte[] { 2, 3 }); }
    public static bool Case1685() { var value = new byte[] { 1, 2, 3, 4 }; value.AsSpan(1, 2).Reverse(); return value.SequenceEqual(new byte[] { 1, 3, 2, 4 }); }
    public static bool Case1686() { var value = new byte[] { 1, 2, 3, 4 }; value.AsSpan().Sort(); return value.SequenceEqual(new byte[] { 1, 2, 3, 4 }); }
    public static bool Case1687() { var value = new[] { 4, 2, 3, 1 }; value.AsSpan().Sort(); return value.SequenceEqual(new[] { 1, 2, 3, 4 }); }
    public static bool Case1688() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().IndexOf(3) == 2; }
    public static bool Case1689() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().LastIndexOf(2) == 1; }
    public static bool Case1690() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().Contains(4); }
    public static bool Case1691() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().BinarySearch(3) == 2; }
    public static bool Case1692() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().SequenceEqual(value); }
    public static bool Case1693() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().StartsWith(new[] { 1, 2 }); }
    public static bool Case1694() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().EndsWith(new[] { 3, 4 }); }
    public static bool Case1695() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().CommonPrefixLength(new[] { 1, 2, 9 }) == 2; }
    public static bool Case1696() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().Overlaps(value.AsSpan(1)); }
    public static bool Case1697() { var value = new[] { 1, 2, 3, 4 }; return value.AsSpan().IsEmpty == false; }
    public static bool Case1698() { Span<int> value = stackalloc[] { 1, 2, 3 }; return value.Length == 3; }
    public static bool Case1699() { Span<byte> value = stackalloc byte[4]; value.Fill(169); return value[0] == 169; }
    public static bool Case1700() { var value = new ReadOnlySequence<byte>(new byte[] { 17, 0 }); return value.Length == 2; }

    private enum CompressionKind { Deflate, Gzip, Brotli }
    private static byte[] Compress(string text, CompressionLevel level) { using var stream = new MemoryStream(); using (var zip = new DeflateStream(stream, level, true)) zip.Write(Encoding.UTF8.GetBytes(text)); return stream.ToArray(); }
    private static byte[] CompressGzip(string text) { using var stream = new MemoryStream(); using (var zip = new GZipStream(stream, CompressionLevel.Optimal, true)) zip.Write(Encoding.UTF8.GetBytes(text)); return stream.ToArray(); }
    private static byte[] CompressBrotli(string text) { using var stream = new MemoryStream(); using (var zip = new BrotliStream(stream, CompressionLevel.Optimal, true)) zip.Write(Encoding.UTF8.GetBytes(text)); return stream.ToArray(); }
    private static string Decompress(byte[] bytes, CompressionKind kind) { using var input = new MemoryStream(bytes); using Stream zip = kind switch { CompressionKind.Gzip => new GZipStream(input, CompressionMode.Decompress), CompressionKind.Brotli => new BrotliStream(input, CompressionMode.Decompress), _ => new DeflateStream(input, CompressionMode.Decompress) }; using var reader = new StreamReader(zip); return reader.ReadToEnd(); }
}
