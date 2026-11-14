using System.Numerics;
using System.Collections.Immutable;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1700, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class GenericMathCases
{
    public static bool Case1501() { return Add(1500, 1) == 1501; }
    public static bool Case1502() { return Add(1500L, 2L) == 1502L; }
    public static bool Case1503() { return Add(1500m, 3m) == 1503m; }
    public static bool Case1504() { return Multiply(1504, 1) == 1504; }
    public static bool Case1505() { return Multiply(1500L, 1L) + 5 == 1505L; }
    public static bool Case1506() { return Clamp(1506, 0, 2000) == 1506; }
    public static bool Case1507() { return Clamp(-1, 0, 1507) == 0; }
    public static bool Case1508() { return Max(1508, 1) == 1508; }
    public static bool Case1509() { return Min(1509, 2000) == 1509; }
    public static bool Case1510() { return Abs(-1510) == 1510; }
    public static bool Case1511() { return Sign(-1511) == -1; }
    public static bool Case1512() { return Create(1512) == 1512; }
    public static bool Case1513() { return Create(1513L) == 1513L; }
    public static bool Case1514() { return Create(1514m) == 1514m; }
    public static bool Case1515() { return Parse<int>("1515") == 1515; }
    public static bool Case1516() { return Parse<long>("1516") == 1516L; }
    public static bool Case1517() { return Parse<decimal>("1517") == 1517m; }
    public static bool Case1518() { return Zero<int>() == 0; }
    public static bool Case1519() { return One<int>() == 1; }
    public static bool Case1520() { return Sum(new[] { 1, 2, 3, 1520 }) == 1526; }
    public static bool Case1521() { return Product(new[] { 1, 2, 3, 1521 }) == 9126; }
    public static bool Case1522() { var value = new[] { 1, 2, 3 }.Select(number => Add(number, 1522)); return value.SequenceEqual(new[] { 1523, 1524, 1525 }); }
    public static bool Case1523() { var value = new[] { 1, 2, 3 }.Select(number => Multiply(number, 1523)); return value.Last() == 4569; }
    public static bool Case1524() { var value = new[] { 1, 2, 3 }.Aggregate(Zero<int>(), Add); return value == 6; }
    public static bool Case1525() { var value = new[] { 1, 2, 3 }.Aggregate(One<int>(), Multiply); return value == 6; }
    public static bool Case1526() { var value = new Range(1, 5); return value.Start.Value == 1 && value.End.Value == 5; }
    public static bool Case1527() { var value = new Index(1527, fromEnd: true); return value.IsFromEnd && value.Value == 1527; }
    public static bool Case1528() { var value = new[] { 1, 2, 3, 4 }; return value[^1] == 4 && value[1..3].Length == 2; }
    public static bool Case1529() { var value = (First: 15, Second: 29); return value.First * 100 + value.Second == 1529; }
    public static bool Case1530() { var value = (15, 30); var (left, right) = value; return left + right == 45; }
    public static bool Case1531() { var value = new Pair(15, 31); var (left, right) = value; return left + right == 46; }
    public static bool Case1532() { var value = new Pair(15, 32) with { Second = 1532 }; return value.Second == 1532; }
    public static bool Case1533() { var value = new[] { new Pair(1, 2), new Pair(15, 33) }; return value.MaxBy(pair => pair.Second).Second == 33; }
    public static bool Case1534() { var value = new[] { new Pair(1, 2), new Pair(15, 34) }.OrderBy(pair => pair.First); return value.Last().Second == 34; }
    public static bool Case1535() { var value = new[] { 1, 2, 3 }.Select(number => new Pair(number, number * 2)); return value.All(pair => pair.Second == pair.First * 2); }
    public static bool Case1536() { var value = EqualityComparer<Pair>.Default.Equals(new Pair(15, 36), new Pair(15, 36)); return value; }
    public static bool Case1537() { var value = Comparer<Pair>.Create((left, right) => left.Second.CompareTo(right.Second)); return value.Compare(new Pair(15, 37), new Pair(15, 1)) > 0; }
    public static bool Case1538() { var value = new[] { 3, 1, 2 }; Array.Sort(value, Comparer<int>.Create((left, right) => right.CompareTo(left))); return value[0] == 3; }
    public static bool Case1539() { var value = new SortedSet<int>(Comparer<int>.Create((left, right) => right.CompareTo(left))) { 1, 3, 2 }; return value.First() == 3; }
    public static bool Case1540() { var value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["X"] = 1540 }; return value["x"] == 1540; }
    public static bool Case1541() { var value = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A" }; return value.Contains("a"); }
    public static bool Case1542() { var value = new[] { "A", "a", "B" }.Distinct(StringComparer.OrdinalIgnoreCase); return value.Count() == 2; }
    public static bool Case1543() { var value = string.Join(',', new[] { "A", "a", "B" }.Distinct(StringComparer.OrdinalIgnoreCase)); return value == "A,B"; }
    public static bool Case1544() { var value = new[] { 1, 2, 3 }.ToImmutableArray(); return value.Length == 3 && value[2] == 3; }
    public static bool Case1545() { var value = System.Collections.Immutable.ImmutableHashSet.Create(1, 2, 1545); return value.Contains(1545); }
    public static bool Case1546() { var value = System.Collections.Immutable.ImmutableDictionary.CreateRange(new[] { KeyValuePair.Create("x", 1546) }); return value["x"] == 1546; }
    public static bool Case1547() { var value = System.Collections.Immutable.ImmutableQueue.Create(1547); value = value.Enqueue(1); return value.Peek() == 1547; }
    public static bool Case1548() { var value = System.Collections.Immutable.ImmutableStack.Create(1548); value = value.Push(1); return value.Peek() == 1; }
    public static bool Case1549() { var value = System.Collections.Immutable.ImmutableList.Create(1549); value = value.Add(1); return value[0] == 1549; }
    public static bool Case1550() { var value = System.Collections.Immutable.ImmutableSortedSet.Create(1550, 1, 2); return value.Max == 1550; }

    private static T Add<T>(T left, T right) where T : IAdditionOperators<T, T, T> => left + right;
    private static T Multiply<T>(T left, T right) where T : IMultiplyOperators<T, T, T> => left * right;
    private static T Clamp<T>(T value, T min, T max) where T : INumber<T> => T.Clamp(value, min, max);
    private static T Max<T>(T left, T right) where T : INumber<T> => T.Max(left, right);
    private static T Min<T>(T left, T right) where T : INumber<T> => T.Min(left, right);
    private static T Abs<T>(T value) where T : INumber<T> => T.Abs(value);
    private static int Sign<T>(T value) where T : INumber<T> => T.Sign(value);
    private static T Create<T>(T value) where T : INumber<T> => value;
    private static T Parse<T>(string value) where T : INumber<T> => T.Parse(value, null);
    private static T Zero<T>() where T : INumber<T> => T.Zero;
    private static T One<T>() where T : INumber<T> => T.One;
    private static T Sum<T>(IEnumerable<T> values) where T : INumber<T> => values.Aggregate(T.Zero, Add);
    private static T Product<T>(IEnumerable<T> values) where T : INumber<T> => values.Aggregate(T.One, Multiply);
    private readonly record struct Pair(int First, int Second);
}
