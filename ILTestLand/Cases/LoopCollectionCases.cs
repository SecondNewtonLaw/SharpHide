internal static class LoopCollectionCases
{
    public static bool Case2251() { var sum = 0; for (var i = 1; i <= 5; i++) sum += i; return sum == 15; }
    public static bool Case2252() { var value = 1; var i = 0; while (i++ < 5) value *= 2; return value == 32; }
    public static bool Case2253() { var value = 0; var i = 0; do value += ++i; while (i < 5); return value == 15; }
    public static bool Case2254() { var value = 0; foreach (var item in new[] { 2, 2, 5, 4 }) value += item; return value == 13; }
    public static bool Case2255() { var value = 0; for (var i = 0; i < 10; i++) { if (i % 2 == 0) continue; value += i; } return value == 25; }
    public static bool Case2256() { var value = 0; for (var i = 0; i < 10; i++) { if (i == 6) break; value += i; } return value == 15; }
    public static bool Case2257() { var value = 0; for (var i = 1; i <= 3; i++) for (var j = 1; j <= 3; j++) value += i * j; return value == 36; }
    public static bool Case2258() { var value = 0; for (var i = 0; i < 4; i++) value = value * 10 + i; return value == 123; }
    public static bool Case2259() { var value = 2259; var count = 0; while (value > 0) { value /= 10; count++; } return count == 4; }
    public static bool Case2260() { var value = 0; foreach (var item in Enumerable.Range(1, 5).Reverse()) value = value * 10 + item; return value == 54321; }
    public static bool Case2261() { return Enumerable.Range(1, 10).Where(x => x % 2 == 0).Sum() == 30; }
    public static bool Case2262() { return Enumerable.Range(1, 5).Aggregate(1, (a, b) => a * b) == 120; }
    public static bool Case2263() { return Enumerable.Range(1, 5).Select(x => x * x).SequenceEqual(new[] { 1, 4, 9, 16, 25 }); }
    public static bool Case2264() { return Enumerable.Range(1, 5).Zip(Enumerable.Range(6, 5), (a, b) => a + b).Sum() == 55; }
    public static bool Case2265() { return new[] { "a", "bb", "ccc" }.GroupBy(x => x.Length).Count() == 3; }
    public static bool Case2266() { return new[] { 1, 2, 2, 3 }.Distinct().SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2267() { return new[] { 1, 2, 3 }.Skip(1).Take(1).Single() == 2; }
    public static bool Case2268() { return new[] { 1, 2, 3 }.FirstOrDefault(x => x > 2) == 3; }
    public static bool Case2269() { return new[] { 1, 2, 3 }.Last(x => x < 3) == 2; }
    public static bool Case2270() { return new[] { 1, 2, 3 }.Any(x => x == 2) && new[] { 1, 2, 3 }.All(x => x > 0); }
    public static bool Case2271() { var list = new List<int> { 1, 2 }; list.AddRange(new[] { 3, 4 }); return list.Count == 4 && list[3] == 4; }
    public static bool Case2272() { var list = new LinkedList<int>(new[] { 1, 3 }); list.AddAfter(list.First!, 2); return list.SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2273() { var set = new HashSet<int> { 1, 2 }; set.UnionWith(new[] { 2, 3 }); return set.SetEquals(new[] { 1, 2, 3 }); }
    public static bool Case2274() { var map = new Dictionary<string, int> { ["a"] = 1 }; map["b"] = 2; return map.Values.Sum() == 3; }
    public static bool Case2275() { var queue = new Queue<int>(new[] { 1, 2 }); queue.Enqueue(3); return queue.Dequeue() == 1 && queue.Count == 2; }
    public static bool Case2276() { var stack = new Stack<int>(new[] { 1, 2 }); stack.Push(3); return stack.Pop() == 3 && stack.Peek() == 2; }
    public static bool Case2277() { var sorted = new SortedSet<int>(new[] { 3, 1, 2 }); return sorted.SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2278() { var map = new SortedDictionary<int, string> { [2] = "b", [1] = "a" }; return map.First().Value == "a"; }
    public static bool Case2279() { var pair = (left: 22, right: 79); return pair.left + pair.right == 101; }
    public static bool Case2280() { var values = new[] { 1, 2, 3 }; return Array.IndexOf(values, 2) == 1; }
    public static bool Case2281() { var values = new[] { 1, 2, 3 }; Array.Reverse(values); return values.SequenceEqual(new[] { 3, 2, 1 }); }
    public static bool Case2282() { var values = new int[3]; Array.Fill(values, 2282); return values.All(x => x == 2282); }
    public static bool Case2283() { var values = new[] { 1, 2, 3, 4 }; Array.Copy(values, 1, values, 0, 3); return values[0] == 2; }
    public static bool Case2284() { return Enumerable.Repeat(2284, 3).Sum() == 6852; }
    public static bool Case2285() { return Enumerable.Empty<int>().DefaultIfEmpty(2285).Single() == 2285; }
    public static bool Case2286() { return Enumerable.Range(1, 3).SelectMany(x => new[] { x, -x }).Sum() == 0; }
    public static bool Case2287() { return new[] { 1, 2, 3 }.Aggregate((a, b) => a + b) == 6; }
    public static bool Case2288() { return new[] { 1, 2, 3 }.ElementAtOrDefault(5) == 0; }
    public static bool Case2289() { return new[] { 1, 2, 3 }.Concat(new[] { 4 }).Count() == 4; }
    public static bool Case2290() { return new[] { 1, 2, 3 }.Prepend(0).Append(4).SequenceEqual(new[] { 0, 1, 2, 3, 4 }); }
    public static bool Case2291() { return new[] { 1, 2, 3 }.Reverse().SequenceEqual(new[] { 3, 2, 1 }); }
    public static bool Case2292() { return new[] { 1, 2, 3 }.OrderByDescending(x => x).First() == 3; }
    public static bool Case2293() { return new[] { 1, 2, 3 }.SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2294() { return new[] { 1, 2, 3 }.Except(new[] { 2 }).SequenceEqual(new[] { 1, 3 }); }
    public static bool Case2295() { return new[] { 1, 2, 3 }.Intersect(new[] { 2, 3, 4 }).SequenceEqual(new[] { 2, 3 }); }
    public static bool Case2296() { return new[] { 1, 2 }.Union(new[] { 2, 3 }).SequenceEqual(new[] { 1, 2, 3 }); }
    public static bool Case2297() { return new[] { 1, 2, 3 }.Aggregate(0, (sum, x) => sum + x * x) == 14; }
    public static bool Case2298() { return new[] { 1, 2, 3 }.Select((x, i) => x + i).Sum() == 9; }
    public static bool Case2299() { return new[] { 1, 2, 3 }.Chunk(2).Count() == 2; }
    public static bool Case2300() { return new[] { 1, 2, 3 }.ToLookup(x => x % 2)[1].Count() == 2; }
}
