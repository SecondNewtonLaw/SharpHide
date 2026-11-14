using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using SharpHide.Vendor;

namespace ILTestLand;

internal static class HandwrittenCases {
    private static readonly Type[] Suites = [typeof(IntegerArithmeticCases), typeof(ControlFlowLoopCases),
        typeof(LibraryCollectionCases), typeof(IoStreamCases), typeof(NetworkCases), typeof(ConcurrencyCases),
        typeof(CryptographyEncodingCases), typeof(DateTimeNumericCases), typeof(SerializationReflectionCases),
        typeof(WorkflowCases), typeof(BufferMemoryCases), typeof(AdvancedCollectionCases), typeof(AdvancedIoCases),
        typeof(ControlFlowExceptionCases), typeof(NetworkProtocolCases), typeof(TextParsingCases),
        typeof(ObjectModelCases), typeof(ConcurrencyNumericCases), typeof(FormatParserCases), typeof(IoAlgorithmCases), typeof(ReflectionCryptoCases), typeof(AdvancedAlgorithmCases), typeof(ProtocolStateCases), typeof(AsyncPipelineCases), typeof(EncodingDataCases), typeof(ResourceLifetimeCases), typeof(CacheStateCases), typeof(GenericMathCases), typeof(ContractReflectionCases), typeof(ProtocolInteropCases), typeof(CompressionArchiveCases), typeof(DataTableCases), typeof(ComponentModelCases), typeof(DiagnosticsRuntimeCases), typeof(SerializationContractCases), typeof(NetworkMetadataCases), typeof(SpecializedCollectionCases), typeof(RuntimeHelperCases), typeof(FileSystemMetadataCases), typeof(RegexAdvancedCases), typeof(ParsingCultureCases), typeof(MathBitCases), typeof(LoopCollectionCases), typeof(BufferSpanCases), typeof(DataFormatCases), typeof(AsyncSyncCases), typeof(CryptoHashCases), typeof(NetworkProtocolCases2), typeof(ReflectionObjectCases2), typeof(XmlDomCases), typeof(DateCultureCases2), typeof(AlgorithmCases2), typeof(TextFormatCases2), typeof(RuntimeEnvironmentCases), typeof(LanguageSemanticsCases), typeof(FinalCases)];

    private static readonly string[] SuiteNames = ["Integer arithmetic", "Control-flow loops", "Library collections", "I/O streams", "Networking", "Concurrency", "Cryptography and encoding", "Date/time and numeric", "Serialization and reflection", "Workflows", "Buffer and memory", "Advanced collections", "Advanced I/O", "Control-flow exceptions", "Network protocols", "Text parsing", "Object model", "Concurrent numerics", "Format parsers", "I/O algorithms", "Reflection and crypto", "Advanced algorithms", "Protocol state", "Async pipelines", "Encoding data", "Resource lifetime", "Cache state", "Generic math", "Contract reflection", "Protocol interop", "Compression and archives", "Data tables", "Component model", "Diagnostics and runtime", "Serialization contracts", "Network metadata", "Specialized collections", "Runtime helpers", "Filesystem metadata", "Advanced regular expressions", "Parsing and culture", "Math and bits", "Collection loops", "Buffer and spans", "Data formats", "Async/sync", "Crypto hashes", "Network protocols 2", "Reflection objects 2", "XML DOM", "Date/culture 2", "Algorithms 2", "Text formats 2", "Runtime environment", "Language semantics", "Final cases"];

    public static int Run(Action<string, object?, object?> check) {
        var count = 0;
        for (var suiteIndex = 0; suiteIndex < Suites.Length; suiteIndex++) {
            var suite = Suites[suiteIndex];
            var suiteName = SuiteNames[suiteIndex];
            foreach (var method in suite.GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .OrderBy(method => method.Name, StringComparer.Ordinal)) {
                var suiteLabel = suite.Name.EndsWith("Cases", StringComparison.Ordinal)
                    ? suite.Name[..^"Cases".Length]
                    : suite.Name;
                var testLabel = method.Name.StartsWith("Case", StringComparison.Ordinal)
                    ? "Test" + method.Name["Case".Length..]
                    : method.Name;
                var name = suiteName + "." + suiteLabel + "." + testLabel;
                try {
                    check(name, method.Invoke(null, null), true);
                }
                catch (TargetInvocationException error) {
                    check(name, error.InnerException?.ToString(), "no exception");
                }
                count++;
            }
        }
        return count;
    }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 400, BogusStates = 4, OpaqueDepth = 2,
    DecryptHashChance = 0.5)]
internal static class IntegerArithmeticCases {
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Input(int value) => value;

    public static bool Case0001() { var x = Input(1); return (x + 17) - 17 == x; }
    public static bool Case0002() { var x = Input(2); return (x - 29) + 29 == x; }
    public static bool Case0003() { var x = Input(3); return unchecked(x * 7 / 7) == x; }
    public static bool Case0004() { var x = Input(4); return ((x ^ 0x55) ^ 0x55) == x; }
    public static bool Case0005() { var x = Input(5); return ((x | 0x40) - (x & 0x40)) == (x ^ 0x40); }
    public static bool Case0006() { var x = Input(6); return (x << 3) >> 3 == x; }
    public static bool Case0007() { var x = Input(7); return x % 5 == 2; }
    public static bool Case0008() { var x = Input(8); return -(-x) == x; }
    public static bool Case0009() { var x = Input(9); return ~x == -x - 1; }
    public static bool Case0010() { var x = Input(10); return checked(x + 100) == 110; }
    public static bool Case0011() { var x = Input(11); return (x + 31) - 31 == x; }
    public static bool Case0012() { var x = Input(12); return (x - 43) + 43 == x; }
    public static bool Case0013() { var x = Input(13); return unchecked(x * 9 / 9) == x; }
    public static bool Case0014() { var x = Input(14); return ((x ^ 0xAA) ^ 0xAA) == x; }
    public static bool Case0015() { var x = Input(15); return ((x | 0x80) - (x & 0x80)) == (x ^ 0x80); }
    public static bool Case0016() { var x = Input(16); return (x << 4) >> 4 == x; }
    public static bool Case0017() { var x = Input(17); return x % 6 == 5; }
    public static bool Case0018() { var x = Input(18); return -(-x) == x; }
    public static bool Case0019() { var x = Input(19); return ~x == -x - 1; }
    public static bool Case0020() { var x = Input(20); return checked(x + 200) == 220; }
    public static bool Case0021() { var x = Input(21); return (x + 47) - 47 == x; }
    public static bool Case0022() { var x = Input(22); return (x - 53) + 53 == x; }
    public static bool Case0023() { var x = Input(23); return unchecked(x * 11 / 11) == x; }
    public static bool Case0024() { var x = Input(24); return ((x ^ 0x1234) ^ 0x1234) == x; }
    public static bool Case0025() { var x = Input(25); return ((x | 0x100) - (x & 0x100)) == (x ^ 0x100); }
    public static bool Case0026() { var x = Input(26); return (x << 5) >> 5 == x; }
    public static bool Case0027() { var x = Input(27); return x % 7 == 6; }
    public static bool Case0028() { var x = Input(28); return -(-x) == x; }
    public static bool Case0029() { var x = Input(29); return ~x == -x - 1; }
    public static bool Case0030() { var x = Input(30); return checked(x + 300) == 330; }
    public static bool Case0031() { var x = Input(31); return (x + 59) - 59 == x; }
    public static bool Case0032() { var x = Input(32); return (x - 61) + 61 == x; }
    public static bool Case0033() { var x = Input(33); return unchecked(x * 13 / 13) == x; }
    public static bool Case0034() { var x = Input(34); return ((x ^ 0x4321) ^ 0x4321) == x; }
    public static bool Case0035() { var x = Input(35); return ((x | 0x200) - (x & 0x200)) == (x ^ 0x200); }
    public static bool Case0036() { var x = Input(36); return (x << 6) >> 6 == x; }
    public static bool Case0037() { var x = Input(37); return x % 8 == 5; }
    public static bool Case0038() { var x = Input(38); return -(-x) == x; }
    public static bool Case0039() { var x = Input(39); return ~x == -x - 1; }
    public static bool Case0040() { var x = Input(40); return checked(x + 400) == 440; }
    public static bool Case0041() { var x = Input(41); return (x + 67) - 67 == x; }
    public static bool Case0042() { var x = Input(42); return (x - 71) + 71 == x; }
    public static bool Case0043() { var x = Input(43); return unchecked(x * 15 / 15) == x; }
    public static bool Case0044() { var x = Input(44); return ((x ^ 0x2468) ^ 0x2468) == x; }
    public static bool Case0045() { var x = Input(45); return ((x | 0x400) - (x & 0x400)) == (x ^ 0x400); }
    public static bool Case0046() { var x = Input(46); return (x << 7) >> 7 == x; }
    public static bool Case0047() { var x = Input(47); return x % 9 == 2; }
    public static bool Case0048() { var x = Input(48); return -(-x) == x; }
    public static bool Case0049() { var x = Input(49); return ~x == -x - 1; }
    public static bool Case0050() { var x = Input(50); return checked(x + 500) == 550; }
    public static bool Case0051() { var x = Input(51); return (x + 73) - 73 == x; }
    public static bool Case0052() { var x = Input(52); return (x - 79) + 79 == x; }
    public static bool Case0053() { var x = Input(53); return unchecked(x * 17 / 17) == x; }
    public static bool Case0054() { var x = Input(54); return ((x ^ 0x1357) ^ 0x1357) == x; }
    public static bool Case0055() { var x = Input(55); return ((x | 0x800) - (x & 0x800)) == (x ^ 0x800); }
    public static bool Case0056() { var x = Input(56); return (x << 8) >> 8 == x; }
    public static bool Case0057() { var x = Input(57); return x % 10 == 7; }
    public static bool Case0058() { var x = Input(58); return -(-x) == x; }
    public static bool Case0059() { var x = Input(59); return ~x == -x - 1; }
    public static bool Case0060() { var x = Input(60); return checked(x + 600) == 660; }
    public static bool Case0061() { var x = Input(61); return (x + 83) - 83 == x; }
    public static bool Case0062() { var x = Input(62); return (x - 89) + 89 == x; }
    public static bool Case0063() { var x = Input(63); return unchecked(x * 19 / 19) == x; }
    public static bool Case0064() { var x = Input(64); return ((x ^ 0xCAFE) ^ 0xCAFE) == x; }
    public static bool Case0065() { var x = Input(65); return ((x | 0x1000) - (x & 0x1000)) == (x ^ 0x1000); }
    public static bool Case0066() { var x = Input(66); return (x << 9) >> 9 == x; }
    public static bool Case0067() { var x = Input(67); return x % 11 == 1; }
    public static bool Case0068() { var x = Input(68); return -(-x) == x; }
    public static bool Case0069() { var x = Input(69); return ~x == -x - 1; }
    public static bool Case0070() { var x = Input(70); return checked(x + 700) == 770; }
    public static bool Case0071() { var x = Input(71); return (x + 97) - 97 == x; }
    public static bool Case0072() { var x = Input(72); return (x - 101) + 101 == x; }
    public static bool Case0073() { var x = Input(73); return unchecked(x * 21 / 21) == x; }
    public static bool Case0074() { var x = Input(74); return ((x ^ 0xBEEF) ^ 0xBEEF) == x; }
    public static bool Case0075() { var x = Input(75); return ((x | 0x2000) - (x & 0x2000)) == (x ^ 0x2000); }
    public static bool Case0076() { var x = Input(76); return (x << 10) >> 10 == x; }
    public static bool Case0077() { var x = Input(77); return x % 12 == 5; }
    public static bool Case0078() { var x = Input(78); return -(-x) == x; }
    public static bool Case0079() { var x = Input(79); return ~x == -x - 1; }
    public static bool Case0080() { var x = Input(80); return checked(x + 800) == 880; }
    public static bool Case0081() { var x = Input(81); return (x + 103) - 103 == x; }
    public static bool Case0082() { var x = Input(82); return (x - 107) + 107 == x; }
    public static bool Case0083() { var x = Input(83); return unchecked(x * 23 / 23) == x; }
    public static bool Case0084() { var x = Input(84); return ((x ^ 0xDEAD) ^ 0xDEAD) == x; }
    public static bool Case0085() { var x = Input(85); return ((x | 0x4000) - (x & 0x4000)) == (x ^ 0x4000); }
    public static bool Case0086() { var x = Input(86); return (x << 11) >> 11 == x; }
    public static bool Case0087() { var x = Input(87); return x % 13 == 9; }
    public static bool Case0088() { var x = Input(88); return -(-x) == x; }
    public static bool Case0089() { var x = Input(89); return ~x == -x - 1; }
    public static bool Case0090() { var x = Input(90); return checked(x + 900) == 990; }
    public static bool Case0091() { var x = Input(91); return (x + 109) - 109 == x; }
    public static bool Case0092() { var x = Input(92); return (x - 113) + 113 == x; }
    public static bool Case0093() { var x = Input(93); return unchecked(x * 25 / 25) == x; }
    public static bool Case0094() { var x = Input(94); return ((x ^ 0xFACE) ^ 0xFACE) == x; }
    public static bool Case0095() { var x = Input(95); return ((x | 0x8000) - (x & 0x8000)) == (x ^ 0x8000); }
    public static bool Case0096() { var x = Input(96); return (x << 12) >> 12 == x; }
    public static bool Case0097() { var x = Input(97); return x % 14 == 13; }
    public static bool Case0098() { var x = Input(98); return -(-x) == x; }
    public static bool Case0099() { var x = Input(99); return ~x == -x - 1; }
    public static bool Case0100() { var x = Input(100); return checked(x + 1000) == 1100; }
}

[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 500, BogusStates = 7, OpaqueDepth = 4,
    DecryptHashChance = 0.75)]
internal static class ControlFlowLoopCases {
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Input(int value) => value;

    public static bool Case0101() { var n = Input(1); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0102() { var n = Input(2); var c = 0; while (n-- > 0) c++; return c == 2; }
    public static bool Case0103() { var n = Input(3); var c = 0; do { c++; } while (c < n); return c == 3; }
    public static bool Case0104() { var n = Input(4); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0105() { var n = Input(5); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 5; }
    public static bool Case0106() { var n = Input(6); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 18; }
    public static bool Case0107() { var n = Input(7); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0108() { var n = Input(8); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0109() { var n = Input(9); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 10; }
    public static bool Case0110() { var n = Input(10); var c = 0; Start: if (c++ < n) goto Start; return c == 11; }
    public static bool Case0111() { var n = Input(11); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0112() { var n = Input(12); var c = 0; while (n-- > 0) c++; return c == 12; }
    public static bool Case0113() { var n = Input(13); var c = 0; do { c++; } while (c < n); return c == 13; }
    public static bool Case0114() { var n = Input(14); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0115() { var n = Input(15); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 15; }
    public static bool Case0116() { var n = Input(16); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 48; }
    public static bool Case0117() { var n = Input(17); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0118() { var n = Input(18); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0119() { var n = Input(19); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 20; }
    public static bool Case0120() { var n = Input(20); var c = 0; Start: if (c++ < n) goto Start; return c == 21; }
    public static bool Case0121() { var n = Input(21); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0122() { var n = Input(22); var c = 0; while (n-- > 0) c++; return c == 22; }
    public static bool Case0123() { var n = Input(23); var c = 0; do { c++; } while (c < n); return c == 23; }
    public static bool Case0124() { var n = Input(24); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0125() { var n = Input(25); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 25; }
    public static bool Case0126() { var n = Input(26); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 78; }
    public static bool Case0127() { var n = Input(27); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0128() { var n = Input(28); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0129() { var n = Input(29); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 30; }
    public static bool Case0130() { var n = Input(30); var c = 0; Start: if (c++ < n) goto Start; return c == 31; }
    public static bool Case0131() { var n = Input(31); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0132() { var n = Input(32); var c = 0; while (n-- > 0) c++; return c == 32; }
    public static bool Case0133() { var n = Input(33); var c = 0; do { c++; } while (c < n); return c == 33; }
    public static bool Case0134() { var n = Input(34); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0135() { var n = Input(35); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 35; }
    public static bool Case0136() { var n = Input(36); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 108; }
    public static bool Case0137() { var n = Input(37); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0138() { var n = Input(38); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0139() { var n = Input(39); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 40; }
    public static bool Case0140() { var n = Input(40); var c = 0; Start: if (c++ < n) goto Start; return c == 41; }
    public static bool Case0141() { var n = Input(41); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0142() { var n = Input(42); var c = 0; while (n-- > 0) c++; return c == 42; }
    public static bool Case0143() { var n = Input(43); var c = 0; do { c++; } while (c < n); return c == 43; }
    public static bool Case0144() { var n = Input(44); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0145() { var n = Input(45); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 45; }
    public static bool Case0146() { var n = Input(46); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 138; }
    public static bool Case0147() { var n = Input(47); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0148() { var n = Input(48); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0149() { var n = Input(49); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 50; }
    public static bool Case0150() { var n = Input(50); var c = 0; Start: if (c++ < n) goto Start; return c == 51; }
    public static bool Case0151() { var n = Input(51); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0152() { var n = Input(52); var c = 0; while (n-- > 0) c++; return c == 52; }
    public static bool Case0153() { var n = Input(53); var c = 0; do { c++; } while (c < n); return c == 53; }
    public static bool Case0154() { var n = Input(54); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0155() { var n = Input(55); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 55; }
    public static bool Case0156() { var n = Input(56); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 168; }
    public static bool Case0157() { var n = Input(57); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0158() { var n = Input(58); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0159() { var n = Input(59); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 60; }
    public static bool Case0160() { var n = Input(60); var c = 0; Start: if (c++ < n) goto Start; return c == 61; }
    public static bool Case0161() { var n = Input(61); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0162() { var n = Input(62); var c = 0; while (n-- > 0) c++; return c == 62; }
    public static bool Case0163() { var n = Input(63); var c = 0; do { c++; } while (c < n); return c == 63; }
    public static bool Case0164() { var n = Input(64); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0165() { var n = Input(65); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 65; }
    public static bool Case0166() { var n = Input(66); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 198; }
    public static bool Case0167() { var n = Input(67); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0168() { var n = Input(68); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0169() { var n = Input(69); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 70; }
    public static bool Case0170() { var n = Input(70); var c = 0; Start: if (c++ < n) goto Start; return c == 71; }
    public static bool Case0171() { var n = Input(71); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0172() { var n = Input(72); var c = 0; while (n-- > 0) c++; return c == 72; }
    public static bool Case0173() { var n = Input(73); var c = 0; do { c++; } while (c < n); return c == 73; }
    public static bool Case0174() { var n = Input(74); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0175() { var n = Input(75); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 75; }
    public static bool Case0176() { var n = Input(76); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 228; }
    public static bool Case0177() { var n = Input(77); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0178() { var n = Input(78); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0179() { var n = Input(79); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 80; }
    public static bool Case0180() { var n = Input(80); var c = 0; Start: if (c++ < n) goto Start; return c == 81; }
    public static bool Case0181() { var n = Input(81); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0182() { var n = Input(82); var c = 0; while (n-- > 0) c++; return c == 82; }
    public static bool Case0183() { var n = Input(83); var c = 0; do { c++; } while (c < n); return c == 83; }
    public static bool Case0184() { var n = Input(84); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0185() { var n = Input(85); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 85; }
    public static bool Case0186() { var n = Input(86); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 258; }
    public static bool Case0187() { var n = Input(87); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0188() { var n = Input(88); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0189() { var n = Input(89); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 90; }
    public static bool Case0190() { var n = Input(90); var c = 0; Start: if (c++ < n) goto Start; return c == 91; }
    public static bool Case0191() { var n = Input(91); var s = 0; for (var i = 0; i <= n; i++) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0192() { var n = Input(92); var c = 0; while (n-- > 0) c++; return c == 92; }
    public static bool Case0193() { var n = Input(93); var c = 0; do { c++; } while (c < n); return c == 93; }
    public static bool Case0194() { var n = Input(94); var s = 0; for (var i = 1; i < n * 2; i++) { if ((i & 1) == 0) continue; s += i; } return s == n * n; }
    public static bool Case0195() { var n = Input(95); var x = -1; for (var i = 0; ; i++) { if (i != n) continue; x = i; break; } return x == 95; }
    public static bool Case0196() { var n = Input(96); var c = 0; for (var i = 0; i < n; i++) for (var j = 0; j < 3; j++) c++; return c == 288; }
    public static bool Case0197() { var n = Input(97); var s = 0; for (var i = 0; i < n; i++) s += (i % 3) switch { 0 => i, 1 => -i, _ => i * 2 }; return s == Enumerable.Range(0, n).Sum(i => i % 3 == 0 ? i : i % 3 == 1 ? -i : i * 2); }
    public static bool Case0198() { var n = Input(98); var s = 0; foreach (var i in Enumerable.Range(1, n)) s += i; return s == n * (n + 1) / 2; }
    public static bool Case0199() { var n = Input(99); var c = 0; try { for (var i = 0; i < n; i++) c++; } finally { c++; } return c == 100; }
    public static bool Case0200() { var n = Input(100); var c = 0; Start: if (c++ < n) goto Start; return c == 101; }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 800, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class LibraryCollectionCases {
    public static bool Case0201() { var x = 201; return $"item-{x}".ToUpperInvariant() == "ITEM-201"; }
    public static bool Case0202() { var x = 202; return $"a,b,{x}".Split(',')[2] == "202"; }
    public static bool Case0203() { var x = 203; return new StringBuilder().Append("x=").Append(x).ToString() == "x=203"; }
    public static bool Case0204() { var x = 204; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0205() { var x = 205; var v = new List<int> { x, 2, 3 }; return v.Sum() == 210; }
    public static bool Case0206() { var x = 206; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 206; }
    public static bool Case0207() { var x = 207; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0208() { var x = 208; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 208 && q.Peek() == 209; }
    public static bool Case0209() { var x = 209; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0210() { var x = 210; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 210, 211 }); }
    public static bool Case0211() { var x = 211; return $"item-{x}".ToUpperInvariant() == "ITEM-211"; }
    public static bool Case0212() { var x = 212; return $"a,b,{x}".Split(',')[2] == "212"; }
    public static bool Case0213() { var x = 213; return new StringBuilder().Append("x=").Append(x).ToString() == "x=213"; }
    public static bool Case0214() { var x = 214; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0215() { var x = 215; var v = new List<int> { x, 2, 3 }; return v.Sum() == 220; }
    public static bool Case0216() { var x = 216; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 216; }
    public static bool Case0217() { var x = 217; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0218() { var x = 218; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 218 && q.Peek() == 219; }
    public static bool Case0219() { var x = 219; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0220() { var x = 220; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 220, 221 }); }
    public static bool Case0221() { var x = 221; return $"item-{x}".ToUpperInvariant() == "ITEM-221"; }
    public static bool Case0222() { var x = 222; return $"a,b,{x}".Split(',')[2] == "222"; }
    public static bool Case0223() { var x = 223; return new StringBuilder().Append("x=").Append(x).ToString() == "x=223"; }
    public static bool Case0224() { var x = 224; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0225() { var x = 225; var v = new List<int> { x, 2, 3 }; return v.Sum() == 230; }
    public static bool Case0226() { var x = 226; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 226; }
    public static bool Case0227() { var x = 227; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0228() { var x = 228; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 228 && q.Peek() == 229; }
    public static bool Case0229() { var x = 229; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0230() { var x = 230; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 230, 231 }); }
    public static bool Case0231() { var x = 231; return $"item-{x}".ToUpperInvariant() == "ITEM-231"; }
    public static bool Case0232() { var x = 232; return $"a,b,{x}".Split(',')[2] == "232"; }
    public static bool Case0233() { var x = 233; return new StringBuilder().Append("x=").Append(x).ToString() == "x=233"; }
    public static bool Case0234() { var x = 234; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0235() { var x = 235; var v = new List<int> { x, 2, 3 }; return v.Sum() == 240; }
    public static bool Case0236() { var x = 236; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 236; }
    public static bool Case0237() { var x = 237; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0238() { var x = 238; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 238 && q.Peek() == 239; }
    public static bool Case0239() { var x = 239; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0240() { var x = 240; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 240, 241 }); }
    public static bool Case0241() { var x = 241; return $"item-{x}".ToUpperInvariant() == "ITEM-241"; }
    public static bool Case0242() { var x = 242; return $"a,b,{x}".Split(',')[2] == "242"; }
    public static bool Case0243() { var x = 243; return new StringBuilder().Append("x=").Append(x).ToString() == "x=243"; }
    public static bool Case0244() { var x = 244; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0245() { var x = 245; var v = new List<int> { x, 2, 3 }; return v.Sum() == 250; }
    public static bool Case0246() { var x = 246; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 246; }
    public static bool Case0247() { var x = 247; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0248() { var x = 248; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 248 && q.Peek() == 249; }
    public static bool Case0249() { var x = 249; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0250() { var x = 250; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 250, 251 }); }
    public static bool Case0251() { var x = 251; return $"item-{x}".ToUpperInvariant() == "ITEM-251"; }
    public static bool Case0252() { var x = 252; return $"a,b,{x}".Split(',')[2] == "252"; }
    public static bool Case0253() { var x = 253; return new StringBuilder().Append("x=").Append(x).ToString() == "x=253"; }
    public static bool Case0254() { var x = 254; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0255() { var x = 255; var v = new List<int> { x, 2, 3 }; return v.Sum() == 260; }
    public static bool Case0256() { var x = 256; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 256; }
    public static bool Case0257() { var x = 257; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0258() { var x = 258; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 258 && q.Peek() == 259; }
    public static bool Case0259() { var x = 259; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0260() { var x = 260; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 260, 261 }); }
    public static bool Case0261() { var x = 261; return $"item-{x}".ToUpperInvariant() == "ITEM-261"; }
    public static bool Case0262() { var x = 262; return $"a,b,{x}".Split(',')[2] == "262"; }
    public static bool Case0263() { var x = 263; return new StringBuilder().Append("x=").Append(x).ToString() == "x=263"; }
    public static bool Case0264() { var x = 264; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0265() { var x = 265; var v = new List<int> { x, 2, 3 }; return v.Sum() == 270; }
    public static bool Case0266() { var x = 266; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 266; }
    public static bool Case0267() { var x = 267; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0268() { var x = 268; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 268 && q.Peek() == 269; }
    public static bool Case0269() { var x = 269; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0270() { var x = 270; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 270, 271 }); }
    public static bool Case0271() { var x = 271; return $"item-{x}".ToUpperInvariant() == "ITEM-271"; }
    public static bool Case0272() { var x = 272; return $"a,b,{x}".Split(',')[2] == "272"; }
    public static bool Case0273() { var x = 273; return new StringBuilder().Append("x=").Append(x).ToString() == "x=273"; }
    public static bool Case0274() { var x = 274; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0275() { var x = 275; var v = new List<int> { x, 2, 3 }; return v.Sum() == 280; }
    public static bool Case0276() { var x = 276; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 276; }
    public static bool Case0277() { var x = 277; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0278() { var x = 278; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 278 && q.Peek() == 279; }
    public static bool Case0279() { var x = 279; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0280() { var x = 280; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 280, 281 }); }
    public static bool Case0281() { var x = 281; return $"item-{x}".ToUpperInvariant() == "ITEM-281"; }
    public static bool Case0282() { var x = 282; return $"a,b,{x}".Split(',')[2] == "282"; }
    public static bool Case0283() { var x = 283; return new StringBuilder().Append("x=").Append(x).ToString() == "x=283"; }
    public static bool Case0284() { var x = 284; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0285() { var x = 285; var v = new List<int> { x, 2, 3 }; return v.Sum() == 290; }
    public static bool Case0286() { var x = 286; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 286; }
    public static bool Case0287() { var x = 287; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0288() { var x = 288; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 288 && q.Peek() == 289; }
    public static bool Case0289() { var x = 289; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0290() { var x = 290; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 290, 291 }); }
    public static bool Case0291() { var x = 291; return $"item-{x}".ToUpperInvariant() == "ITEM-291"; }
    public static bool Case0292() { var x = 292; return $"a,b,{x}".Split(',')[2] == "292"; }
    public static bool Case0293() { var x = 293; return new StringBuilder().Append("x=").Append(x).ToString() == "x=293"; }
    public static bool Case0294() { var x = 294; return Regex.Replace($"id{x}", "\\d+", "#") == "id#"; }
    public static bool Case0295() { var x = 295; var v = new List<int> { x, 2, 3 }; return v.Sum() == 300; }
    public static bool Case0296() { var x = 296; var d = new Dictionary<string, int> { ["v"] = x }; return d.TryGetValue("v", out var v) && v == 296; }
    public static bool Case0297() { var x = 297; return new HashSet<int> { x, x, x + 1 }.Count == 2; }
    public static bool Case0298() { var x = 298; var q = new Queue<int>(); q.Enqueue(x); q.Enqueue(x + 1); return q.Dequeue() == 298 && q.Peek() == 299; }
    public static bool Case0299() { var x = 299; return Enumerable.Range(1, 5).Select(v => v + x).Where(v => (v & 1) == 0).Count() == 3; }
    public static bool Case0300() { var x = 300; var json = JsonSerializer.Serialize(new[] { x, x + 1 }); return JsonSerializer.Deserialize<int[]>(json)!.SequenceEqual(new[] { 300, 301 }); }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1600, BogusStates = 4, OpaqueDepth = 3,
    DecryptHashChance = 0.55)]
internal static class IoStreamCases {
    public static bool Case0301() { using var s = new MemoryStream(); s.Write([1, 2, 3, 4]); return s.Length == 4 && s.ToArray()[2] == 3; }
    public static bool Case0302() { using var s = new MemoryStream(); using (var w = new BinaryWriter(s, Encoding.UTF8, true)) { w.Write(302); w.Write("io302"); } s.Position = 0; using var r = new BinaryReader(s); return r.ReadInt32() == 302 && r.ReadString() == "io302"; }
    public static bool Case0303() { using var s = new MemoryStream(); using (var w = new StreamWriter(s, Encoding.UTF8, 1024, true)) w.Write("héllo-303"); s.Position = 0; using var r = new StreamReader(s, Encoding.UTF8); return r.ReadToEnd() == "héllo-303"; }
    public static bool Case0304() { using var r = new StringReader("a\nb\n304"); return r.ReadLine() == "a" && r.ReadLine() == "b" && r.ReadLine() == "304"; }
    public static bool Case0305() { using var w = new StringWriter(); w.Write("value="); w.Write(305); return w.ToString() == "value=305"; }
    public static bool Case0306() { var data = Encoding.UTF8.GetBytes("gzip-306-λ"); using var p = new MemoryStream(); using (var z = new GZipStream(p, CompressionLevel.SmallestSize, true)) z.Write(data); p.Position = 0; using var u = new GZipStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.ToArray().SequenceEqual(data); }
    public static bool Case0307() { var data = Encoding.UTF8.GetBytes("deflate-307-狐"); using var p = new MemoryStream(); using (var z = new DeflateStream(p, CompressionLevel.Fastest, true)) z.Write(data); p.Position = 0; using var u = new DeflateStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.ToArray().SequenceEqual(data); }
    public static bool Case0308() { var p = Path.GetTempFileName(); try { File.WriteAllText(p, "file-308-λ"); return File.ReadAllText(p) == "file-308-λ"; } finally { File.Delete(p); } }
    public static bool Case0309() { var p = Path.GetTempFileName(); try { File.WriteAllBytes(p, [3, 0, 9]); return File.ReadAllBytes(p).SequenceEqual(new byte[] { 3, 0, 9 }); } finally { File.Delete(p); } }
    public static bool Case0310() { var p = Path.Combine(Path.GetTempPath(), "sh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); try { File.WriteAllText(Path.Combine(p, "310.txt"), "x"); return Directory.EnumerateFiles(p, "*.txt").Count() == 1; } finally { Directory.Delete(p, true); } }
    public static bool Case0311() { using var s = new MemoryStream(); s.Write([1, 1, 3, 1]); return s.Length == 4 && s.ToArray().Sum(x => x) == 6; }
    public static bool Case0312() { using var s = new MemoryStream(); using (var w = new BinaryWriter(s, Encoding.UTF8, true)) { w.Write(312); w.Write("io312"); } s.Position = 0; using var r = new BinaryReader(s); return r.ReadInt32() == 312 && r.ReadString() == "io312"; }
    public static bool Case0313() { using var s = new MemoryStream(); using (var w = new StreamWriter(s, Encoding.UTF8, 1024, true)) w.Write("héllo-313"); s.Position = 0; using var r = new StreamReader(s, Encoding.UTF8); return r.ReadToEnd() == "héllo-313"; }
    public static bool Case0314() { using var r = new StringReader("x\ny\n314"); return r.ReadLine() == "x" && r.ReadLine() == "y" && r.ReadLine() == "314"; }
    public static bool Case0315() { using var w = new StringWriter(); w.Write("value="); w.Write(315); return w.ToString() == "value=315"; }
    public static bool Case0316() { var data = Encoding.UTF8.GetBytes("gzip-316-λ"); using var p = new MemoryStream(); using (var z = new GZipStream(p, CompressionLevel.SmallestSize, true)) z.Write(data); p.Position = 0; using var u = new GZipStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.ToArray().SequenceEqual(data); }
    public static bool Case0317() { var data = Encoding.UTF8.GetBytes("deflate-317-狐"); using var p = new MemoryStream(); using (var z = new DeflateStream(p, CompressionLevel.Fastest, true)) z.Write(data); p.Position = 0; using var u = new DeflateStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.ToArray().SequenceEqual(data); }
    public static bool Case0318() { var p = Path.GetTempFileName(); try { File.WriteAllText(p, "file-318-λ"); return File.ReadAllText(p) == "file-318-λ"; } finally { File.Delete(p); } }
    public static bool Case0319() { var p = Path.GetTempFileName(); try { File.WriteAllBytes(p, [3, 1, 9]); return File.ReadAllBytes(p).SequenceEqual(new byte[] { 3, 1, 9 }); } finally { File.Delete(p); } }
    public static bool Case0320() { var p = Path.Combine(Path.GetTempPath(), "sh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); try { File.WriteAllText(Path.Combine(p, "320.txt"), "x"); File.WriteAllText(Path.Combine(p, "ignore.bin"), "y"); return Directory.EnumerateFiles(p, "*.txt").Single().EndsWith("320.txt"); } finally { Directory.Delete(p, true); } }
    public static bool Case0321() { using var s = new MemoryStream(); s.Write([2, 1, 3, 2]); s.Position = 1; return s.ReadByte() == 1 && s.Position == 2; }
    public static bool Case0322() { using var s = new MemoryStream(); using (var w = new BinaryWriter(s, Encoding.UTF8, true)) { w.Write(322L); w.Write(true); } s.Position = 0; using var r = new BinaryReader(s); return r.ReadInt64() == 322 && r.ReadBoolean(); }
    public static bool Case0323() { using var s = new MemoryStream(); using (var w = new StreamWriter(s, Encoding.Unicode, 1024, true)) w.Write("utf16-323"); s.Position = 0; using var r = new StreamReader(s, Encoding.Unicode); return r.ReadToEnd() == "utf16-323"; }
    public static bool Case0324() { using var r = new StringReader("324\r\nend"); return r.ReadLine() == "324" && r.ReadToEnd() == "end"; }
    public static bool Case0325() { using var w = new StringWriter(CultureInfo.InvariantCulture); w.Write("{0:F2}", 325.5); return w.ToString() == "325.50"; }
    public static bool Case0326() { var data = Enumerable.Repeat((byte)26, 326).ToArray(); using var p = new MemoryStream(); using (var z = new GZipStream(p, CompressionLevel.Optimal, true)) z.Write(data); return p.Length < data.Length; }
    public static bool Case0327() { var data = Enumerable.Repeat((byte)27, 327).ToArray(); using var p = new MemoryStream(); using (var z = new DeflateStream(p, CompressionLevel.Optimal, true)) z.Write(data); return p.Length < data.Length; }
    public static bool Case0328() { var p = Path.GetTempFileName(); try { File.AppendAllText(p, "328"); File.AppendAllText(p, "-ok"); return File.ReadAllText(p) == "328-ok"; } finally { File.Delete(p); } }
    public static bool Case0329() { var p = Path.GetTempFileName(); try { using (var s = File.OpenWrite(p)) { s.WriteByte(3); s.WriteByte(29); } return File.ReadAllBytes(p).SequenceEqual(new byte[] { 3, 29 }); } finally { File.Delete(p); } }
    public static bool Case0330() { var p = Path.Combine(Path.GetTempPath(), "sh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); try { Directory.CreateDirectory(Path.Combine(p, "child")); return Directory.GetDirectories(p).Length == 1; } finally { Directory.Delete(p, true); } }
    public static bool Case0331() { using var s = new MemoryStream([3, 3, 1]); var b = new byte[2]; return s.Read(b, 0, 2) == 2 && b.SequenceEqual(new byte[] { 3, 3 }); }
    public static bool Case0332() { using var s = new MemoryStream(); using (var w = new BinaryWriter(s, Encoding.UTF8, true)) { w.Write(33.2); w.Write((short)332); } s.Position = 0; using var r = new BinaryReader(s); return Math.Abs(r.ReadDouble() - 33.2) < 1e-9 && r.ReadInt16() == 332; }
    public static bool Case0333() { using var s = new MemoryStream(); using (var w = new StreamWriter(s, new UTF8Encoding(false), 1024, true)) w.WriteLine("333"); return Encoding.UTF8.GetString(s.ToArray()) == "333" + Environment.NewLine; }
    public static bool Case0334() { using var r = new StringReader("abcdef334"); var b = new char[6]; return r.Read(b, 0, b.Length) == 6 && new string(b) == "abcdef"; }
    public static bool Case0335() { using var w = new StringWriter(); w.WriteLine("335"); return w.ToString().TrimEnd() == "335"; }
    public static bool Case0336() { var data = Encoding.UTF8.GetBytes(new string('a', 336)); using var p = new MemoryStream(); using (var z = new GZipStream(p, CompressionLevel.Optimal, true)) z.Write(data); p.Position = 0; using var u = new GZipStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.Length == 336; }
    public static bool Case0337() { var data = Encoding.UTF8.GetBytes(new string('b', 337)); using var p = new MemoryStream(); using (var z = new DeflateStream(p, CompressionLevel.Optimal, true)) z.Write(data); p.Position = 0; using var u = new DeflateStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return o.Length == 337; }
    public static bool Case0338() { var p = Path.GetTempFileName(); try { File.WriteAllLines(p, ["a", "b", "338"]); return File.ReadLines(p).Last() == "338"; } finally { File.Delete(p); } }
    public static bool Case0339() { var p = Path.GetTempFileName(); try { File.WriteAllBytes(p, Enumerable.Range(0, 40).Select(x => (byte)x).ToArray()); using var s = File.OpenRead(p); s.Seek(33, SeekOrigin.Begin); return s.ReadByte() == 33; } finally { File.Delete(p); } }
    public static bool Case0340() { var p = Path.Combine(Path.GetTempPath(), "sh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); try { var f = Path.Combine(p, "340.dat"); File.WriteAllText(f, "x"); File.Move(f, Path.Combine(p, "moved.dat")); return File.Exists(Path.Combine(p, "moved.dat")); } finally { Directory.Delete(p, true); } }
    public static bool Case0341() { using var s = new MemoryStream([4, 1, 0]); s.SetLength(2); return s.ToArray().SequenceEqual(new byte[] { 4, 1 }); }
    public static bool Case0342() { using var s = new MemoryStream(); using (var w = new BinaryWriter(s, Encoding.UTF8, true)) { w.Write(342u); w.Write((byte)42); } s.Position = 0; using var r = new BinaryReader(s); return r.ReadUInt32() == 342 && r.ReadByte() == 42; }
    public static bool Case0343() { using var s = new MemoryStream(); using (var w = new StreamWriter(s, Encoding.UTF8, 1024, true)) { w.Write("34"); w.Flush(); w.Write("3"); } s.Position = 0; using var r = new StreamReader(s); return r.ReadToEnd() == "343"; }
    public static bool Case0344() { using var r = new StringReader("344"); return r.Peek() == '3' && r.Read() == '3' && r.ReadToEnd() == "44"; }
    public static bool Case0345() { using var w = new StringWriter(); w.Write(new char[] { '3', '4', '5' }); return w.GetStringBuilder().Length == 3; }
    public static bool Case0346() { var data = BitConverter.GetBytes(346); using var p = new MemoryStream(); using (var z = new GZipStream(p, CompressionLevel.Fastest, true)) z.Write(data); p.Position = 0; using var u = new GZipStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return BitConverter.ToInt32(o.ToArray()) == 346; }
    public static bool Case0347() { var data = BitConverter.GetBytes(347L); using var p = new MemoryStream(); using (var z = new DeflateStream(p, CompressionLevel.Fastest, true)) z.Write(data); p.Position = 0; using var u = new DeflateStream(p, CompressionMode.Decompress); using var o = new MemoryStream(); u.CopyTo(o); return BitConverter.ToInt64(o.ToArray()) == 347; }
    public static bool Case0348() { var p = Path.GetTempFileName(); try { File.WriteAllText(p, "348"); var info = new FileInfo(p); return info.Exists && info.Length == 3; } finally { File.Delete(p); } }
    public static bool Case0349() { var p = Path.GetTempFileName(); try { using var s = new FileStream(p, FileMode.Create, FileAccess.ReadWrite, FileShare.None); s.Write([3, 4, 9]); s.Position = 2; return s.ReadByte() == 9; } finally { File.Delete(p); } }
    public static bool Case0350() { var p = Path.Combine(Path.GetTempPath(), "sh-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); try { var child = Directory.CreateDirectory(Path.Combine(p, "350")); return child.Parent!.FullName == new DirectoryInfo(p).FullName; } finally { Directory.Delete(p, true); } }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 4, OpaqueDepth = 3,
    DecryptHashChance = 0.5)]
internal static class NetworkCases {
    public static bool Case0351() { var u = new Uri("https://example.test:8443/a/b?q=351#x"); return u.Scheme == "https" && u.Port == 8443 && u.AbsolutePath == "/a/b"; }
    public static bool Case0352() { var b = new UriBuilder("http", "localhost", 8352, "api/items"); return b.Uri.AbsoluteUri == "http://localhost:8352/api/items"; }
    public static bool Case0353() { var u = new Uri("https://host.test/?left=3&right=53"); return u.Query.Contains("right=53", StringComparison.Ordinal); }
    public static bool Case0354() { var text = Uri.EscapeDataString("λ 354/狐"); return Uri.UnescapeDataString(text) == "λ 354/狐"; }
    public static bool Case0355() { var u = new Uri("items/355", UriKind.Relative); return !u.IsAbsoluteUri && u.OriginalString.EndsWith("355", StringComparison.Ordinal); }
    public static bool Case0356() { var ip = IPAddress.Parse("127.0.0.56"); return ip.GetAddressBytes().SequenceEqual(new byte[] { 127, 0, 0, 56 }); }
    public static bool Case0357() { var ip = IPAddress.Parse("::1"); return IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case0358() { var ip = IPAddress.Parse("10.0.0.58").MapToIPv6().MapToIPv4(); return ip.Equals(IPAddress.Parse("10.0.0.58")); }
    public static bool Case0359() { var ep = new IPEndPoint(IPAddress.Loopback, 8359); return ep.Port == 8359 && IPAddress.IsLoopback(ep.Address); }
    public static bool Case0360() { var n = 360; return IPAddress.NetworkToHostOrder(IPAddress.HostToNetworkOrder(n)) == n; }
    public static bool Case0361() { var u = new Uri("ftp://user@example.test/data/361"); return u.Scheme == "ftp" && u.UserInfo == "user" && u.Segments[^1] == "361"; }
    public static bool Case0362() { var b = new UriBuilder("https://example.test") { Port = 8362, Query = "v=362" }; return b.Uri.Port == 8362 && b.Uri.Query == "?v=362"; }
    public static bool Case0363() { var u = new Uri("https://host.test/a%20b/363"); return u.LocalPath.Contains("a b", StringComparison.Ordinal); }
    public static bool Case0364() { var text = Uri.HexEscape((char)0x64); return text == "%64" && Uri.HexUnescape(text, ref UnsafeIndex.Value) == 'd'; }
    public static bool Case0365() { return Uri.TryCreate("https://localhost:8365/path", UriKind.Absolute, out var u) && u.Port == 8365; }
    public static bool Case0366() { var ip = new IPAddress(new byte[] { 192, 168, 1, 66 }); return ip.ToString() == "192.168.1.66"; }
    public static bool Case0367() { return IPAddress.TryParse("2001:db8::367", out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case0368() { var bytes = IPAddress.Parse("172.16.0.68").GetAddressBytes(); Array.Reverse(bytes); Array.Reverse(bytes); return new IPAddress(bytes).ToString() == "172.16.0.68"; }
    public static bool Case0369() { EndPoint ep = new IPEndPoint(IPAddress.Any, 8369); return ep.AddressFamily == AddressFamily.InterNetwork; }
    public static bool Case0370() { var n = 370L << 32; return IPAddress.NetworkToHostOrder(IPAddress.HostToNetworkOrder(n)) == n; }
    public static bool Case0371() { var baseUri = new Uri("https://example.test/root/"); return new Uri(baseUri, "child/371").AbsolutePath == "/root/child/371"; }
    public static bool Case0372() { var u = new Uri("https://EXAMPLE.test:443/372"); return u.DnsSafeHost == "example.test" && u.IsDefaultPort; }
    public static bool Case0373() { var u = new Uri("mailto:user373@example.test"); return u.Scheme == "mailto" && u.OriginalString.Contains("373", StringComparison.Ordinal); }
    public static bool Case0374() { var value = "a+b c/374"; return Uri.UnescapeDataString(Uri.EscapeDataString(value)) == value; }
    public static bool Case0375() { var u = new Uri("file:///C:/temp/375.txt"); return u.IsFile && u.AbsolutePath.EndsWith("375.txt", StringComparison.Ordinal); }
    public static bool Case0376() { var ip = IPAddress.Parse("8.8.4.76"); return ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip); }
    public static bool Case0377() { var ip = IPAddress.IPv6Loopback; return ip.ScopeId == 0 && ip.GetAddressBytes().Length == 16; }
    public static bool Case0378() { var ip = IPAddress.Parse("::ffff:192.0.2.78"); return ip.IsIPv4MappedToIPv6 && ip.MapToIPv4().ToString() == "192.0.2.78"; }
    public static bool Case0379() { var ep = IPEndPoint.Parse("127.0.0.1:8379"); return ep.Port == 8379 && IPAddress.IsLoopback(ep.Address); }
    public static bool Case0380() { short n = 380; return IPAddress.NetworkToHostOrder(IPAddress.HostToNetworkOrder(n)) == n; }
    public static bool Case0381() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); c.GetStream().WriteByte(81); return s.GetStream().ReadByte() == 81; } finally { l.Stop(); } }
    public static bool Case0382() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); c.GetStream().Write([3, 8, 2]); var b = new byte[3]; return s.GetStream().Read(b) == 3 && b.SequenceEqual(new byte[] { 3, 8, 2 }); } finally { l.Stop(); } }
    public static bool Case0383() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.NoDelay = true; c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); return c.Connected && s.Connected && c.NoDelay; } finally { l.Stop(); } }
    public static bool Case0384() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(AddressFamily.InterNetwork); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); s.GetStream().WriteByte(84); return c.GetStream().ReadByte() == 84; } finally { l.Stop(); } }
    public static bool Case0385() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); var data = Encoding.UTF8.GetBytes("tcp-385"); c.GetStream().Write(data); var b = new byte[data.Length]; s.GetStream().ReadExactly(b); return Encoding.UTF8.GetString(b) == "tcp-385"; } finally { l.Stop(); } }
    public static bool Case0386() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); return ((IPEndPoint)c.Client.RemoteEndPoint!).Port == ((IPEndPoint)l.LocalEndpoint).Port; } finally { l.Stop(); } }
    public static bool Case0387() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.ReceiveTimeout = 1000; c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); s.GetStream().WriteByte(87); return c.GetStream().ReadByte() == 87; } finally { l.Stop(); } }
    public static bool Case0388() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); c.GetStream().WriteByte(88); return s.Available == 1 && s.GetStream().ReadByte() == 88; } finally { l.Stop(); } }
    public static bool Case0389() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); c.GetStream().Write(BitConverter.GetBytes(389)); var b = new byte[4]; s.GetStream().ReadExactly(b); return BitConverter.ToInt32(b) == 389; } finally { l.Stop(); } }
    public static bool Case0390() { var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); try { using var c = new TcpClient(); c.Connect((IPEndPoint)l.LocalEndpoint); using var s = l.AcceptTcpClient(); c.GetStream().WriteByte(90); c.Client.Shutdown(SocketShutdown.Send); return s.GetStream().ReadByte() == 90 && s.GetStream().ReadByte() == -1; } finally { l.Stop(); } }
    public static bool Case0391() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([91], 1, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return r.Receive(ref remote).Single() == 91; }
    public static bool Case0392() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([3, 9, 2], 3, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return r.Receive(ref remote).SequenceEqual(new byte[] { 3, 9, 2 }); }
    public static bool Case0393() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; var data = Encoding.UTF8.GetBytes("udp-393"); s.Send(data, data.Length, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return Encoding.UTF8.GetString(r.Receive(ref remote)) == "udp-393"; }
    public static bool Case0394() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); r.Client.ReceiveTimeout = 1000; using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([94], 1, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return r.Receive(ref remote)[0] == 94 && IPAddress.IsLoopback(remote.Address); }
    public static bool Case0395() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Connect(ep); return s.Send([95], 1) == 1 && r.Available == 1; }
    public static bool Case0396() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send(BitConverter.GetBytes(396), 4, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return BitConverter.ToInt32(r.Receive(ref remote)) == 396; }
    public static bool Case0397() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(AddressFamily.InterNetwork); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([97], 1, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return r.Receive(ref remote)[0] == 97; }
    public static bool Case0398() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; var sent = s.Send([3, 9, 8, 0], 4, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return sent == 4 && r.Receive(ref remote).Length == 4; }
    public static bool Case0399() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([99], 1, ep); var remote = new IPEndPoint(IPAddress.Any, 0); r.Receive(ref remote); return remote.Port == ((IPEndPoint)s.Client.LocalEndPoint!).Port; }
    public static bool Case0400() { using var r = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)); using var s = new UdpClient(); var ep = (IPEndPoint)r.Client.LocalEndPoint!; s.Send([4, 0, 0], 3, ep); var remote = new IPEndPoint(IPAddress.Any, 0); return r.Receive(ref remote).Sum(x => x) == 4; }

    private static class UnsafeIndex {
        public static int Value;
    }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1400, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class ConcurrencyCases {
    public static bool Case0401() { return Task.FromResult(401).GetAwaiter().GetResult() == 401; }
    public static bool Case0402() { return Task.Run(() => 402 * 2).GetAwaiter().GetResult() == 804; }
    public static bool Case0403() { var tasks = new[] { Task.FromResult(400), Task.FromResult(3) }; return Task.WhenAll(tasks).GetAwaiter().GetResult().Sum() == 403; }
    public static bool Case0404() { var total = 0; Parallel.For(0, 4, i => Interlocked.Add(ref total, i)); return total == 6; }
    public static bool Case0405() { var value = 0; var t = new Thread(() => value = 405); t.Start(); t.Join(); return value == 405; }
    public static bool Case0406() { using var gate = new SemaphoreSlim(1, 1); gate.Wait(); try { return gate.CurrentCount == 0; } finally { gate.Release(); } }
    public static bool Case0407() { using var source = new CancellationTokenSource(); source.Cancel(); return source.IsCancellationRequested && source.Token.IsCancellationRequested; }
    public static bool Case0408() { var value = new AsyncLocal<int> { Value = 408 }; return Task.Run(() => value.Value).GetAwaiter().GetResult() == 408; }
    public static bool Case0409() { using var value = new ThreadLocal<int>(() => 409); return value.Value == 409 && value.IsValueCreated; }
    public static bool Case0410() { var values = new ConcurrentDictionary<int, int>(); values[410] = 41; return values.AddOrUpdate(410, 0, (_, old) => old + 369) == 410; }
    public static bool Case0411() { return Task.FromResult("411").Result == "411"; }
    public static bool Case0412() { return Task.Run(() => "case-" + 412).Result == "case-412"; }
    public static bool Case0413() { var tasks = Enumerable.Range(1, 4).Select(x => Task.FromResult(x)).ToArray(); return Task.WhenAll(tasks).Result.Sum() == 10; }
    public static bool Case0414() { var total = 0; Parallel.ForEach(new[] { 4, 1, 4 }, x => Interlocked.Add(ref total, x)); return total == 9; }
    public static bool Case0415() { var value = ""; var t = new Thread(() => value = "415"); t.Start(); return t.Join(1000) && value == "415"; }
    public static bool Case0416() { using var gate = new SemaphoreSlim(0, 1); ThreadPool.QueueUserWorkItem(_ => gate.Release()); return gate.Wait(1000); }
    public static bool Case0417() { using var source = new CancellationTokenSource(); source.CancelAfter(TimeSpan.FromMilliseconds(1)); return SpinWait.SpinUntil(() => source.IsCancellationRequested, 1000); }
    public static bool Case0418() { var value = new AsyncLocal<string> { Value = "418" }; return Task.Run(() => value.Value + "!").Result == "418!"; }
    public static bool Case0419() { using var value = new ThreadLocal<int>(() => Environment.CurrentManagedThreadId); return value.Value == Environment.CurrentManagedThreadId; }
    public static bool Case0420() { var q = new ConcurrentQueue<int>(); q.Enqueue(420); return q.TryDequeue(out var x) && x == 420 && q.IsEmpty; }
    public static bool Case0421() { var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously); completion.SetResult(421); return completion.Task.Result == 421; }
    public static bool Case0422() { var task = Task.Run(() => 422); task.Wait(); return task.IsCompletedSuccessfully && task.Result == 422; }
    public static bool Case0423() { var tasks = new[] { Task.Delay(1), Task.CompletedTask }; Task.WhenAll(tasks).Wait(); return tasks.All(x => x.IsCompletedSuccessfully); }
    public static bool Case0424() { var values = new int[8]; Parallel.For(0, values.Length, i => values[i] = i * i); return values.Sum() == 140; }
    public static bool Case0425() { var value = 0; var t = new Thread(x => value = (int)x!) { IsBackground = true }; t.Start(425); t.Join(); return value == 425; }
    public static bool Case0426() { using var gate = new SemaphoreSlim(2, 2); gate.Wait(); gate.Wait(); var zero = gate.CurrentCount == 0; gate.Release(2); return zero && gate.CurrentCount == 2; }
    public static bool Case0427() { using var source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken(true)); return source.Token.IsCancellationRequested; }
    public static bool Case0428() { var value = new AsyncLocal<int>(); value.Value = 428; var before = value.Value; value.Value++; return before == 428 && value.Value == 429; }
    public static bool Case0429() { using var value = new ThreadLocal<List<int>>(() => []); value.Value!.Add(429); return value.Value.Single() == 429; }
    public static bool Case0430() { var bag = new ConcurrentBag<int>(); bag.Add(430); bag.Add(43); return bag.Count == 2 && bag.Contains(430); }
    public static bool Case0431() { var source = new TaskCompletionSource<string>(); return source.TrySetResult("431") && source.Task.Result == "431"; }
    public static bool Case0432() { return Task.Run(() => Enumerable.Range(1, 432 % 10).Sum()).Result == 3; }
    public static bool Case0433() { var first = Task.FromResult(433); var second = Task.FromResult(1); return Task.WhenAny(first, second).Result.IsCompletedSuccessfully; }
    public static bool Case0434() { var max = 0; Parallel.For(1, 5, i => InterlockedExtensions.Max(ref max, i)); return max == 4; }
    public static bool Case0435() { using var ready = new ManualResetEventSlim(); var t = new Thread(() => ready.Set()); t.Start(); var signaled = ready.Wait(1000); t.Join(); return signaled; }
    public static bool Case0436() { using var gate = new SemaphoreSlim(1, 1); return gate.Wait(0) && gate.Release() == 0; }
    public static bool Case0437() { var token = new CancellationToken(true); try { token.ThrowIfCancellationRequested(); return false; } catch (OperationCanceledException) { return true; } }
    public static bool Case0438() { var value = new AsyncLocal<int> { Value = 438 }; ExecutionContext.SuppressFlow(); try { return Task.Run(() => value.Value).Result == 0; } finally { ExecutionContext.RestoreFlow(); } }
    public static bool Case0439() { using var value = new ThreadLocal<int>(() => 439, true); _ = value.Value; return value.Values.Single() == 439; }
    public static bool Case0440() { var stack = new ConcurrentStack<int>(); stack.Push(440); stack.Push(44); return stack.TryPop(out var x) && x == 44 && stack.Count == 1; }
    public static bool Case0441() { return Task.FromException<int>(new InvalidOperationException("441")).IsFaulted; }
    public static bool Case0442() { return Task.Run(() => 442).ContinueWith(task => task.Result + 1).Result == 443; }
    public static bool Case0443() { var tasks = new[] { Task.FromResult("4"), Task.FromResult("43") }; return string.Concat(Task.WhenAll(tasks).Result) == "443"; }
    public static bool Case0444() { var total = 0; Parallel.Invoke(() => Interlocked.Add(ref total, 400), () => Interlocked.Add(ref total, 44)); return total == 444; }
    public static bool Case0445() { using var ready = new CountdownEvent(2); var a = new Thread(() => ready.Signal()); var b = new Thread(() => ready.Signal()); a.Start(); b.Start(); var ok = ready.Wait(1000); a.Join(); b.Join(); return ok && ready.CurrentCount == 0; }
    public static bool Case0446() { using var gate = new SemaphoreSlim(0, 1); var task = gate.WaitAsync(); gate.Release(); return task.Wait(1000) && task.IsCompletedSuccessfully; }
    public static bool Case0447() { using var source = new CancellationTokenSource(); var called = 0; using var registration = source.Token.Register(() => called++); source.Cancel(); return called == 1; }
    public static bool Case0448() { var value = new AsyncLocal<int> { Value = 448 }; var copy = ExecutionContext.Capture(); var seen = 0; ExecutionContext.Run(copy!, _ => seen = value.Value, null); return seen == 448; }
    public static bool Case0449() { using var value = new ThreadLocal<int>(); value.Value = 449; return value.Value == 449; }
    public static bool Case0450() { var dictionary = new ConcurrentDictionary<string, int>(); return dictionary.TryAdd("450", 450) && dictionary.TryRemove("450", out var value) && value == 450; }

    private static class InterlockedExtensions {
        public static void Max(ref int location, int value) {
            while (true) {
                var current = Volatile.Read(ref location);
                if (current >= value || Interlocked.CompareExchange(ref location, value, current) == current)
                    return;
            }
        }
    }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1400, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class CryptographyEncodingCases {
    public static bool Case0451() { return SHA256.HashData("451"u8).Length == 32; }
    public static bool Case0452() { return Convert.ToHexString(SHA256.HashData("abc"u8)) == "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"; }
    public static bool Case0453() { return SHA1.HashData("453"u8).Length == 20; }
    public static bool Case0454() { return MD5.HashData("454"u8).Length == 16; }
    public static bool Case0455() { return SHA384.HashData("455"u8).Length == 48; }
    public static bool Case0456() { return SHA512.HashData("456"u8).Length == 64; }
    public static bool Case0457() { using var hmac = new HMACSHA256("key-457"u8.ToArray()); return hmac.ComputeHash("payload"u8.ToArray()).SequenceEqual(hmac.ComputeHash("payload"u8.ToArray())); }
    public static bool Case0458() { using var first = new HMACSHA256("key-a"u8.ToArray()); using var second = new HMACSHA256("key-b"u8.ToArray()); return !first.ComputeHash("458"u8.ToArray()).SequenceEqual(second.ComputeHash("458"u8.ToArray())); }
    public static bool Case0459() { var left = SHA256.HashData("459"u8); var right = SHA256.HashData("459"u8); return CryptographicOperations.FixedTimeEquals(left, right); }
    public static bool Case0460() { return !CryptographicOperations.FixedTimeEquals(SHA256.HashData("460-a"u8), SHA256.HashData("460-b"u8)); }
    public static bool Case0461() { var first = new Random(461); var second = new Random(461); return Enumerable.Range(0, 8).All(_ => first.Next() == second.Next()); }
    public static bool Case0462() { var random = new Random(462); for (var i = 0; i < 50; i++) { var value = random.Next(-10, 11); if (value < -10 || value > 10) return false; } return true; }
    public static bool Case0463() { var bytes = new byte[64]; new Random(463).NextBytes(bytes); return bytes.Any(value => value != 0) && bytes.Distinct().Count() > 8; }
    public static bool Case0464() { var bytes = Encoding.UTF8.GetBytes("case-464-🦊"); return Convert.FromBase64String(Convert.ToBase64String(bytes)).SequenceEqual(bytes); }
    public static bool Case0465() { var bytes = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray(); return Convert.FromHexString(Convert.ToHexString(bytes)).SequenceEqual(bytes); }
    public static bool Case0466() { return Convert.ToHexStringLower(new byte[] { 0x0a, 0xbc, 0xff }) == "0abcff"; }
    public static bool Case0467() { const string text = "héllo-467-世界"; return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text)) == text; }
    public static bool Case0468() { const string text = "unicode-468-𝄞"; return Encoding.Unicode.GetString(Encoding.Unicode.GetBytes(text)) == text; }
    public static bool Case0469() { return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes("ASCII-469")) == "ASCII-469"; }
    public static bool Case0470() { const string text = "utf32-470-λ"; return Encoding.UTF32.GetString(Encoding.UTF32.GetBytes(text)) == text; }
    public static bool Case0471() { using var aes = Aes.Create(); aes.Key = SHA256.HashData("key-471"u8); aes.GenerateIV(); using var encryptor = aes.CreateEncryptor(); var cipher = encryptor.TransformFinalBlock("payload-471"u8.ToArray(), 0, 11); using var decryptor = aes.CreateDecryptor(); return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(cipher, 0, cipher.Length)) == "payload-471"; }
    public static bool Case0472() { using var aes = Aes.Create(); aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.PKCS7; aes.Key = SHA256.HashData("key-472"u8); var plain = "ecb-472"u8.ToArray(); var cipher = aes.EncryptEcb(plain, PaddingMode.PKCS7); return aes.DecryptEcb(cipher, PaddingMode.PKCS7).SequenceEqual(plain); }
    public static bool Case0473() { using var aes = Aes.Create(); aes.Key = SHA256.HashData("key-473"u8); var iv = new byte[16]; iv[^1] = 73; var plain = "cbc-473-content"u8.ToArray(); var cipher = aes.EncryptCbc(plain, iv, PaddingMode.PKCS7); return aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7).SequenceEqual(plain); }
    public static bool Case0474() { using var aes = Aes.Create(); aes.KeySize = 256; aes.GenerateKey(); return aes.Key.Length == 32 && aes.LegalKeySizes.Any(size => size.MinSize == 128 && size.MaxSize >= 256); }
    public static bool Case0475() { var bytes = RandomNumberGenerator.GetBytes(75); return bytes.Length == 75 && bytes.Any(value => value != 0); }
    public static bool Case0476() { for (var i = 0; i < 32; i++) { var value = RandomNumberGenerator.GetInt32(476, 576); if (value < 476 || value >= 576) return false; } return true; }
    public static bool Case0477() { return HashAlgorithmName.SHA256.Name == "SHA256" && HashAlgorithmName.SHA512.Name == "SHA512"; }
    public static bool Case0478() { using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); hash.AppendData("47"u8); hash.AppendData("8"u8); return hash.GetHashAndReset().SequenceEqual(SHA256.HashData("478"u8)); }
    public static bool Case0479() { using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, "key"u8); hmac.AppendData("479"u8); using var expected = new HMACSHA256("key"u8.ToArray()); return hmac.GetHashAndReset().SequenceEqual(expected.ComputeHash("479"u8.ToArray())); }
    public static bool Case0480() { using var hmac = new HMACSHA512("key-480"u8.ToArray()); return hmac.ComputeHash("data"u8.ToArray()).Length == 64; }
    public static bool Case0481() { var first = Rfc2898DeriveBytes.Pbkdf2("password", "salt-481"u8.ToArray(), 10, HashAlgorithmName.SHA256, 24); var second = Rfc2898DeriveBytes.Pbkdf2("password", "salt-481"u8.ToArray(), 10, HashAlgorithmName.SHA256, 24); return first.SequenceEqual(second); }
    public static bool Case0482() { var first = Rfc2898DeriveBytes.Pbkdf2("password", "salt-a"u8.ToArray(), 10, HashAlgorithmName.SHA256, 16); var second = Rfc2898DeriveBytes.Pbkdf2("password", "salt-b"u8.ToArray(), 10, HashAlgorithmName.SHA256, 16); return !first.SequenceEqual(second); }
    public static bool Case0483() { var secret = Enumerable.Repeat((byte)0x5a, 48).ToArray(); CryptographicOperations.ZeroMemory(secret); return secret.All(value => value == 0); }
    public static bool Case0484() { var left = new byte[] { 4, 8, 4 }; var right = new byte[] { 4, 8, 4 }; return CryptographicOperations.FixedTimeEquals(left, right); }
    public static bool Case0485() { return Encoding.UTF8.GetString(Convert.FromBase64String("Y2FzZS00\r\nODU=")) == "case-485"; }
    public static bool Case0486() { Span<byte> buffer = stackalloc byte[16]; return Convert.TryFromBase64String("NDg2", buffer, out var written) && written == 3 && buffer[..written].SequenceEqual("486"u8); }
    public static bool Case0487() { Span<char> buffer = stackalloc char[16]; return Convert.TryToBase64Chars("487"u8, buffer, out var written) && new string(buffer[..written]) == "NDg3"; }
    public static bool Case0488() { try { _ = Convert.FromHexString("ABC"); return false; } catch (FormatException) { return true; } }
    public static bool Case0489() { try { _ = Convert.FromBase64String("not-base64"); return false; } catch (FormatException) { return true; } }
    public static bool Case0490() { var id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"); return new Guid(id.ToByteArray()) == id; }
    public static bool Case0491() { const long value = 0x0102030405060708; return BitConverter.ToInt64(BitConverter.GetBytes(value)) == value; }
    public static bool Case0492() { const string text = "é-492-🦊"; return Encoding.UTF8.GetByteCount(text) == Encoding.UTF8.GetBytes(text).Length; }
    public static bool Case0493() { var rune = Rune.GetRuneAt("🦊 fox", 0); return rune.Value == 0x1f98a && rune.Utf16SequenceLength == 2; }
    public static bool Case0494() { var decomposed = "e\u0301"; return decomposed.Normalize(NormalizationForm.FormC) == "é"; }
    public static bool Case0495() { using var stream = new MemoryStream("stream-495"u8.ToArray()); using var hash = SHA256.Create(); return hash.ComputeHash(stream).SequenceEqual(SHA256.HashData("stream-495"u8)); }
    public static bool Case0496() { using var stream = new MemoryStream("stream-496"u8.ToArray()); using var hash = MD5.Create(); return hash.ComputeHash(stream).Length == 16 && stream.Position == stream.Length; }
    public static bool Case0497() { using var stream = new MemoryStream("stream-497"u8.ToArray()); using var hmac = new HMACSHA256("key"u8.ToArray()); return hmac.ComputeHash(stream).SequenceEqual(hmac.ComputeHash("stream-497"u8.ToArray())); }
    public static bool Case0498() { using var hash = SHA256.Create(); var input = "block-498"u8.ToArray(); var output = new byte[input.Length]; _ = hash.TransformBlock(input, 0, input.Length, output, 0); _ = hash.TransformFinalBlock([], 0, 0); return output.SequenceEqual(input) && hash.Hash!.Length == 32; }
    public static bool Case0499() { var key = SHA256.HashData("key-499"u8); var nonce = new byte[12]; var plain = "gcm-499"u8.ToArray(); var cipher = new byte[plain.Length]; var tag = new byte[16]; using var aes = new AesGcm(key, tag.Length); aes.Encrypt(nonce, plain, cipher, tag); var result = new byte[plain.Length]; aes.Decrypt(nonce, cipher, tag, result); return result.SequenceEqual(plain); }
    public static bool Case0500() { if (!ChaCha20Poly1305.IsSupported) return true; var key = SHA256.HashData("key-500"u8); var nonce = new byte[12]; var plain = "chacha-500"u8.ToArray(); var cipher = new byte[plain.Length]; var tag = new byte[16]; using var chacha = new ChaCha20Poly1305(key); chacha.Encrypt(nonce, plain, cipher, tag); var result = new byte[plain.Length]; chacha.Decrypt(nonce, cipher, tag, result); return result.SequenceEqual(plain); }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1200, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class DateTimeNumericCases {
    public static bool Case0501() { return DateTime.IsLeapYear(2024) && !DateTime.IsLeapYear(2100); }
    public static bool Case0502() { var value = DateTime.ParseExact("2026-09-08 22:15", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture); return value.Year == 2026 && value.Minute == 15; }
    public static bool Case0503() { var value = new DateTime(2026, 9, 8, 22, 16, 17, DateTimeKind.Utc); return value.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) == "20260908-221617"; }
    public static bool Case0504() { return new DateTime(2024, 1, 31).AddMonths(1) == new DateTime(2024, 2, 29); }
    public static bool Case0505() { var value = DateOnly.FromDateTime(new DateTime(2026, 5, 5)); return value.AddDays(5).DayNumber - value.DayNumber == 5; }
    public static bool Case0506() { var value = new TimeOnly(23, 59, 30); return value.AddMinutes(2, out var wrappedDays) == new TimeOnly(0, 1, 30) && wrappedDays == 1; }
    public static bool Case0507() { var value = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000); return value.ToUnixTimeSeconds() == 1_700_000_000; }
    public static bool Case0508() { var value = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.FromHours(-3)); return value.UtcDateTime.Hour == 15 && value.Offset == TimeSpan.FromHours(-3); }
    public static bool Case0509() { return TimeSpan.ParseExact("12:34:56", "hh\\:mm\\:ss", CultureInfo.InvariantCulture) == new TimeSpan(12, 34, 56); }
    public static bool Case0510() { var value = TimeSpan.FromMinutes(510) - TimeSpan.FromHours(8); return value == TimeSpan.FromMinutes(30); }
    public static bool Case0511() { var timer = Stopwatch.StartNew(); timer.Stop(); return timer.ElapsedTicks >= 0 && Stopwatch.Frequency > 0; }
    public static bool Case0512() { var calendar = CultureInfo.InvariantCulture.Calendar; return calendar.GetDaysInMonth(2024, 2) == 29 && calendar.GetDayOfYear(new DateTime(2024, 12, 31)) == 366; }
    public static bool Case0513() { return ISOWeek.GetWeekOfYear(new DateTime(2021, 1, 1)) == 53 && ISOWeek.GetYear(new DateTime(2021, 1, 1)) == 2020; }
    public static bool Case0514() { var calendar = new GregorianCalendar(GregorianCalendarTypes.MiddleEastFrench); return calendar.IsLeapDay(2024, 2, 29); }
    public static bool Case0515() { var culture = CultureInfo.GetCultureInfo("fr-FR"); return decimal.Parse("1 234,50", NumberStyles.Number, culture) == 1234.50m; }
    public static bool Case0516() { return 516.25m.ToString("C2", CultureInfo.GetCultureInfo("en-US")) == "$516.25"; }
    public static bool Case0517() { var compare = CultureInfo.GetCultureInfo("en-US").CompareInfo; return compare.Compare("café", "cafe", CompareOptions.IgnoreNonSpace) == 0; }
    public static bool Case0518() { return CultureInfo.GetCultureInfo("en-US").TextInfo.ToTitleCase("case five eighteen") == "Case Five Eighteen"; }
    public static bool Case0519() { var region = new RegionInfo("es-UY"); return region.TwoLetterISORegionName == "UY" && region.ISOCurrencySymbol == "UYU"; }
    public static bool Case0520() { var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone(); format.NumberGroupSeparator = "_"; return 1_234_520.ToString("N0", format) == "1_234_520"; }
    public static bool Case0521() { return decimal.Round(521.555m, 2, MidpointRounding.AwayFromZero) == 521.56m; }
    public static bool Case0522() { return decimal.Truncate(-522.99m) == -522m; }
    public static bool Case0523() { return decimal.Remainder(523m, 10m) == 3m; }
    public static bool Case0524() { return BigInteger.Pow(new BigInteger(2), 20) == 1_048_576; }
    public static bool Case0525() { return BigInteger.Parse("999999999999999999999999", CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) == "999999999999999999999999"; }
    public static bool Case0526() { return BigInteger.GreatestCommonDivisor(new BigInteger(987654), new BigInteger(123456)) == 6; }
    public static bool Case0527() { var value = new Complex(3, 4); return value.Magnitude == 5; }
    public static bool Case0528() { var value = new Complex(2, 3) * new Complex(4, -1); return value.Real == 11 && value.Imaginary == 10; }
    public static bool Case0529() { return Vector2.Dot(new Vector2(5, 2), new Vector2(9, 4)) == 53; }
    public static bool Case0530() { var transformed = Vector2.Transform(new Vector2(3, 4), Matrix3x2.CreateTranslation(5, 6)); return transformed == new Vector2(8, 10); }
    public static bool Case0531() { var value = Quaternion.Normalize(new Quaternion(0, 0, 0, 2)); return value == Quaternion.Identity; }
    public static bool Case0532() { return BitOperations.PopCount(0b101101u) == 4; }
    public static bool Case0533() { return BitOperations.LeadingZeroCount(1u) == 31 && BitOperations.TrailingZeroCount(8u) == 3; }
    public static bool Case0534() { return BitOperations.RotateLeft(0x12345678u, 8) == 0x34567812u; }
    public static bool Case0535() { var bits = decimal.GetBits(535.25m); return new decimal(bits) == 535.25m && bits.Length == 4; }
    public static bool Case0536() { Half value = (Half)536.5f; return (float)value == 536.5f && Half.IsFinite(value); }
    public static bool Case0537() { const float value = -537.25f; return BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value)) == value; }
    public static bool Case0538() { const double value = 538.125; return BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(value)) == value; }
    public static bool Case0539() { Int128 value = Int128.Parse("1701411834604692317316873037158841057", CultureInfo.InvariantCulture); return value / 10 * 10 + value % 10 == value; }
    public static bool Case0540() { UInt128 value = UInt128.Parse("340282366920938463463374607431768211", CultureInfo.InvariantCulture); return value.ToString(CultureInfo.InvariantCulture).EndsWith("211", StringComparison.Ordinal); }
    public static bool Case0541() { return Math.Clamp(541, 0, 500) == 500 && Math.Clamp(-1, 0, 500) == 0; }
    public static bool Case0542() { var quotient = Math.DivRem(542, 17, out var remainder); return quotient == 31 && remainder == 15; }
    public static bool Case0543() { return Math.BigMul(100_000, 100_000) == 10_000_000_000L; }
    public static bool Case0544() { return Math.Log2(1024) == 10 && Math.ILogB(8.0) == 3; }
    public static bool Case0545() { return Math.Abs(Math.Sin(Math.PI / 2) - 1.0) < 1e-12; }
    public static bool Case0546() { return MathF.Sqrt(54.6f * 54.6f) is > 54.59f and < 54.61f; }
    public static bool Case0547() { return Convert.ToDecimal("547.75", CultureInfo.InvariantCulture) == 547.75m; }
    public static bool Case0548() { var utc = DateTime.SpecifyKind(new DateTime(2026, 9, 8), DateTimeKind.Utc); return utc.Kind == DateTimeKind.Utc && utc.ToUniversalTime() == utc; }
    public static bool Case0549() { return TimeZoneInfo.Utc.GetUtcOffset(new DateTime(2026, 9, 8)) == TimeSpan.Zero && TimeZoneInfo.Utc.Id == "UTC"; }
    public static bool Case0550() { var zone = TimeZoneInfo.CreateCustomTimeZone("Case550", TimeSpan.FromHours(-3), "Case550", "Case550"); var utc = new DateTime(2026, 9, 8, 15, 0, 0, DateTimeKind.Utc); return TimeZoneInfo.ConvertTimeFromUtc(utc, zone).Hour == 12; }
}

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1400, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class SerializationReflectionCases {
    public static bool Case0551() { var json = JsonSerializer.Serialize(new Dictionary<string, int> { ["value"] = 551 }); return json.Contains("551", StringComparison.Ordinal); }
    public static bool Case0552() { var value = JsonSerializer.Deserialize<Dictionary<string, int>>("{\"value\":552}"); return value!["value"] == 552; }
    public static bool Case0553() { using var document = JsonDocument.Parse("{\"items\":[5,5,3]}"); return document.RootElement.GetProperty("items").EnumerateArray().Sum(item => item.GetInt32()) == 13; }
    public static bool Case0554() { var node = JsonNode.Parse("{\"case\":554}")!; return node["case"]!.GetValue<int>() == 554; }
    public static bool Case0555() { using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) { writer.WriteStartObject(); writer.WriteNumber("case", 555); writer.WriteEndObject(); } return Encoding.UTF8.GetString(stream.ToArray()) == "{\"case\":555}"; }
    public static bool Case0556() { var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }; return JsonSerializer.Serialize(new JsonModel(556), options) == "{\"value\":556}"; }
    public static bool Case0557() { var json = JsonSerializer.Serialize(new { Name = "case", Value = 557 }); return json.Contains("\"Value\":557", StringComparison.Ordinal); }
    public static bool Case0558() { var values = JsonSerializer.Deserialize<List<int>>(JsonSerializer.Serialize(new[] { 5, 5, 8 })); return values!.SequenceEqual(new[] { 5, 5, 8 }); }
    public static bool Case0559() { var json = JsonSerializer.Serialize("line\n559"); return JsonSerializer.Deserialize<string>(json) == "line\n559"; }
    public static bool Case0560() { using var document = JsonDocument.Parse("[560,1,2]"); var values = document.RootElement.EnumerateArray().Select(item => item.GetInt32()).ToArray(); return values[0] == 560 && values.Length == 3; }
    public static bool Case0561() { var document = XDocument.Parse("<root><value>561</value></root>"); return (int)document.Root!.Element("value")! == 561; }
    public static bool Case0562() { var root = new XElement("root", new XElement("value", 562)); return root.ToString(SaveOptions.DisableFormatting) == "<root><value>562</value></root>"; }
    public static bool Case0563() { var root = XElement.Parse("<r><x>5</x><g><x>6</x><x>3</x></g></r>"); return root.Descendants("x").Sum(item => (int)item) == 14; }
    public static bool Case0564() { var element = new XElement("case", new XAttribute("id", 564)); return (int)element.Attribute("id")! == 564; }
    public static bool Case0565() { XNamespace ns = "urn:case:565"; var element = new XElement(ns + "root", new XElement(ns + "value", 565)); return element.Element(ns + "value")!.Value == "565"; }
    public static bool Case0566() { var document = new XDocument(new XElement("root", "566")); using var writer = new StringWriter(CultureInfo.InvariantCulture); document.Save(writer, SaveOptions.DisableFormatting); return XDocument.Parse(writer.ToString()).Root!.Value == "566"; }
    public static bool Case0567() { using var reader = XmlReader.Create(new StringReader("<root><value>567</value></root>")); reader.ReadToFollowing("value"); return reader.ReadElementContentAsInt() == 567; }
    public static bool Case0568() { var output = new StringBuilder(); using (var writer = XmlWriter.Create(output, new XmlWriterSettings { OmitXmlDeclaration = true })) { writer.WriteStartElement("case"); writer.WriteValue(568); writer.WriteEndElement(); } return output.ToString() == "<case>568</case>"; }
    public static bool Case0569() { return XmlConvert.ToInt32(XmlConvert.ToString(569)) == 569; }
    public static bool Case0570() { var value = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc); return XmlConvert.ToDateTime(XmlConvert.ToString(value, XmlDateTimeSerializationMode.Utc), XmlDateTimeSerializationMode.Utc) == value; }
    public static bool Case0571() { return typeof(Dictionary<int, string>).IsGenericType && typeof(Dictionary<int, string>).GetGenericArguments().SequenceEqual(new[] { typeof(int), typeof(string) }); }
    public static bool Case0572() { var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!; return (int)method.Invoke(null, [-572])! == 572; }
    public static bool Case0573() { var type = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(int)); return type == typeof(Dictionary<string, int>); }
    public static bool Case0574() { return typeof(MarkedModel).GetCustomAttribute<MarkerAttribute>()?.Value == 574; }
    public static bool Case0575() { return Enum.GetValues<DayOfWeek>().Length == 7 && Enum.GetNames<DayOfWeek>().Contains(nameof(DayOfWeek.Friday)); }
    public static bool Case0576() { var builder = (StringBuilder)Activator.CreateInstance(typeof(StringBuilder))!; builder.Append(576); return builder.ToString() == "576"; }
    public static bool Case0577() { Delegate function = (Func<int, int>)(value => value + 7); return (int)function.DynamicInvoke(570)! == 577; }
    public static bool Case0578() { var property = typeof(string).GetProperty(nameof(string.Length))!; return (int)property.GetValue("12345678")! == 8; }
    public static bool Case0579() { return typeof(DateTime).GetField(nameof(DateTime.MinValue), BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!.Equals(DateTime.MinValue); }
    public static bool Case0580() { var constructors = typeof(List<int>).GetConstructors(); return constructors.Any(constructor => constructor.GetParameters().Length == 0); }
    public static bool Case0581() { return Type.GetTypeCode(typeof(decimal)) == TypeCode.Decimal && Type.GetTypeCode(typeof(DateTime)) == TypeCode.DateTime; }
    public static bool Case0582() { return Nullable.GetUnderlyingType(typeof(int?)) == typeof(int) && Nullable.GetUnderlyingType(typeof(int)) is null; }
    public static bool Case0583() { return typeof(int).MakeArrayType() == typeof(int[]) && typeof(int).MakeArrayType(2) == typeof(int[,]); }
    public static bool Case0584() { return typeof(List<>).MakeGenericType(typeof(Guid)) == typeof(List<Guid>); }
    public static bool Case0585() { return typeof(Stream).IsAssignableFrom(typeof(MemoryStream)) && !typeof(MemoryStream).IsAssignableFrom(typeof(Stream)); }
    public static bool Case0586() { return typeof(List<int>).GetInterfaces().Any(type => type == typeof(IReadOnlyList<int>)); }
    public static bool Case0587() { var method = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string), typeof(StringComparison)])!; return method.GetParameters()[1].ParameterType == typeof(StringComparison); }
    public static bool Case0588() { var attribute = typeof(MarkedModel).CustomAttributes.Single(item => item.AttributeType == typeof(MarkerAttribute)); return (int)attribute.ConstructorArguments[0].Value! == 574; }
    public static bool Case0589() { return typeof(Math).GetMethods(BindingFlags.Public | BindingFlags.Static).Count(method => method.Name == nameof(Math.Max)) >= 8; }
    public static bool Case0590() { var name = typeof(string).Assembly.GetName(); return name.Name == "System.Private.CoreLib" && name.Version is not null; }
    public static bool Case0591() { var builder = new UriBuilder("https", "example.test", 443, "api/items") { Query = "id=591" }; return builder.Uri.AbsolutePath == "/api/items" && builder.Uri.Query == "?id=591"; }
    public static bool Case0592() { var uri = new Uri("https://user:pass@example.test:8443/path?q=592#part"); return uri.UserInfo == "user:pass" && uri.Port == 8443 && uri.Fragment == "#part"; }
    public static bool Case0593() { var version = Version.Parse("5.9.3.7"); return version.Major == 5 && version.Minor == 9 && version.Build == 3 && version.Revision == 7; }
    public static bool Case0594() { return IPAddress.Parse("2001:db8::594").AddressFamily == AddressFamily.InterNetworkV6; }
    public static bool Case0595() { var values = new System.Collections.Specialized.NameValueCollection { ["case"] = "595" }; values.Add("case", "extra"); return values.GetValues("case")!.SequenceEqual(new[] { "595", "extra" }); }
    public static bool Case0596() { var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["CASE"] = 596 }; return values["case"] == 596; }
    public static bool Case0597() { var calls = 0; var value = new Lazy<int>(() => { calls++; return 597; }); return !value.IsValueCreated && value.Value == 597 && value.Value == 597 && calls == 1; }
    public static bool Case0598() { var target = new object(); var reference = new WeakReference<object>(target); return reference.TryGetTarget(out var found) && ReferenceEquals(found, target); }
    public static bool Case0599() { var fields = typeof((int Number, string Text)).GetFields(); return fields.Length == 2 && fields[0].FieldType == typeof(int) && fields[1].FieldType == typeof(string); }
    public static bool Case0600() { var pair = new KeyValuePair<string, int>("case", 600); return pair.Key == "case" && pair.Value == 600 && pair.ToString().Contains("600", StringComparison.Ordinal); }

    private sealed record JsonModel(int Value);

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class MarkerAttribute(int value) : Attribute {
        public int Value { get; } = value;
    }

    [Marker(574)]
    private sealed class MarkedModel;
}
