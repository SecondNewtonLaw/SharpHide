using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1900, BogusStates = 7, OpaqueDepth = 4, DecryptHashChance = 0.7)]
internal static class AdvancedAlgorithmCases
{
    public static bool Case1201() { var value = Enumerable.Range(1, 20).Aggregate(0, (sum, item) => sum ^ item); return value == 20; }
    public static bool Case1202() { var value = Enumerable.Range(1, 20).Aggregate(0, (sum, item) => sum | item); return value == 31; }
    public static bool Case1203() { var value = Enumerable.Range(1, 20).Aggregate(0, (sum, item) => sum & item); return value == 0; }
    public static bool Case1204() { var value = Enumerable.Range(1, 20).Aggregate(0, (sum, item) => sum + item * item); return value == 2870; }
    public static bool Case1205() { var value = Enumerable.Range(1, 20).Reverse().Select(item => item % 10).ToArray(); return value.Length == 20 && value[0] == 0 && value[^1] == 1; }
    public static bool Case1206() { var value = Enumerable.Range(1, 20).Select(Factorial).ToArray(); return value[5] == 720 && value[9] == 3628800; }
    public static bool Case1207() { var value = Enumerable.Range(1, 20).Select(Fibonacci).ToArray(); return value[10] == 89 && value[19] == 6765; }
    public static bool Case1208() { return IsPrime(1201) && !IsPrime(1208); }
    public static bool Case1209() { var value = Enumerable.Range(1, 1209).Count(IsPrime); return value > 190 && value < 205; }
    public static bool Case1210() { var value = new[] { 1193, 1201 }.Where(IsPrime).ToArray(); return value.SequenceEqual(new[] { 1193, 1201 }); }
    public static bool Case1211() { var value = Gcd(1211, 11); return value == 1; }
    public static bool Case1212() { var value = Lcm(12, 12); return value == 12; }
    public static bool Case1213() { var value = Euclid(1213, 13); return value == 1; }
    public static bool Case1214() { var value = new[] { 8, 4, 2, 1 }; return value.Zip(value.Skip(1)).All(pair => pair.First == pair.Second * 2); }
    public static bool Case1215() { var value = new[] { 1, 2, 3, 4, 5 }; return value.SequenceEqual(value.Reverse().Reverse()); }
    public static bool Case1216() { var value = "1216".Reverse().Aggregate("", (text, character) => text + character); return value == "6121"; }
    public static bool Case1217() { var value = "racecar"; return value.SequenceEqual(value.Reverse()); }
    public static bool Case1218() { var value = "aabbcc".GroupBy(character => character).ToDictionary(group => group.Key, group => group.Count()); return value.Values.All(count => count == 2); }
    public static bool Case1219() { var value = "abracadabra".GroupBy(character => character).OrderByDescending(group => group.Count()).First().Key; return value == 'a'; }
    public static bool Case1220() { var value = "1220".Select(character => character - '0').Aggregate(0, (sum, item) => sum + item); return value == 5; }
    public static bool Case1221() { var value = new Dictionary<char, int>(); foreach (var character in "1221") value[character] = value.GetValueOrDefault(character) + 1; return value['1'] == 2 && value['2'] == 2; }
    public static bool Case1222() { var value = "1222".GroupBy(character => character).Select(group => group.Count()).Order().ToArray(); return value.SequenceEqual(new[] { 1, 3 }); }
    public static bool Case1223() { var value = new[] { 1, 2, 3, 4, 5 }.Select((item, index) => item - index); return value.Distinct().Single() == 1; }
    public static bool Case1224() { var value = Enumerable.Range(1, 10).SelectMany(left => Enumerable.Range(left + 1, 10 - left)); return value.Count() == 45; }
    public static bool Case1225() { var value = Enumerable.Range(1, 5).SelectMany(left => Enumerable.Range(1, 5).Select(right => left * right)); return value.Max() == 25 && value.Count() == 25; }
    public static bool Case1226() { var value = new[] { 1, 2, 3, 4, 5 }; var result = value.Aggregate((left, right) => left + right); return result == 15; }
    public static bool Case1227() { var value = new[] { 1, 2, 3, 4, 5 }; return value.Aggregate((left, right) => left * right) == 120; }
    public static bool Case1228() { var value = Enumerable.Range(1, 10).Select(number => number % 2 == 0 ? number / 2 : number * 3 + 1).ToArray(); return value[^1] == 5; }
    public static bool Case1229() { var value = Enumerable.Range(1, 100).Select(number => number.ToString().Length).Sum(); return value == 192; }
    public static bool Case1230() { var value = Enumerable.Range(1, 100).Count(number => number.ToString().Contains('7')); return value == 19; }
    public static bool Case1231() { var value = Enumerable.Range(1, 100).Where(number => number % 10 == number / 10).ToArray(); return value.SequenceEqual(new[] { 11, 22, 33, 44, 55, 66, 77, 88, 99 }); }
    public static bool Case1232() { var value = Enumerable.Range(1, 50).Sum(number => number % 3 == 0 ? number : 0); return value == 408; }
    public static bool Case1233() { var value = Enumerable.Range(1, 50).Sum(number => number % 3 != 0 ? number : 0); return value == 867; }
    public static bool Case1234() { var value = Enumerable.Range(1, 34).Chunk(4).Select(chunk => chunk.Sum()).ToArray(); return value.Length == 9 && value[^1] == 67; }
    public static bool Case1235() { var value = Enumerable.Range(1, 35).Chunk(5).Select(chunk => chunk.Sum()).ToArray(); return value.Sum() == 630 && value.Length == 7; }
    public static bool Case1236() { var value = new[] { 3, 1, 4, 1, 5, 9 }; return value.Order().SequenceEqual(new[] { 1, 1, 3, 4, 5, 9 }); }
    public static bool Case1237() { var value = new[] { 3, 1, 4, 1, 5, 9 }; return value.OrderDescending().First() == 9; }
    public static bool Case1238() { var value = new[] { "a", "bb", "ccc" }; return value.MaxBy(text => text.Length) == "ccc"; }
    public static bool Case1239() { var value = new[] { "a", "bb", "ccc" }; return value.MinBy(text => text.Length) == "a"; }
    public static bool Case1240() { var value = Enumerable.Range(1, 10).Aggregate(new List<int>(), (list, item) => { list.Add(item * 2); return list; }); return value[^1] == 20; }
    public static bool Case1241() { var value = new int[10]; for (var index = 0; index < value.Length; index++) value[index] = index * index; return value.Sum() == 285; }
    public static bool Case1242() { var value = new int[10]; for (var index = 0; index < value.Length; index++) value[index] = index % 2; return value.Count(item => item == 1) == 5; }
    public static bool Case1243() { var value = 0; for (var index = 0; index < 10; index++) for (var inner = 0; inner < index; inner++) value++; return value == 45; }
    public static bool Case1244() { var value = 0; for (var index = 0; index < 10; index++) { if (index == 5) continue; value += index; } return value == 40; }
    public static bool Case1245() { var value = 0; for (var index = 0; index < 10; index++) { if (index == 5) break; value += index; } return value == 10; }
    public static bool Case1246() { var value = 0; var index = 0; while (index < 10) value += index++; return value == 45; }
    public static bool Case1247() { var value = 0; var index = 0; do value += ++index; while (index < 9); return value == 45; }
    public static bool Case1248() { var value = 0; for (var index = 0; index < 10; index++) value += index % 2 == 0 ? 2 : -1; return value == 5; }
    public static bool Case1249() { var value = 0; foreach (var item in new[] { 1, 2, 3, 4, 5 }) value = (value << 1) ^ item; return value == 1; }
    public static bool Case1250() { var value = 0; foreach (var item in new[] { 1, 2, 3, 4, 5 }) value = (value * 31) + item; return value == 986115; }

    private static int Factorial(int value) { var result = 1; for (var index = 2; index <= value; index++) result *= index; return result; }
    private static int Fibonacci(int value) { var left = 0; var right = 1; for (var index = 0; index < value; index++) (left, right) = (right, left + right); return left; }
    private static bool IsPrime(int value) { if (value < 2) return false; for (var divisor = 2; divisor * divisor <= value; divisor++) if (value % divisor == 0) return false; return true; }
    private static int Gcd(int left, int right) { while (right != 0) (left, right) = (right, left % right); return left; }
    private static int Lcm(int left, int right) => left / Gcd(left, right) * right;
    private static int Euclid(int left, int right) => Gcd(left, right);
}
