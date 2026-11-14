using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1500, BogusStates = 6, OpaqueDepth = 3,
    DecryptHashChance = 0.65)]
internal static class TextParsingCases {
    public static bool TextParsing_0901() { return Regex.IsMatch("user.901@example.test", @"^[\w.]+@[\w.]+$"); }
    public static bool TextParsing_0902() { var match = Regex.Match("192.0.2.90", @"^(\d{1,3}\.){3}\d{1,3}$"); return match.Success && match.Length == 10; }
    public static bool TextParsing_0903() { var match = Regex.Match("case=903", @"(?<key>\w+)=(?<value>\d+)"); return match.Groups["key"].Value == "case" && match.Groups["value"].Value == "903"; }
    public static bool TextParsing_0904() { var match = Regex.Match("1,2,3,4", @"(\d)(?:,|$)"); return match.Success && match.Groups[1].Value == "1"; }
    public static bool TextParsing_0905() { var output = Regex.Replace("9-0-5", @"\d", match => ((int.Parse(match.Value) + 1) % 10).ToString()); return output == "0-1-6"; }
    public static bool TextParsing_0906() { return Regex.Split("a  b\t c", @"\s+").SequenceEqual(new[] { "a", "b", "c" }); }
    public static bool TextParsing_0907() { var regex = new Regex(@"^case-\d{3}$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)); return regex.IsMatch("case-907"); }
    public static bool TextParsing_0908() { return Regex.IsMatch("CaSe-908", "case-908", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant); }
    public static bool TextParsing_0909() { var matches = Regex.Matches("first\n909\nlast", @"^\d+$", RegexOptions.Multiline); return matches.Single().Value == "909"; }
    public static bool TextParsing_0910() { return Regex.Match("a\n910\nz", @"a.*z", RegexOptions.Singleline).Success; }
    public static bool TextParsing_0911() { return Regex.IsMatch("word word", @"^(\w+)\s+\1$"); }
    public static bool TextParsing_0912() { var match = Regex.Match("case912", @"case(?=912)"); return match.Success && match.Value == "case"; }
    public static bool TextParsing_0913() { return Regex.IsMatch("case-913", @"^case-(?!000)\d+$") && !Regex.IsMatch("case-000", @"^case-(?!000)\d+$"); }
    public static bool TextParsing_0914() { var matches = Regex.Matches("cat dog fox", @"cat|fox"); return matches.Count == 2 && matches[1].Value == "fox"; }
    public static bool TextParsing_0915() { return Regex.IsMatch("aaabbbbcc", @"^a{3}b{2,4}c+$"); }
    public static bool TextParsing_0916() { var literal = "a+b?(916)"; return Regex.Unescape(Regex.Escape(literal)) == literal; }
    public static bool TextParsing_0917() { var total = 0; foreach (var match in Regex.EnumerateMatches("9a1b7", @"\d")) total += match.Length; return total == 3; }
    public static bool TextParsing_0918() { return Regex.Count("918-18-8", @"\d") == 6; }
    public static bool TextParsing_0919() { ReadOnlySpan<char> input = "prefix-919-suffix"; var match = Regex.Match(input.ToString(), @"\d+"); return match.ValueSpan.SequenceEqual("919"); }
    public static bool TextParsing_0920() { var builder = new StringBuilder(); for (var value = 0; value < 5; value++) builder.Append(value); return builder.ToString() == "01234" && builder.Length == 5; }
    public static bool TextParsing_0921() { return char.GetUnicodeCategory('λ') == UnicodeCategory.LowercaseLetter && char.GetUnicodeCategory('9') == UnicodeCategory.DecimalDigitNumber; }
    public static bool TextParsing_0922() { var rune = Rune.GetRuneAt("🦊", 0); Span<char> chars = stackalloc char[2]; return rune.EncodeToUtf16(chars) == 2 && chars.SequenceEqual("🦊"); }
    public static bool TextParsing_0923() { const string decomposed = "A\u030A"; return decomposed.Normalize(NormalizationForm.FormC) == "Å" && !decomposed.IsNormalized(NormalizationForm.FormC); }
    public static bool TextParsing_0924() { var info = new StringInfo("A🦊e\u0301"); return info.LengthInTextElements == 3 && info.SubstringByTextElements(1, 1) == "🦊"; }
    public static bool TextParsing_0925() { var elements = StringInfo.GetTextElementEnumerator("x🦊y"); var count = 0; while (elements.MoveNext()) count++; return count == 3; }
    public static bool TextParsing_0926() { var compare = CultureInfo.GetCultureInfo("en-US").CompareInfo; return compare.IndexOf("café-926", "CAFE", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0; }
    public static bool TextParsing_0927() { var compare = CultureInfo.InvariantCulture.CompareInfo; var first = compare.GetSortKey("abc"); var second = compare.GetSortKey("abd"); return SortKey.Compare(first, second) < 0; }
    public static bool TextParsing_0928() { var turkish = CultureInfo.GetCultureInfo("tr-TR"); return "i".ToUpper(turkish) == "İ" && "I".ToLower(turkish) == "ı"; }
    public static bool TextParsing_0929() { return string.Equals("CASE-929".ToLowerInvariant(), "case-929", StringComparison.Ordinal); }
    public static bool TextParsing_0930() { var encoding = Encoding.GetEncoding("us-ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback); try { _ = encoding.GetBytes("λ"); return false; } catch (EncoderFallbackException) { return true; } }
    public static bool TextParsing_0931() { var encoding = Encoding.GetEncoding(Encoding.ASCII.CodePage, new EncoderReplacementFallback("?"), DecoderFallback.ReplacementFallback); return Encoding.ASCII.GetString(encoding.GetBytes("AλB")) == "A?B"; }
    public static bool TextParsing_0932() { var preamble = new UTF8Encoding(true).GetPreamble(); return preamble.SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }); }
    public static bool TextParsing_0933() { var text = Encoding.UTF8.GetString(new byte[] { 0xff, 0x39, 0x33 }); return text.Contains('\ufffd') && text.EndsWith("93", StringComparison.Ordinal); }
    public static bool TextParsing_0934() { var input = Enumerable.Range(0, 934).Select(value => (byte)value).ToArray(); return Convert.FromBase64String(Convert.ToBase64String(input, Base64FormattingOptions.InsertLineBreaks)).SequenceEqual(input); }
    public static bool TextParsing_0935() { var pair = char.ConvertFromUtf32(0x1f98a); return pair.Length == 2 && char.ConvertToUtf32(pair, 0) == 0x1f98a; }
    public static bool TextParsing_0936() { var high = '\ud83e'; var low = '\udd8a'; return char.IsSurrogatePair(high, low) && char.ConvertToUtf32(high, low) == 0x1f98a; }
    public static bool TextParsing_0937() { FormattableString value = $"case={937:D4}, rate={1.25:F2}"; return value.Format == "case={0:D4}, rate={1:F2}" && value.GetArguments().Length == 2; }
    public static bool TextParsing_0938() { var format = CompositeFormat.Parse("{0}-{1:D3}"); return string.Format(CultureInfo.InvariantCulture, format, "case", 938) == "case-938"; }
    public static bool TextParsing_0939() { var value = 939; return $"[{value,5:D3}]" == "[  939]"; }
    public static bool TextParsing_0940() { var output = string.Create(CultureInfo.InvariantCulture, $"case-{940:X}"); return output == "case-3AC"; }
    public static bool TextParsing_0941() { var values = new[] { "9", "4", "1" }; return string.Join(':', values) == "9:4:1"; }
    public static bool TextParsing_0942() { return string.Concat(Enumerable.Repeat("x", 942)).Length == 942; }
    public static bool TextParsing_0943() { var builder = new StringBuilder("case-943-case"); builder.Replace("case", "test"); return builder.ToString() == "test-943-test"; }
    public static bool TextParsing_0944() { var builder = new StringBuilder("944"); builder.Insert(0, "case-"); builder.Remove(4, 1); return builder.ToString() == "case944"; }
    public static bool TextParsing_0945() { var builder = new StringBuilder(); builder.AppendFormat(CultureInfo.InvariantCulture, "{0:D3}/{1:F1}", 945, 9.45); return builder.ToString() == "945/9.4"; }
    public static bool TextParsing_0946() { var builder = new StringBuilder(4); var capacity = builder.EnsureCapacity(946); builder.Append('x', 946); return capacity >= 946 && builder.Length == 946; }
    public static bool TextParsing_0947() { ReadOnlySpan<char> input = "a|bb|ccc|947"; var lengths = new List<int>(); while (true) { var split = input.IndexOf('|'); if (split < 0) { lengths.Add(input.Length); break; } lengths.Add(split); input = input[(split + 1)..]; } return lengths.SequenceEqual(new[] { 1, 2, 3, 3 }); }
    public static bool TextParsing_0948() { using var reader = new StringReader("alpha\r\nbeta\ngamma"); var lines = new List<string>(); string? line; while ((line = reader.ReadLine()) is not null) lines.Add(line); return lines.SequenceEqual(new[] { "alpha", "beta", "gamma" }); }
    public static bool TextParsing_0949() { var words = "nine four nine words wrap cleanly".Split(' '); var lines = new List<string>(); var current = new StringBuilder(); foreach (var word in words) { if (current.Length > 0 && current.Length + 1 + word.Length > 12) { lines.Add(current.ToString()); current.Clear(); } if (current.Length > 0) current.Append(' '); current.Append(word); } lines.Add(current.ToString()); return lines.All(line => line.Length <= 12) && string.Join(' ', lines) == string.Join(' ', words); }
    public static bool TextParsing_0950() { const string input = "run \"case 950\" --fast"; var tokens = new List<string>(); var current = new StringBuilder(); var quoted = false; foreach (var character in input) { if (character == '"') { quoted = !quoted; continue; } if (character == ' ' && !quoted) { if (current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); } } else current.Append(character); } if (current.Length > 0) tokens.Add(current.ToString()); return tokens.SequenceEqual(new[] { "run", "case 950", "--fast" }); }
}
