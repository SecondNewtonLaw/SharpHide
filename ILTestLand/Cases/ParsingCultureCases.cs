using System.Globalization;

internal static class ParsingCultureCases
{
    public static bool Case2151() { return int.Parse("2,151", NumberStyles.AllowThousands, CultureInfo.InvariantCulture) == 2151; }
    public static bool Case2152() { return long.TryParse("2152", out var value) && value == 2152; }
    public static bool Case2153() { return double.TryParse("21.53", NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 21.5; }
    public static bool Case2154() { return decimal.Parse("2154.00", CultureInfo.InvariantCulture) == 2154m; }
    public static bool Case2155() { return Guid.TryParse("00000000-0000-0000-0000-000000002155", out var value) && value.ToString("N").EndsWith("2155"); }
    public static bool Case2156() { return DateTime.TryParseExact("2026-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) && value.Year == 2026; }
    public static bool Case2157() { return DateTimeOffset.TryParse("2026-02-15T21:57:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value) && value.UtcDateTime.Hour == 21; }
    public static bool Case2158() { return TimeSpan.TryParse("21:58:00", CultureInfo.InvariantCulture, out var value) && value.Hours == 21; }
    public static bool Case2159() { return Enum.TryParse<DayOfWeek>("Friday", out var value) && value == DayOfWeek.Friday; }
    public static bool Case2160() { return bool.TryParse("true", out var value) && value; }
    public static bool Case2161() { return Convert.ToInt32("2161", CultureInfo.InvariantCulture) == 2161; }
    public static bool Case2162() { return Convert.ToBase64String(new byte[] { 21, 62 }) == "FT4="; }
    public static bool Case2163() { return Convert.FromBase64String("FT4=").SequenceEqual(new byte[] { 21, 62 }); }
    public static bool Case2164() { return Uri.TryCreate("https://example.com/2164", UriKind.Absolute, out var value) && value.Scheme == Uri.UriSchemeHttps; }
    public static bool Case2165() { return Uri.UnescapeDataString(Uri.EscapeDataString("21 65")) == "21 65"; }
    public static bool Case2166() { return string.Compare("2166", "2166", StringComparison.Ordinal) == 0; }
    public static bool Case2167() { return string.Equals("2167", "2167", StringComparison.OrdinalIgnoreCase); }
    public static bool Case2168() { return "2168".AsSpan().SequenceEqual("2168".AsSpan()); }
    public static bool Case2169() { return string.Join("-", new[] { "21", "69" }) == "21-69"; }
    public static bool Case2170() { return string.Concat("21", "70") == "2170"; }
    public static bool Case2171() { return "2171".PadLeft(5, '0') == "02171"; }
    public static bool Case2172() { return "2172".Insert(2, ":") == "21:72"; }
    public static bool Case2173() { return "2173".Remove(2) == "21"; }
    public static bool Case2174() { return "2174".Replace("17", "x", StringComparison.Ordinal) == "2x4"; }
    public static bool Case2175() { return "2175".Contains("75", StringComparison.Ordinal); }
    public static bool Case2176() { return "2176".StartsWith("21", StringComparison.Ordinal); }
    public static bool Case2177() { return "2177".EndsWith("77", StringComparison.Ordinal); }
    public static bool Case2178() { return "a,b,c".Split(',').Length == 3; }
    public static bool Case2179() { return " 2179 ".Trim() == "2179"; }
    public static bool Case2180() { return "\t2180\n".Trim() == "2180"; }
    public static bool Case2181() { return "2181".ToCharArray().Reverse().SequenceEqual("1812".ToCharArray()); }
    public static bool Case2182() { return new string('x', 2182 % 10) == "xx"; }
    public static bool Case2183() { return "2183".IndexOf('8') == 2; }
    public static bool Case2184() { return "2184".LastIndexOf('8') == 2; }
    public static bool Case2185() { return "2185".Substring(1, 2) == "18"; }
    public static bool Case2186() { return "2186".ToUpperInvariant() == "2186"; }
    public static bool Case2187() { return "2187".ToLowerInvariant() == "2187"; }
    public static bool Case2188() { return "2188".Normalize() == "2188"; }
    public static bool Case2189() { return "2189".IsNormalized(); }
    public static bool Case2190() { return "2190".GetHashCode() == "2190".GetHashCode(); }
    public static bool Case2191() { return "2191".Length == 4; }
    public static bool Case2192() { return "2192"[0] == '2'; }
    public static bool Case2193() { return "2193".Any(char.IsDigit); }
    public static bool Case2194() { return "2194".All(char.IsDigit); }
    public static bool Case2195() { return "2195".Select(char.GetNumericValue).Sum() == 17; }
    public static bool Case2196() { return "2196".Where(char.IsDigit).Count() == 4; }
    public static bool Case2197() { return "2197".Aggregate(0, (sum, character) => sum + character - '0') == 19; }
    public static bool Case2198() { return "2198".OrderBy(character => character).SequenceEqual("1289"); }
    public static bool Case2199() { return "2199".Distinct().Count() == 3; }
    public static bool Case2200() { return "2200".ToCharArray().SequenceEqual(new[] { '2', '2', '0', '0' }); }
}
