using System.Text.RegularExpressions;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class RegexAdvancedCases
{
    public static bool Case2101() { return Regex.IsMatch("2101", "^\\d{4}$"); }
    public static bool Case2102() { return Regex.IsMatch("abc2102", "\\A[a-z]+\\d+\\z"); }
    public static bool Case2103() { return Regex.IsMatch("2103", "(?=2103)"); }
    public static bool Case2104() { return Regex.IsMatch("2104x", "2104(?!y)"); }
    public static bool Case2105() { return Regex.IsMatch("x2105", "(?<=x)2105"); }
    public static bool Case2106() { return Regex.IsMatch("2106x", "2106(?=x)"); }
    public static bool Case2107() { var value = Regex.Match("id=2107", "id=(?<id>\\d+)"); return value.Groups["id"].Value == "2107"; }
    public static bool Case2108() { var value = Regex.Match("a=1;b=2108", "(?<key>\\w+)=(?<value>\\d+)", RegexOptions.RightToLeft); return value.Groups["value"].Value == "2108"; }
    public static bool Case2109() { var value = Regex.Matches("2109 2110", "\\d+"); return value.Count == 2 && value[1].Value == "2110"; }
    public static bool Case2110() { var value = Regex.Matches("a1b2c3", "\\d").Select(match => match.Value); return value.SequenceEqual(new[] { "1", "2", "3" }); }
    public static bool Case2111() { var value = Regex.Replace("2111", "(\\d)(\\d)(\\d)(\\d)", "$4$3$2$1"); return value == "1112"; }
    public static bool Case2112() { var value = Regex.Replace("a2112b", "\\d+", match => (int.Parse(match.Value) + 1).ToString()); return value == "a2113b"; }
    public static bool Case2113() { var value = Regex.Split("a,b;c", "[,;]"); return value.SequenceEqual(new[] { "a", "b", "c" }); }
    public static bool Case2114() { var value = Regex.Unescape(Regex.Escape("[2114]")); return value == "[2114]"; }
    public static bool Case2115() { var value = Regex.Unescape("\\[2115\\]"); return value == "[2115]"; }
    public static bool Case2116() { var value = Regex.Match("2116", "(?i)2116"); return value.Success; }
    public static bool Case2117() { var value = Regex.Match("ABC", "(?i)abc"); return value.Value == "ABC"; }
    public static bool Case2118() { var value = Regex.Match("2118", "(?n)(\\d+)"); return value.Groups.Count == 1; }
    public static bool Case2119() { var value = Regex.Match("2119", "(?<n>\\d+)"); return value.Groups["n"].Success; }
    public static bool Case2120() { var value = Regex.Match("2120", "(?>\\d+)"); return value.Success; }
    public static bool Case2121() { var value = Regex.Match("2121", "\\d+"); return value.Success; }
    public static bool Case2122() { var value = Regex.Match("2122", "\\d{2,4}"); return value.Value == "2122"; }
    public static bool Case2123() { var value = Regex.Match("2123", "\\d{2,4}?"); return value.Value == "21"; }
    public static bool Case2124() { var value = Regex.Match("2124", "\\d{4}"); return value.Index == 0 && value.Length == 4; }
    public static bool Case2125() { var value = Regex.Match("x2125y", "\\d+"); return value.Index == 1; }
    public static bool Case2126() { var value = Regex.Match("2126", "[12][0-9]{3}"); return value.Success; }
    public static bool Case2127() { var value = Regex.Match("2127", "[^a-z]+"); return value.Success; }
    public static bool Case2128() { var value = Regex.Match("a_2128", "\\w+"); return value.Value == "a_2128"; }
    public static bool Case2129() { var value = Regex.Match("a 2129", "\\s"); return value.Value == " "; }
    public static bool Case2130() { var value = Regex.Match("x2130", "\\p{L}+\\d+"); return value.Success; }
    public static bool Case2131() { var value = Regex.Match("é2131", "\\p{L}+\\p{N}+"); return value.Success; }
    public static bool Case2132() { var value = Regex.Match("2132", "\\D+"); return !value.Success; }
    public static bool Case2133() { var value = Regex.Match("abc", "\\Aabc\\z"); return value.Success; }
    public static bool Case2134() { var value = Regex.Match("abc\ndef", "^def$", RegexOptions.Multiline); return value.Success; }
    public static bool Case2135() { var value = Regex.Match("abc\ndef", "abc.def", RegexOptions.Singleline); return value.Success; }
    public static bool Case2136() { var value = Regex.Match("ABC", "abc", RegexOptions.IgnoreCase); return value.Success; }
    public static bool Case2137() { var value = Regex.Match("abc", "ABC", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); return value.Success; }
    public static bool Case2138() { var value = new Regex("2138", RegexOptions.Compiled); return value.IsMatch("2138"); }
    public static bool Case2139() { var value = new Regex("2139", RegexOptions.NonBacktracking); return value.IsMatch("2139"); }
    public static bool Case2140() { var value = Regex.Count("a2140b2140", "2140"); return value == 2; }
    public static bool Case2141() { var value = Regex.Count("abc", ""); return value == 4; }
    public static bool Case2142() { var value = Regex.IsMatch("2142", "\\d", RegexOptions.None, TimeSpan.FromSeconds(1)); return value; }
    public static bool Case2143() { var value = Regex.Match("2143", "2143", RegexOptions.None, TimeSpan.FromSeconds(1)); return value.Success; }
    public static bool Case2144() { var value = new Regex("2144", RegexOptions.None, TimeSpan.FromSeconds(1)); return value.IsMatch("2144"); }
    public static bool Case2145() { var value = Regex.Replace("2145", "2145", "x", RegexOptions.None, TimeSpan.FromSeconds(1)); return value == "x"; }
    public static bool Case2146() { var value = Regex.Split("2146", "x", RegexOptions.None, TimeSpan.FromSeconds(1)); return value.Single() == "2146"; }
    public static bool Case2147() { var value = Regex.Match("2147", "(?<n>\\d+)").Groups["n"].Captures; return value.Count == 1; }
    public static bool Case2148() { var value = Regex.Match("2148", "(\\d)+").Groups[1].Captures; return value.Count == 4; }
    public static bool Case2149() { var value = Regex.Match("aabb", "(?<open>a)+(?<-open>b)+(?(open)(?!))"); return value.Success; }
    public static bool Case2150() { var value = Regex.Match("(2150)", "\\((?<n>\\d+)\\)"); return value.Groups["n"].Value == "2150"; }
}
