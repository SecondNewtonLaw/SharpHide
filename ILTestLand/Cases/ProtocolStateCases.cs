using System.Globalization;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class ProtocolStateCases
{
    public static bool Case1251() { var state = "start"; foreach (var command in new[] { "open", "write", "close" }) state = (state, command) switch { ("start", "open") => "open", ("open", "write") => "written", ("written", "close") => "closed", _ => "error" }; return state == "closed"; }
    public static bool Case1252() { var state = "start"; foreach (var command in new[] { "close", "open" }) state = (state, command) switch { ("start", "open") => "open", _ => "error" }; return state == "error"; }
    public static bool Case1253() { var headers = ParseHeaders("Host: example.test\nContent-Length: 1253"); return headers["Host"] == "example.test" && headers["Content-Length"] == "1253"; }
    public static bool Case1254() { var headers = ParseHeaders("x-a: 1\nx-b: 2\nx-a: 3"); return headers["x-a"] == "3"; }
    public static bool Case1255() { var fields = ParseQuery("a=1&b=1255"); return fields["b"] == "1255"; }
    public static bool Case1256() { var fields = ParseQuery("a=hello%20world"); return fields["a"] == "hello world"; }
    public static bool Case1257() { var value = ParseHex("0A FF 1257"); return value.SequenceEqual(new byte[] { 10, 255, 0x12, 0x57 }); }
    public static bool Case1258() { var value = ParseHex("1258"); return value.SequenceEqual(new byte[] { 0x12, 0x58 }); }
    public static bool Case1259() { var value = ParseCsv("a,b,c"); return value.SequenceEqual(new[] { "a", "b", "c" }); }
    public static bool Case1260() { var value = ParseCsv("1260,,x"); return value.Length == 3 && value[0] == "1260" && value[1] == ""; }
    public static bool Case1261() { var value = ParseIntList("1,2,1261"); return value[^1] == 1261; }
    public static bool Case1262() { var value = ParseIntList("1;2;1262", ';'); return value.Sum() == 1265; }
    public static bool Case1263() { var value = ParseBool("TRUE"); return value; }
    public static bool Case1264() { var value = ParseBool("off"); return !value; }
    public static bool Case1265() { var value = ValidateEmail("test@example.test"); return value; }
    public static bool Case1266() { var value = ValidateEmail("bad"); return !value; }
    public static bool Case1267() { var value = ValidatePort(1267); return value; }
    public static bool Case1268() { var value = ValidatePort(70000); return !value; }
    public static bool Case1269() { var value = NormalizePath("/api/../api/1269"); return value == "api/1269"; }
    public static bool Case1270() { var value = NormalizePath("a//b/./c"); return value == "a/b/c"; }
    public static bool Case1271() { var value = "1271".PadLeft(8, '0'); return value == "00001271"; }
    public static bool Case1272() { var value = string.Join('-', Enumerable.Range(1, 4).Select(number => number.ToString("D4", CultureInfo.InvariantCulture))); return value == "0001-0002-0003-0004"; }
    public static bool Case1273() { var value = DateTimeOffset.ParseExact("20260908T1213", "yyyyMMdd'T'HHmm", CultureInfo.InvariantCulture); return value.Day == 8 && value.Minute == 13; }
    public static bool Case1274() { var value = TimeSpan.ParseExact("12:34", "mm\\:ss", CultureInfo.InvariantCulture); return value.TotalSeconds == 754; }
    public static bool Case1275() { var value = decimal.Parse("1_275.00".Replace("_", "", StringComparison.Ordinal), CultureInfo.InvariantCulture); return value == 1275m; }
    public static bool Case1276() { var value = new[] { "1", "bad", "3" }.Select(text => int.TryParse(text, out var number) ? number : -1); return value.SequenceEqual(new[] { 1, -1, 3 }); }
    public static bool Case1277() { var value = new[] { 1, 2, 3 }.Select(number => number switch { 1 => "one", 2 => "two", _ => "many" }); return value.SequenceEqual(new[] { "one", "two", "many" }); }
    public static bool Case1278() { var value = new[] { 1278, 1, 2 }.OrderBy(number => number).ToArray(); return value[0] == 1; }
    public static bool Case1279() { var value = new[] { (Key: "a", Value: 1), (Key: "b", Value: 2) }.ToDictionary(item => item.Key); return value["b"].Value == 2; }
    public static bool Case1280() { var value = new[] { "a", "b", "c" }.Select((text, index) => $"{index}:{text}"); return value.SequenceEqual(new[] { "0:a", "1:b", "2:c" }); }
    public static bool Case1281() { var value = Enumerable.Range(1, 1281).TakeWhile(number => number < 5).Sum(); return value == 10; }
    public static bool Case1282() { var value = Enumerable.Range(1, 1282).SkipWhile(number => number < 1279).Take(4).ToArray(); return value.SequenceEqual(new[] { 1279, 1280, 1281, 1282 }); }
    public static bool Case1283() { var value = Enumerable.Range(1, 1283).Where(number => number % 100 == 83).ToArray(); return value.SequenceEqual(new[] { 83, 183, 283, 383, 483, 583, 683, 783, 883, 983, 1083, 1183, 1283 }); }
    public static bool Case1284() { var value = Enumerable.Range(1, 1284).Except(new[] { 1284 }).Last(); return value == 1283; }
    public static bool Case1285() { var value = Enumerable.Range(1, 1285).Intersect(new[] { 1, 2, 1285 }).ToArray(); return value.SequenceEqual(new[] { 1, 2, 1285 }); }
    public static bool Case1286() { var value = new[] { 1, 2 }.Concat(new[] { 1286 }).ToArray(); return value[^1] == 1286; }
    public static bool Case1287() { var value = new[] { 1, 2 }.Append(1287).Prepend(0).ToArray(); return value.SequenceEqual(new[] { 0, 1, 2, 1287 }); }
    public static bool Case1288() { var value = Enumerable.Range(1, 10).Aggregate(1, (product, number) => product * number); return value == 3628800; }
    public static bool Case1289() { var value = Enumerable.Range(1, 10).Aggregate("", (text, number) => text + number); return value == "12345678910"; }
    public static bool Case1290() { var value = new[] { 1, 2, 3 }.SelectMany(number => new[] { number, number * 10 }).ToArray(); return value.SequenceEqual(new[] { 1, 10, 2, 20, 3, 30 }); }
    public static bool Case1291() { var value = new[] { 1, 2, 3 }.Join(new[] { (Id: 2, Text: "two") }, left => left, right => right.Id, (left, right) => right.Text); return value.Single() == "two"; }
    public static bool Case1292() { var value = new[] { 1, 2, 3 }.GroupJoin(new[] { (Id: 2, Text: "two") }, left => left, right => right.Id, (left, right) => (left, Count: right.Count())); return value.Single(item => item.left == 2).Count == 1; }
    public static bool Case1293() { var value = new[] { 1, 2, 3 }.Zip(new[] { 10, 20, 30 }, (left, right) => left + right); return value.SequenceEqual(new[] { 11, 22, 33 }); }
    public static bool Case1294() { var value = new[] { 1, 2, 3 }.Chunk(2).SelectMany(chunk => chunk).ToArray(); return value.SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case1295() { var value = new[] { 1, 2, 3 }.AggregateBy(number => number % 2, 0, (sum, number) => sum + number).ToDictionary(item => item.Key, item => item.Value); return value[1] == 4 && value[0] == 2; }
    public static bool Case1296() { var value = new[] { "a", "b", "a" }.CountBy(text => text); return value.Single(item => item.Key == "a").Value == 2; }
    public static bool Case1297() { var value = Enumerable.Range(1, 10).MinBy(number => Math.Abs(number - 7)); return value == 7; }
    public static bool Case1298() { var value = Enumerable.Range(1, 10).MaxBy(number => number % 3); return value % 3 == 2; }
    public static bool Case1299() { var value = new[] { 1, 2, 3 }.TryGetNonEnumeratedCount(out var count); return value && count == 3; }
    public static bool Case1300() { var value = new[] { 1, 2, 3 }.Reverse().ToArray(); return value.SequenceEqual(new[] { 3, 2, 1 }); }

    private static Dictionary<string, string> ParseHeaders(string text) => text.Split('\n').Select(line => line.Split(": ")).GroupBy(parts => parts[0]).ToDictionary(group => group.Key, group => group.Last()[1]);
    private static Dictionary<string, string> ParseQuery(string text) => text.Split('&').Select(part => part.Split('=', 2)).ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
    private static byte[] ParseHex(string text) => Convert.FromHexString(text.Replace(" ", "", StringComparison.Ordinal));
    private static string[] ParseCsv(string text) => text.Split(',').Select(item => item.Trim('"')).ToArray();
    private static int[] ParseIntList(string text, char separator = ',') => text.Split(separator).Select(int.Parse).ToArray();
    private static bool ParseBool(string text) => text.Equals("true", StringComparison.OrdinalIgnoreCase) || text.Equals("on", StringComparison.OrdinalIgnoreCase) || text.Equals("yes", StringComparison.OrdinalIgnoreCase);
    private static bool ValidateEmail(string text) => text.Count(character => character == '@') == 1 && text.IndexOf('@') > 0 && text.IndexOf('@') < text.Length - 1;
    private static bool ValidatePort(int port) => port is >= 1 and <= 65535;
    private static string NormalizePath(string text) { var parts = new Stack<string>(); foreach (var part in text.Split('/')) { if (part is "" or ".") continue; if (part == ".." && parts.Count > 0) parts.Pop(); else parts.Push(part); } return string.Join('/', parts.Reverse()); }
}
