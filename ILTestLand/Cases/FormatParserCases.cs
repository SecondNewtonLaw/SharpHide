using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1600, BogusStates = 5, OpaqueDepth = 3, DecryptHashChance = 0.6)]
internal static class FormatParserCases
{
    public static bool Case1051() { var value = Convert.ToBase64String(Encoding.UTF8.GetBytes("1051")); return value == "MTA1MQ=="; }
    public static bool Case1052() { var value = Encoding.UTF8.GetString(Convert.FromBase64String("MTA1Mg==")); return value == "1052"; }
    public static bool Case1053() { var value = Uri.UnescapeDataString(Uri.EscapeDataString("case 1053/✓")); return value == "case 1053/✓"; }
    public static bool Case1054() { var value = System.Net.WebUtility.HtmlDecode("&lt;1054&gt;"); return value == "<1054>"; }
    public static bool Case1055() { var value = System.Net.WebUtility.UrlDecode("case%201055"); return value == "case 1055"; }
    public static bool Case1056() { var value = System.Text.RegularExpressions.Regex.Replace("a-1056-b", "\\d+", "N"); return value == "a-N-b"; }
    public static bool Case1057() { var value = System.Text.RegularExpressions.Regex.Match("id=1057", "id=(\\d+)").Groups[1].Value; return value == "1057"; }
    public static bool Case1058() { var value = string.Join(',', "a,b,c".Split(',', StringSplitOptions.RemoveEmptyEntries)); return value == "a,b,c"; }
    public static bool Case1059() { var value = "1059".AsSpan().ToString(); return value == "1059"; }
    public static bool Case1060() { var value = new StringBuilder().Append("10").Append(60).ToString(); return value == "1060"; }
    public static bool Case1061() { var value = JsonDocument.Parse("{\"id\":1061,\"name\":\"x\"}").RootElement; return value.GetProperty("id").GetInt32() == 1061 && value.GetProperty("name").GetString() == "x"; }
    public static bool Case1062() { var node = JsonNode.Parse("{\"value\":1062}"); return node?["value"]?.GetValue<int>() == 1062; }
    public static bool Case1063() { var value = JsonSerializer.Serialize(new Payload(1063, "x")); return value.Contains("1063", StringComparison.Ordinal); }
    public static bool Case1064() { var value = JsonDocument.Parse("{\"items\":[1,2,1064]}").RootElement.GetProperty("items")[2].GetInt32(); return value == 1064; }
    public static bool Case1065() { var value = XDocument.Parse("<root><item id='1065'/></root>").Root!.Element("item")!.Attribute("id")!.Value; return value == "1065"; }
    public static bool Case1066() { var value = XElement.Parse("<item><value>1066</value></item>").Value; return value == "1066"; }
    public static bool Case1067() { var value = new XDocument(new XElement("root", new XElement("id", 1067))).ToString(SaveOptions.DisableFormatting); return value == "<root><id>1067</id></root>"; }
    public static bool Case1068() { var value = new System.Xml.Linq.XElement("r", new System.Xml.Linq.XAttribute("n", 1068)); return value.Attribute("n")!.Value == "1068"; }
    public static bool Case1069() { var value = DateTimeOffset.Parse("2026-09-08T10:11:09+00:00"); return value.Hour == 10 && value.Minute == 11; }
    public static bool Case1070() { var value = DateTime.ParseExact("20260908", "yyyyMMdd", null); return value.Year == 2026 && value.Day == 8; }
    public static bool Case1071() { var value = decimal.Parse("1,071.50", System.Globalization.CultureInfo.InvariantCulture); return value == 1071.5m; }
    public static bool Case1072() { var value = double.TryParse("1072.25", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed); return value && parsed == 1072.25; }
    public static bool Case1073() { var value = Enum.Parse<DayOfWeek>("Tuesday"); return value == DayOfWeek.Tuesday; }
    public static bool Case1074() { var value = int.TryParse("bad", out _); return !value; }
    public static bool Case1075() { var value = Guid.TryParse("00000000-0000-0000-0000-000000001075", out var guid); return value && guid.ToString("N").EndsWith("1075", StringComparison.Ordinal); }
    public static bool Case1076() { var value = Version.Parse("10.76.3"); return value.Major == 10 && value.Minor == 76; }
    public static bool Case1077() { var value = new Uri("https://example.test:1077/a?b=c"); return value.Port == 1077 && value.Query == "?b=c"; }
    public static bool Case1078() { var value = new UriBuilder("https", "example.test", 1078, "x").Uri; return value.AbsolutePath == "/x"; }
    public static bool Case1079() { var value = System.Net.IPAddress.Parse("127.0.0.1"); return value.GetAddressBytes().SequenceEqual(new byte[] { 127, 0, 0, 1 }); }
    public static bool Case1080() { var value = System.Net.IPAddress.Parse("::1"); return System.Net.IPAddress.IsLoopback(value); }
    public static bool Case1081() { var value = new System.Net.NetworkCredential("user", "pass"); return value.UserName == "user" && value.Password == "pass"; }
    public static bool Case1082() { var value = System.Net.WebUtility.HtmlEncode("<1082>"); return value == "&lt;1082&gt;"; }
    public static bool Case1083() { var value = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase("case 1083"); return value == "Case 1083"; }
    public static bool Case1084() { var value = string.Compare("a", "A", StringComparison.OrdinalIgnoreCase); return value == 0; }
    public static bool Case1085() { var value = "aaab".Replace("a", "x", StringComparison.Ordinal); return value == "xxxb"; }
    public static bool Case1086() { var value = "1086".PadLeft(6, '0'); return value == "001086"; }
    public static bool Case1087() { var value = "a|b|c".Split('|', StringSplitOptions.TrimEntries); return value.Length == 3 && value[1] == "b"; }
    public static bool Case1088() { var value = "a\r\nb".Split("\r\n", StringSplitOptions.None); return value.SequenceEqual(new[] { "a", "b" }); }
    public static bool Case1089() { var value = "🦊".EnumerateRunes().Single().Value; return value == 129418; }
    public static bool Case1090() { var value = "é".Normalize(NormalizationForm.FormD); return value.Length == 2; }
    public static bool Case1091() { var value = "abc".Contains('b'); return value; }
    public static bool Case1092() { var value = "abc".IndexOfAny(['x', 'b']); return value == 1; }
    public static bool Case1093() { var value = "abc".LastIndexOf('c'); return value == 2; }
    public static bool Case1094() { var value = "abc".AsSpan(1, 2).SequenceEqual("bc"); return value; }
    public static bool Case1095() { var value = "a".PadRight(5, 'x'); return value == "axxxx"; }
    public static bool Case1096() { var value = string.Create(4, 1096, (span, number) => number.ToString().AsSpan().CopyTo(span)); return value == "1096"; }
    public static bool Case1097() { var value = new Rune('✓').ToString(); return value == "✓"; }
    public static bool Case1098() { var value = char.ConvertToUtf32("🦊", 0); return value == 129418; }
    public static bool Case1099() { var value = "1050".Select(character => character - '0').Sum(); return value == 6; }
    public static bool Case1100() { var value = "1100".Aggregate(0, (sum, character) => sum * 10 + character - '0'); return value == 1100; }

    private sealed record Payload(int Id, string Name);
}
