using System.IO.Compression;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 3, DecryptHashChance = 0.65)]
internal static class IoAlgorithmCases
{
    public static bool Case1101() { using var stream = new MemoryStream(); stream.WriteByte(unchecked((byte)1101)); stream.Position = 0; return stream.ReadByte() == 77; }
    public static bool Case1102() { using var stream = new MemoryStream(Encoding.UTF8.GetBytes("1102")); using var reader = new StreamReader(stream); return reader.ReadToEnd() == "1102"; }
    public static bool Case1103() { using var stream = new MemoryStream(); using (var writer = new StreamWriter(stream, leaveOpen: true)) writer.Write("1103"); return stream.Length == 4; }
    public static bool Case1104() { using var stream = new MemoryStream(); using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) writer.Write(1104); stream.Position = 0; using var reader = new BinaryReader(stream); return reader.ReadInt32() == 1104; }
    public static bool Case1105() { var bytes = Encoding.UTF8.GetBytes("1105"); using var input = new MemoryStream(bytes); using var output = new MemoryStream(); input.CopyTo(output); return output.ToArray().SequenceEqual(bytes); }
    public static bool Case1106() { using var stream = new MemoryStream(); using (var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true)) gzip.Write(Encoding.UTF8.GetBytes("1106")); stream.Position = 0; using var unzip = new GZipStream(stream, CompressionMode.Decompress); using var reader = new StreamReader(unzip); return reader.ReadToEnd() == "1106"; }
    public static bool Case1107() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1107.txt"); File.WriteAllText(path, "1107"); var result = File.ReadAllText(path); File.Delete(path); return result == "1107"; }
    public static bool Case1108() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1108.bin"); File.WriteAllBytes(path, [11, 8]); var result = File.ReadAllBytes(path); File.Delete(path); return result.SequenceEqual(new byte[] { 11, 8 }); }
    public static bool Case1109() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1109"); using (File.Create(path)) { } var result = File.Exists(path); File.Delete(path); return result; }
    public static bool Case1110() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1110.txt"); File.WriteAllLines(path, ["11", "10"]); var result = File.ReadLines(path).Select(int.Parse).Sum(); File.Delete(path); return result == 21; }
    public static bool Case1111() { using var stream = new MemoryStream(); stream.Write(BitConverter.GetBytes(1111)); return stream.ToArray().Length == sizeof(int); }
    public static bool Case1112() { var value = new StringWriter(CultureInfo.InvariantCulture); value.Write("{0}:{1}", 11, 12); return value.ToString() == "11:12"; }
    public static bool Case1113() { using var reader = new StringReader("11\n13"); return reader.ReadLine() == "11" && reader.ReadLine() == "13"; }
    public static bool Case1114() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1114"); Directory.CreateDirectory(path); var result = Directory.Exists(path); Directory.Delete(path); return result; }
    public static bool Case1115() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1115"); Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, "x"), "ok"); var result = Directory.EnumerateFiles(path).Single().EndsWith("x", StringComparison.Ordinal); Directory.Delete(path, true); return result; }
    public static bool Case1116() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1116"); File.WriteAllText(path, "x"); var info = new FileInfo(path); var result = info.Length == 1; info.Delete(); return result; }
    public static bool Case1117() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1117"); File.WriteAllText(path, "x"); var result = (File.GetAttributes(path) & FileAttributes.Directory) == 0; File.Delete(path); return result; }
    public static bool Case1118() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1118"); File.WriteAllText(path, "x"); var copy = path + ".copy"; File.Copy(path, copy); var result = File.ReadAllText(copy) == "x"; File.Delete(path); File.Delete(copy); return result; }
    public static bool Case1119() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1119"); File.WriteAllText(path, "x"); var moved = path + ".moved"; File.Move(path, moved); var result = File.Exists(moved); File.Delete(moved); return result; }
    public static bool Case1120() { var path = Path.Combine(Path.GetTempPath(), "sharphide-1120"); File.WriteAllText(path, "x"); using var handle = File.OpenHandle(path, FileMode.Open); var result = !handle.IsInvalid; handle.Close(); File.Delete(path); return result; }
    public static bool Case1121() { var value = new int[5]; Array.Fill(value, 1121); return value.Sum() == 5605; }
    public static bool Case1122() { var value = new int[5]; Array.Copy(new[] { 1, 2, 3 }, value, 3); return value[2] == 3; }
    public static bool Case1123() { var value = new[] { 1123, 1, 2 }; Array.Sort(value); return value[^1] == 1123; }
    public static bool Case1124() { var value = new[] { 1, 2, 3, 4 }; Array.Reverse(value); return value[0] == 4; }
    public static bool Case1125() { var value = new[] { 1, 2, 3, 4 }; return Array.IndexOf(value, 4) == 3; }
    public static bool Case1126() { var value = new[] { 1, 2, 3, 4 }; return Array.BinarySearch(value, 3) == 2; }
    public static bool Case1127() { var value = new[] { 1, 2, 3, 4 }; return Array.Exists(value, number => number == 4); }
    public static bool Case1128() { var value = new[] { 1, 2, 3, 4 }; return Array.FindLast(value, number => number % 2 == 0) == 4; }
    public static bool Case1129() { var value = new[] { 1, 2, 3, 4 }; return Array.TrueForAll(value, number => number > 0); }
    public static bool Case1130() { var value = new[] { 1, 2, 3, 4 }; return Array.ConvertAll(value, number => number * 2).Sum() == 20; }
    public static bool Case1131() { var value = new List<int> { 1, 2 }; value.InsertRange(1, [1131]); return value[1] == 1131; }
    public static bool Case1132() { var value = new LinkedList<int>([1, 2]); value.AddFirst(1132); return value.First!.Value == 1132; }
    public static bool Case1133() { var value = new Queue<int>([1, 2]); value.Enqueue(1133); return value.Dequeue() == 1 && value.Count == 2; }
    public static bool Case1134() { var value = new Stack<int>([1, 2]); value.Push(1134); return value.Pop() == 1134; }
    public static bool Case1135() { var value = new PriorityQueue<string, int>(); value.Enqueue("low", 2); value.Enqueue("high", 1); return value.Dequeue() == "high"; }
    public static bool Case1136() { var value = new SortedSet<int> { 3, 1, 2 }; return value.Min == 1 && value.Max == 3; }
    public static bool Case1137() { var value = new SortedDictionary<int, string> { [2] = "b", [1] = "a" }; return value.First().Value == "a"; }
    public static bool Case1138() { var value = new BitArray([true, false, true]); value.Not(); return !value[0] && value[1]; }
    public static bool Case1139() { var value = new System.Collections.Specialized.NameValueCollection { ["x"] = "1139" }; return value["x"] == "1139"; }
    public static bool Case1140() { var value = new System.Collections.Specialized.OrderedDictionary { ["x"] = 1140 }; return (int)value["x"]! == 1140; }
    public static bool Case1141() { var value = new Dictionary<int, string>(capacity: 2) { [1141] = "x" }; return value.TryGetValue(1141, out var found) && found == "x"; }
    public static bool Case1142() { var value = new HashSet<int> { 1, 2 }; value.SymmetricExceptWith([2, 1142]); return value.SetEquals([1, 1142]); }
    public static bool Case1143() { var value = new HashSet<int> { 1, 2, 3 }; return value.IsSupersetOf([1, 2]); }
    public static bool Case1144() { var value = new WeakReference<string>("1144"); return value.TryGetTarget(out var target) && target == "1144"; }
    public static bool Case1145() { var value = new ConditionalWeakTable<object, string>(); var key = new object(); value.Add(key, "1145"); return value.TryGetValue(key, out var found) && found == "1145"; }
    public static bool Case1146() { var value = new System.Collections.Concurrent.ConcurrentQueue<int>(); value.Enqueue(1146); return value.TryDequeue(out var found) && found == 1146; }
    public static bool Case1147() { var value = new System.Collections.Concurrent.ConcurrentStack<int>(); value.Push(1147); return value.TryPop(out var found) && found == 1147; }
    public static bool Case1148() { var value = new System.Collections.Concurrent.ConcurrentBag<int>(); value.Add(1148); return value.Contains(1148); }
    public static bool Case1149() { var value = new System.Collections.Concurrent.BlockingCollection<int>(); value.Add(1149); return value.Take() == 1149; }
    public static bool Case1150() { var value = new System.Collections.Concurrent.ConcurrentDictionary<int, int>(); return value.TryAdd(1150, 1) && value[1150] == 1; }
}
