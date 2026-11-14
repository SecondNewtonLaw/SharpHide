using System.Collections;

internal static class AlgorithmCases2
{
    public static bool Case2701() { return Enumerable.Range(1, 10).Sum() == 55; }
    public static bool Case2702() { return Enumerable.Range(1, 10).Aggregate(1, (a, b) => a * b) == 3628800; }
    public static bool Case2703() { return Enumerable.Range(1, 10).Where(x => x % 2 == 0).Count() == 5; }
    public static bool Case2704() { return Enumerable.Range(1, 10).Select(x => x * x).Max() == 100; }
    public static bool Case2705() { return Enumerable.Range(1, 10).Average() == 5.5; }
    public static bool Case2706() { return Enumerable.Range(1, 10).Contains(6); }
    public static bool Case2707() { return Enumerable.Range(1, 10).Single(x => x == 7) == 7; }
    public static bool Case2708() { return Enumerable.Range(1, 10).ElementAt(7) == 8; }
    public static bool Case2709() { return Enumerable.Range(1, 10).Skip(5).First() == 6; }
    public static bool Case2710() { return Enumerable.Range(1, 10).Take(5).Last() == 5; }
    public static bool Case2711() { return Enumerable.Range(1, 10).TakeWhile(x => x < 6).Count() == 5; }
    public static bool Case2712() { return Enumerable.Range(1, 10).SkipWhile(x => x < 6).First() == 6; }
    public static bool Case2713() { return new[] { 1, 2, 3 }.Aggregate((a, b) => a + b) == 6; }
    public static bool Case2714() { return new[] { "a", "b", "c" }.Aggregate(string.Concat) == "abc"; }
    public static bool Case2715() { return new[] { 1, 2, 3 }.SelectMany(x => new[] { x, x }).Count() == 6; }
    public static bool Case2716() { return new[] { 1, 2, 3 }.Zip(new[] { 4, 5, 6 }, (a, b) => a * b).Sum() == 32; }
    public static bool Case2717() { return new[] { "a", "bb", "c" }.GroupBy(x => x.Length).Count() == 2; }
    public static bool Case2718() { return new[] { 1, 2, 2, 3 }.ToLookup(x => x).Count == 3; }
    public static bool Case2719() { var value = new PriorityQueue<string, int>(); value.Enqueue("low", 2); value.Enqueue("high", 1); return value.Dequeue() == "high"; }
    public static bool Case2720() { var value = new HashSet<int> { 2720 }; return !value.Add(2720); }
    public static bool Case2721() { var value = new Dictionary<int, string>(); value.TryAdd(2721, "x"); return value.ContainsKey(2721); }
    public static bool Case2722() { var value = new SortedList<int, string> { [2] = "b", [1] = "a" }; return value.Keys[0] == 1; }
    public static bool Case2723() { var value = new BitArray(new[] { true, false, true }); return value[0] && !value[1]; }
    public static bool Case2724() { var value = new BitArray(4, true); value.Not(); return !value[0]; }
    public static bool Case2725() { var value = new BitArray(new[] { true, false }); value.And(new BitArray(new[] { true, true })); return value[0] && !value[1]; }
    public static bool Case2726() { var value = new BitArray(new[] { true, false }); value.Or(new BitArray(new[] { false, true })); return value[0] && value[1]; }
    public static bool Case2727() { var value = new BitArray(new[] { true, false }); value.Xor(new BitArray(new[] { true, true })); return !value[0] && value[1]; }
    public static bool Case2728() { return Array.BinarySearch(new[] { 1, 2, 3, 4 }, 3) == 2; }
    public static bool Case2729() { var value = new[] { 4, 1, 3, 2 }; Array.Sort(value); return value.SequenceEqual(new[] { 1, 2, 3, 4 }); }
    public static bool Case2730() { var value = new[] { 1, 2, 3, 4 }; Array.Reverse(value); return value[0] == 4; }
    public static bool Case2731() { return Array.Exists(new[] { 1, 2, 3 }, x => x == 2); }
    public static bool Case2732() { return Array.Find(new[] { 1, 2, 3 }, x => x > 1) == 2; }
    public static bool Case2733() { return Array.FindIndex(new[] { 1, 2, 3 }, x => x == 3) == 2; }
    public static bool Case2734() { return Array.FindLast(new[] { 1, 2, 3, 2 }, x => x == 2) == 2; }
    public static bool Case2735() { return Array.TrueForAll(new[] { 1, 2, 3 }, x => x > 0); }
    public static bool Case2736() { var value = new int[3]; Array.Fill(value, 2736); return value[1] == 2736; }
    public static bool Case2737() { return Array.IndexOf(new[] { 1, 2, 3 }, 2) == 1; }
    public static bool Case2738() { return Array.LastIndexOf(new[] { 1, 2, 3, 2 }, 2) == 3; }
    public static bool Case2739() { return Enumerable.Range(1, 100).Chunk(10).Count() == 10; }
    public static bool Case2740() { return Enumerable.Range(1, 5).Prepend(0).First() == 0; }
    public static bool Case2741() { return Enumerable.Range(1, 5).Append(6).Last() == 6; }
    public static bool Case2742() { return Enumerable.Range(1, 5).Distinct().Count() == 5; }
    public static bool Case2743() { return Enumerable.Range(1, 5).Except(new[] { 2, 4 }).SequenceEqual(new[] { 1, 3, 5 }); }
    public static bool Case2744() { return Enumerable.Range(1, 5).Intersect(new[] { 2, 4, 6 }).SequenceEqual(new[] { 2, 4 }); }
    public static bool Case2745() { return Enumerable.Range(1, 3).Union(new[] { 3, 4 }).SequenceEqual(new[] { 1, 2, 3, 4 }); }
    public static bool Case2746() { return Enumerable.Range(1, 5).Reverse().SequenceEqual(new[] { 5, 4, 3, 2, 1 }); }
    public static bool Case2747() { return Enumerable.Range(1, 5).OrderByDescending(x => x).First() == 5; }
    public static bool Case2748() { return new[] { 1, 2, 3 }.All(x => x < 4); }
    public static bool Case2749() { return new[] { 1, 2, 3 }.Any(x => x == 2); }
    public static bool Case2750() { return new[] { 1, 2, 3 }.Count(x => x > 1) == 2; }
}
