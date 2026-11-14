using System.Text;
using System.Globalization;

internal static class TextFormatCases2
{
    public static bool TextFormat_2751() { return string.Format("{0}", 2751) == "2751"; }
    public static bool TextFormat_2752() { return string.Format("{0:D4}", 2752) == "2752"; }
    public static bool TextFormat_2753() { return string.Format("{0:X}", 2753) == "AC1"; }
    public static bool TextFormat_2754() { return string.Format("{0:N0}", 2754, CultureInfo.InvariantCulture).Contains("2"); }
    public static bool TextFormat_2755() { return 2755.ToString("D", CultureInfo.InvariantCulture) == "2755"; }
    public static bool TextFormat_2756() { return 2756.ToString("X", CultureInfo.InvariantCulture) == "AC4"; }
    public static bool TextFormat_2757() { return 2757.ToString("N0", CultureInfo.InvariantCulture) == "2,757"; }
    public static bool TextFormat_2758() { return 2758.5.ToString("F1", CultureInfo.InvariantCulture) == "2758.5"; }
    public static bool TextFormat_2759() { return 2759.25.ToString("P2", CultureInfo.InvariantCulture).Contains("27"); }
    public static bool TextFormat_2760() { return string.Join(',', new[] { 2, 7, 6, 0 }) == "2,7,6,0"; }
    public static bool TextFormat_2761() { return string.Join("|", new[] { "27", "61" }) == "27|61"; }
    public static bool TextFormat_2762() { return new StringBuilder("2762").Append('!').ToString() == "2762!"; }
    public static bool TextFormat_2763() { return new StringBuilder().AppendFormat("{0:D4}", 2763).ToString() == "2763"; }
    public static bool TextFormat_2764() { return new StringBuilder("2764").Equals(new StringBuilder("2764")); }
    public static bool TextFormat_2765() { return new StringBuilder("2765").ToString().AsSpan().SequenceEqual("2765".AsSpan()); }
    public static bool TextFormat_2766() { return "2766".CompareTo("2766") == 0; }
    public static bool TextFormat_2767() { return string.CompareOrdinal("2767", "2767") == 0; }
    public static bool TextFormat_2768() { return string.Equals("2768", "2768", StringComparison.Ordinal); }
    public static bool TextFormat_2769() { return "2769".Contains("76", StringComparison.Ordinal); }
    public static bool TextFormat_2770() { return "2770".IndexOf("70", StringComparison.Ordinal) == 2; }
    public static bool TextFormat_2771() { return "2771".LastIndexOf('7') == 2; }
    public static bool TextFormat_2772() { return "2772".PadLeft(6, '0') == "002772"; }
    public static bool TextFormat_2773() { return "2773".PadRight(6, '0') == "277300"; }
    public static bool TextFormat_2774() { return "2774".TrimStart('2') == "774"; }
    public static bool TextFormat_2775() { return "2775".TrimEnd('5') == "277"; }
    public static bool TextFormat_2776() { return "2776".Replace('7', 'x') == "2xx6"; }
    public static bool TextFormat_2777() { return "2777".Remove(2, 2) == "27"; }
    public static bool TextFormat_2778() { return "2778".Insert(2, ":") == "27:78"; }
    public static bool TextFormat_2779() { return "2779".Substring(1, 2) == "77"; }
    public static bool TextFormat_2780() { return "2780".Split('8').Length == 2; }
    public static bool TextFormat_2781() { return "2781".ToCharArray().Length == 4; }
    public static bool TextFormat_2782() { return new string(new[] { '2', '7', '8', '2' }) == "2782"; }
    public static bool TextFormat_2783() { return char.IsDigit('3') && char.GetNumericValue('3') == 3; }
    public static bool TextFormat_2784() { return char.IsLetter('x') && char.ToUpperInvariant('x') == 'X'; }
    public static bool TextFormat_2785() { return char.IsWhiteSpace(' ') && char.IsControl('\n'); }
    public static bool TextFormat_2786() { return char.IsPunctuation('!') && char.IsSymbol('+'); }
    public static bool TextFormat_2787() { return char.ConvertToUtf32("😀", 0) > 0xffff; }
    public static bool TextFormat_2788() { return char.ConvertFromUtf32(0x1f600).Length == 2; }
    public static bool TextFormat_2789() { return char.IsSurrogatePair('\ud83d', '\ude00'); }
    public static bool TextFormat_2790() { return char.IsHighSurrogate('\ud83d') && char.IsLowSurrogate('\ude00'); }
    public static bool TextFormat_2791() { return Rune.GetRuneAt("😀", 0).Value > 0; }
    public static bool TextFormat_2792() { return "😀".EnumerateRunes().Count() == 1; }
    public static bool TextFormat_2793() { return "a😀".EnumerateRunes().Count() == 2; }
    public static bool TextFormat_2794() { return Encoding.UTF8.GetByteCount("😀") == 4; }
    public static bool TextFormat_2795() { return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes("😀")) == "😀"; }
    public static bool TextFormat_2796() { return Encoding.Unicode.GetByteCount("2796") == 8; }
    public static bool TextFormat_2797() { return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes("2797")) == "2797"; }
    public static bool TextFormat_2798() { return Encoding.UTF32.GetByteCount("2798") == 16; }
    public static bool TextFormat_2799() { return Encoding.Latin1.GetBytes("2799").Length == 4; }
    public static bool TextFormat_2800() { return "2780".Normalize(NormalizationForm.FormC) == "2780"; }
}
