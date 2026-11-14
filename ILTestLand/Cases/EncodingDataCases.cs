using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class EncodingDataCases
{
    public static bool Case1351() { var value = Encoding.ASCII.GetBytes("1351"); return value.SequenceEqual(new byte[] { 49, 51, 53, 49 }); }
    public static bool Case1352() { var value = Encoding.UTF8.GetString([49, 51, 53, 50]); return value == "1352"; }
    public static bool Case1353() { var value = Encoding.Unicode.GetBytes("1353"); return value.Length == 8; }
    public static bool Case1354() { var value = Encoding.UTF32.GetBytes("1354"); return value.Length == 16; }
    public static bool Case1355() { var value = Encoding.Latin1.GetBytes("1355"); return value.Length == 4; }
    public static bool Case1356() { var value = Encoding.UTF8.GetPreamble(); return value.Length == 3; }
    public static bool Case1357() { var value = "é".Normalize(NormalizationForm.FormC); return value.Length == 1; }
    public static bool Case1358() { var value = "é".Normalize(NormalizationForm.FormD); return value.Length == 2; }
    public static bool Case1359() { var value = new Rune(0x1F98A).ToString(); return value == "🦊"; }
    public static bool Case1360() { var value = string.Concat("136", '0'); return value == "1360"; }
    public static bool Case1361() { var value = string.Create(4, 1361, (span, number) => number.TryFormat(span, out _)); return value == "1361"; }
    public static bool Case1362() { var value = 1362.ToString("X", CultureInfo.InvariantCulture); return value == "552"; }
    public static bool Case1363() { var value = 1363.ToString("D6", CultureInfo.InvariantCulture); return value == "001363"; }
    public static bool Case1364() { var value = 13.64m.ToString("F2", CultureInfo.InvariantCulture); return value == "13.64"; }
    public static bool Case1365() { var value = 1365.ToString("N0", CultureInfo.InvariantCulture); return value == "1,365"; }
    public static bool Case1366() { var value = DateTime.ParseExact("2026-09-08", "yyyy-MM-dd", CultureInfo.InvariantCulture); return value.DayOfWeek == DayOfWeek.Tuesday; }
    public static bool Case1367() { var value = DateTimeOffset.ParseExact("2026-09-08+0300", "yyyy-MM-ddzzz", CultureInfo.InvariantCulture); return value.Offset == TimeSpan.FromHours(3); }
    public static bool Case1368() { var value = TimeSpan.Parse("14:08:00"); return value.TotalMinutes == 848; }
    public static bool Case1369() { var value = CultureInfo.GetCultureInfo("en-US").NumberFormat.CurrencySymbol; return value == "$"; }
    public static bool Case1370() { var value = CultureInfo.GetCultureInfo("tr-TR").TextInfo.ToUpper("i"); return value == "İ"; }
    public static bool Case1371() { var value = string.Compare("ä", "z", CultureInfo.GetCultureInfo("sv-SE"), CompareOptions.None); return value > 0; }
    public static bool Case1372() { var value = "1372".Split(' ', StringSplitOptions.RemoveEmptyEntries); return value.Length == 1; }
    public static bool Case1373() { var value = " a \t b ".Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries); return value.SequenceEqual(new[] { "a", "b" }); }
    public static bool Case1374() { var value = "1374".ReplaceLineEndings("\n"); return value == "1374"; }
    public static bool Case1375() { var value = "1375".AsSpan().Trim().ToString(); return value == "1375"; }
    public static bool Case1376() { var value = new[] { "a", null, "b" }.WhereNotNull().ToArray(); return value.SequenceEqual(new[] { "a", "b" }); }
    public static bool Case1377() { var value = JsonSerializer.Serialize(new Dictionary<string, int> { ["id"] = 1377 }); return value == "{\"id\":1377}"; }
    public static bool Case1378() { var value = JsonDocument.Parse("{\"id\":1378,\"ok\":true}").RootElement; return value.GetProperty("ok").GetBoolean(); }
    public static bool Case1379() { var value = JsonNode.Parse("[1379,1380]")!; return value[1]!.GetValue<int>() == 1380; }
    public static bool Case1380() { var value = JsonSerializer.Deserialize<int[]>("[1,2,1380]")!; return value[^1] == 1380; }
    public static bool Case1381() { var value = new XElement("root", new XElement("id", 1381)); return value.ToString(SaveOptions.DisableFormatting) == "<root><id>1381</id></root>"; }
    public static bool Case1382() { var value = XDocument.Parse("<root><id>1382</id></root>"); return value.Root!.Element("id")!.Value == "1382"; }
    public static bool Case1383() { var value = XDocument.Parse("<root><x/><x/></root>").Root!.Elements("x").Count(); return value == 2; }
    public static bool Case1384() { var value = new XElement("root", new XAttribute("id", 1384)); return value.Attribute("id")!.Value == "1384"; }
    public static bool Case1385() { var value = new XElement("root", new XElement("id", 1385)); return value.Descendants().Single().Value == "1385"; }
    public static bool Case1386() { var value = new[] { 1, 2, 3 }.Select(number => new XElement("n", number)); return value.Last().Value == "3"; }
    public static bool Case1387() { var value = new XElement("root", Enumerable.Range(1, 3).Select(number => new XElement("n", number))); return value.Elements().Sum(element => int.Parse(element.Value)) == 6; }
    public static bool Case1388() { var value = new[] { "a", "b" }.Aggregate(new StringBuilder(), (builder, text) => builder.Append(text)).ToString(); return value == "ab"; }
    public static bool Case1389() { var value = new[] { 1, 2, 3 }.Aggregate(new Dictionary<int, int>(), (map, number) => { map[number] = number * number; return map; }); return value[3] == 9; }
    public static bool Case1390() { var value = new[] { 1, 2, 3 }.ToLookup(number => number % 2); return value[1].Count() == 2; }
    public static bool Case1391() { var value = new[] { "a", "bb", "c" }.ToHashSet(StringComparer.Ordinal); return value.Contains("bb"); }
    public static bool Case1392() { var value = new[] { "a", "bb", "c" }.ToDictionary(text => text); return value["bb"] == "bb"; }
    public static bool Case1393() { var value = new[] { (1, "a"), (2, "b") }.ToDictionary(pair => pair.Item1, pair => pair.Item2); return value[2] == "b"; }
    public static bool Case1394() { var value = Enumerable.Range(1, 10).ToArray(); return value.AsSpan()[4..7].SequenceEqual(new[] { 5, 6, 7 }); }
    public static bool Case1395() { var value = new[] { 1, 2, 3, 4 }.AsSpan(); value[0] = 1395; return value[0] == 1395; }
    public static bool Case1396() { ReadOnlySpan<char> value = "1396"; return value.StartsWith("13") && value.EndsWith("96"); }
    public static bool Case1397() { var value = "1397"u8; return value.Length == 4 && value[0] == (byte)'1'; }
    public static bool Case1398() { var value = Convert.ToBase64String("1398"u8.ToArray()); return value == "MTM5OA=="; }
    public static bool Case1399() { var value = Convert.FromBase64String("MTM5OQ=="); return Encoding.UTF8.GetString(value) == "1399"; }
    public static bool Case1400() { var value = new Uri("https://example.test/1400?x=1"); return value.AbsolutePath == "/1400" && value.Query == "?x=1"; }

    private static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> values) where T : class => values.OfType<T>();
}
