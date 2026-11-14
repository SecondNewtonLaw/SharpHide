using System.Text.Json;
using System.Text.Json.Nodes;

internal static class DataFormatCases
{
    public static bool Case2351() { return JsonSerializer.Serialize(new[] { 2, 3, 5, 1 }).Contains("2351") == false; }
    public static bool Case2352() { var value = JsonSerializer.Serialize(new { Id = 2352 }); return JsonSerializer.Deserialize<Dictionary<string, int>>(value)!["Id"] == 2352; }
    public static bool Case2353() { var value = JsonDocument.Parse("{\"Id\":2353}"); return value.RootElement.GetProperty("Id").GetInt32() == 2353; }
    public static bool Case2354() { using var value = JsonDocument.Parse("[2,3,5,4]"); return value.RootElement.GetArrayLength() == 4; }
    public static bool Case2355() { var value = JsonNode.Parse("{\"Name\":\"2355\"}")!; return value["Name"]!.GetValue<string>() == "2355"; }
    public static bool Case2356() { var value = new JsonObject { ["Id"] = 2356 }; return value.ToJsonString().Contains("2356"); }
    public static bool Case2357() { var value = JsonSerializer.Deserialize<int[]>("[2,3,5,7]")!; return value.Sum() == 17; }
    public static bool Case2358() { var value = JsonSerializer.Deserialize<Dictionary<string, bool>>("{\"ok\":true}")!; return value["ok"]; }
    public static bool Case2359() { return JsonSerializer.Serialize("2359") == "\"2359\""; }
    public static bool Case2360() { return JsonSerializer.Deserialize<string>("\"2360\"") == "2360"; }
    public static bool Case2361() { return Uri.TryCreate("https://example.com:2361/path", UriKind.Absolute, out var value) && value.Port == 2361; }
    public static bool Case2362() { var value = new Uri("https://example.com/a/../2362"); return value.AbsolutePath.EndsWith("2362"); }
    public static bool Case2363() { var value = new Uri("https://example.com/2363?q=1"); return value.Query == "?q=1"; }
    public static bool Case2364() { var value = new Uri("https://user:pass@example.com/2364"); return value.UserInfo == "user:pass"; }
    public static bool Case2365() { return new Uri("mailto:test@example.com").Scheme == Uri.UriSchemeMailto; }
    public static bool Case2366() { return Uri.CheckHostName("example.com") == UriHostNameType.Dns; }
    public static bool Case2367() { return Uri.IsWellFormedUriString("https://example.com/2367", UriKind.Absolute); }
    public static bool Case2368() { var value = new UriBuilder("https", "example.com", 2368); return value.Uri.Port == 2368; }
    public static bool Case2369() { return new Uri("https://example.com/2369").Segments.Last() == "2369"; }
    public static bool Case2370() { return new Uri("https://example.com/2370").Host == "example.com"; }
    public static bool Case2371() { return Path.GetFileName("root/2371.txt") == "2371.txt"; }
    public static bool Case2372() { return Path.GetExtension("2372.data") == ".data"; }
    public static bool Case2373() { return Path.GetFileNameWithoutExtension("2373.data") == "2373"; }
    public static bool Case2374() { return Path.GetDirectoryName("root/2374.txt") == "root"; }
    public static bool Case2375() { return Path.Combine("root", "2375.txt").EndsWith(Path.Combine("root", "2375.txt")); }
    public static bool Case2376() { return Path.ChangeExtension("2376.tmp", ".dat") == "2376.dat"; }
    public static bool Case2377() { return Path.GetRandomFileName().Length > 0; }
    public static bool Case2378() { return Path.GetInvalidFileNameChars().Length > 0; }
    public static bool Case2379() { return Path.IsPathRooted(Path.GetFullPath("2379")); }
    public static bool Case2380() { return Path.GetRelativePath("root", "root/2380") == "2380"; }
    public static bool Case2381() { return new DirectoryInfo(".").Exists; }
    public static bool Case2382() { return new FileInfo(".").Directory != null; }
    public static bool Case2383() { using var value = new MemoryStream(); return value.CanRead && value.CanWrite; }
    public static bool Case2384() { using var value = new MemoryStream(); value.WriteByte(238); return value.Length == 1; }
    public static bool Case2385() { using var value = new MemoryStream(new byte[] { 2, 3, 8, 5 }); return value.ReadByte() == 2; }
    public static bool Case2386() { using var value = new MemoryStream(); value.SetLength(2386); return value.Length == 2386; }
    public static bool Case2387() { using var value = new MemoryStream(new byte[] { 2, 3, 8, 7 }); value.Position = 2; return value.Position == 2; }
    public static bool Case2388() { using var value = new MemoryStream(new byte[] { 2, 3, 8, 8 }); return value.ToArray().Length == 4; }
    public static bool Case2389() { using var value = new MemoryStream(); value.Write(new byte[] { 2, 3, 8, 9 }); value.Position = 0; return value.ReadByte() == 2; }
    public static bool Case2390() { using var value = new MemoryStream(); value.WriteByte(239); return value.TryGetBuffer(out var buffer) && buffer.Array != null; }
    public static bool Case2391() { using var value = new StringReader("2391"); return value.Read() == '2'; }
    public static bool Case2392() { using var value = new StringReader("2392\nnext"); return value.ReadLine() == "2392"; }
    public static bool Case2393() { using var value = new StringWriter(); value.Write("2393"); return value.ToString() == "2393"; }
    public static bool Case2394() { using var value = new StringWriter(); value.WriteLine("2394"); return value.ToString().StartsWith("2394"); }
    public static bool Case2395() { using var value = new StringReader("2395"); return value.Peek() == '2'; }
    public static bool Case2396() { using var value = new StringWriter(); value.Write(2396); return value.ToString() == "2396"; }
    public static bool Case2397() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value); writer.Write(2397); return value.Length == sizeof(int); }
    public static bool Case2398() { using var value = new MemoryStream(BitConverter.GetBytes(2398)); using var reader = new BinaryReader(value); return reader.ReadInt32() == 2398; }
    public static bool Case2399() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value); writer.Write("2399"); return value.Length > 0; }
    public static bool Case2400() { using var value = new MemoryStream(); using var writer = new BinaryWriter(value); writer.Write(true); value.Position = 0; using var reader = new BinaryReader(value); return reader.ReadBoolean(); }
}
