using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1700, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.7)]
internal static class ReflectionCryptoCases
{
    public static bool Case1151() { var type = typeof(ReflectionCryptoCases); return type.IsClass && type.IsAbstract && type.IsSealed; }
    public static bool Case1152() { var method = typeof(ReflectionCryptoCases).GetMethod(nameof(Case1152)); return method?.ReturnType == typeof(bool); }
    public static bool Case1153() { var fields = typeof(ReflectionCryptoCases).GetFields(BindingFlags.NonPublic | BindingFlags.Static); return fields.Any(field => field.FieldType == typeof(int) && (int)field.GetValue(null)! == 1153); }
    public static bool Case1154() { var property = typeof(ReflectionCryptoCases).GetProperty(nameof(PropertyValue), BindingFlags.NonPublic | BindingFlags.Static); return property?.GetValue(null) is int value && value == 1154; }
    public static bool Case1155() { var method = typeof(ReflectionCryptoCases).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(method => method.GetParameters().Length == 2 && method.ReturnType == typeof(int)); return method.Invoke(null, [1155, 0]) is 1155; }
    public static bool Case1156() { var type = typeof(List<int>); return type.IsGenericType && type.GetGenericArguments()[0] == typeof(int); }
    public static bool Case1157() { var type = typeof(Dictionary<string, int>); return type.GetGenericTypeDefinition() == typeof(Dictionary<,>); }
    public static bool Case1158() { return typeof(ReflectionCryptoCases).GetMethods(BindingFlags.Public | BindingFlags.Static).Length >= 50; }
    public static bool Case1159() { var assembly = typeof(ReflectionCryptoCases).Assembly; return assembly.GetType(typeof(ReflectionCryptoCases).FullName!) != null; }
    public static bool Case1160() { var constructor = typeof(DateTime).GetConstructor([typeof(long)]); return constructor != null; }
    public static bool Case1161() { var value = SHA256.HashData(Encoding.UTF8.GetBytes("1161")); return Convert.ToHexString(value).Length == 64; }
    public static bool Case1162() { var value = SHA1.HashData(Encoding.UTF8.GetBytes("1162")); return value.Length == 20; }
    public static bool Case1163() { var value = MD5.HashData(Encoding.UTF8.GetBytes("1163")); return value.Length == 16; }
    public static bool Case1164() { using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("key")); return hmac.ComputeHash(Encoding.UTF8.GetBytes("1164")).Length == 32; }
    public static bool Case1165() { using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes("key")); return hmac.ComputeHash(Encoding.UTF8.GetBytes("1165")).Length == 64; }
    public static bool Case1166() { using var aes = Aes.Create(); aes.GenerateKey(); aes.GenerateIV(); return aes.Key.Length >= 16 && aes.IV.Length == 16; }
    public static bool Case1167() { using var aes = Aes.Create(); aes.Key = SHA256.HashData([1, 1, 6, 7]); aes.GenerateIV(); using var encryptor = aes.CreateEncryptor(); var cipher = encryptor.TransformFinalBlock([1, 1, 6, 7], 0, 4); return cipher.Length > 0; }
    public static bool Case1168() { using var rng = RandomNumberGenerator.Create(); var bytes = new byte[16]; rng.GetBytes(bytes); return bytes.Any(value => value != 0) || bytes.All(value => value == 0); }
    public static bool Case1169() { var value = RandomNumberGenerator.GetInt32(1, 2); return value == 1; }
    public static bool Case1170() { var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)); return value.Length == 8; }
    public static bool Case1171() { var value = CryptographicOperations.FixedTimeEquals([1, 2], [1, 2]); return value; }
    public static bool Case1172() { var value = CryptographicOperations.FixedTimeEquals([1, 2], [1, 3]); return !value; }
    public static bool Case1173() { var value = new byte[] { 1, 2, 3 }; CryptographicOperations.ZeroMemory(value); return value.All(number => number == 0); }
    public static bool Case1174() { var value = Convert.FromHexString("1174"); return value.SequenceEqual(new byte[] { 0x11, 0x74 }); }
    public static bool Case1175() { var value = Convert.ToHexString([0x11, 0x75]); return value == "1175"; }
    public static bool Case1176() { var value = BitConverter.ToString([0x11, 0x76]); return value == "11-76"; }
    public static bool Case1177() { var value = Guid.NewGuid(); return value != Guid.Empty; }
    public static bool Case1178() { var value = Guid.NewGuid().ToByteArray(); return value.Length == 16; }
    public static bool Case1179() { var value = new System.Security.Claims.Claim("id", "1179"); return value.Type == "id" && value.Value == "1179"; }
    public static bool Case1180() { var identity = new System.Security.Principal.GenericIdentity("1180"); return identity.Name == "1180"; }
    public static bool Case1181() { var value = new System.Net.IPNetwork(System.Net.IPAddress.Parse("127.0.0.0"), 8); return value.Contains(System.Net.IPAddress.Loopback); }
    public static bool Case1182() { var value = System.Net.IPAddress.HostToNetworkOrder(1182); return System.Net.IPAddress.NetworkToHostOrder(value) == 1182; }
    public static bool Case1183() { var value = new Uri("mailto:test@example.test"); return value.Scheme == "mailto"; }
    public static bool Case1184() { var value = new System.Net.Mail.MailAddress("test@example.test"); return value.Address == "test@example.test"; }
    public static bool Case1185() { var value = new System.Net.Cookie("id", "1185"); return value.Value == "1185"; }
    public static bool Case1186() { var value = new System.Net.CookieContainer(); value.Add(new Uri("https://example.test"), new System.Net.Cookie("id", "1186")); return value.GetCookies(new Uri("https://example.test"))["id"]!.Value == "1186"; }
    public static bool Case1187() { var value = System.Text.RegularExpressions.Regex.IsMatch("1187", "^\\d+$"); return value; }
    public static bool Case1188() { var value = System.Text.RegularExpressions.Regex.Split("a1188b", "\\d+"); return value.SequenceEqual(new[] { "a", "b" }); }
    public static bool Case1189() { var value = System.Text.RegularExpressions.Regex.Escape("a.b"); return value == "a\\.b"; }
    public static bool Case1190() { var value = System.Text.RegularExpressions.Regex.Replace("1190", "(?<n>\\d+)", "${n}"); return value == "1190"; }
    public static bool Case1191() { var value = new System.Text.RegularExpressions.Regex("(?i)abc").IsMatch("ABC"); return value; }
    public static bool Case1192() { var value = new System.Text.RegularExpressions.Regex("a+").Count("aaab"); return value == 1; }
    public static bool Case1193() { var value = new System.Text.RegularExpressions.Regex("\\d").Matches("1193").Count; return value == 4; }
    public static bool Case1194() { var value = new System.Text.RegularExpressions.Regex("(?<id>\\d+)").Match("id=1194").Groups["id"].Value; return value == "1194"; }
    public static bool Case1195() { var value = new System.Text.RegularExpressions.Regex("a|b").Replace("ab", "x"); return value == "xx"; }
    public static bool Case1196() { var value = new Uri("https://example.test/path").GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped); return value == "https://example.test"; }
    public static bool Case1197() { var value = Uri.CheckHostName("example.test"); return value == UriHostNameType.Dns; }
    public static bool Case1198() { var value = Uri.TryCreate("https://example.test/1198", UriKind.Absolute, out var uri); return value && uri!.AbsolutePath == "/1198"; }
    public static bool Case1199() { var value = System.Net.WebUtility.HtmlEncode("1199 & x"); return value == "1199 &amp; x"; }
    public static bool Case1200() { var value = typeof(ReflectionCryptoCases).GetNestedTypes(BindingFlags.NonPublic).Length; return value >= 1; }

    private static int PrivateValue = 1153;
    private static int PropertyValue => 1154;
    private static int Add(int left, int right) => left + right;
}
