using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1300, BogusStates = 5, OpaqueDepth = 3,
    DecryptHashChance = 0.6)]
internal static class AdvancedCollectionCases {
    public static bool Case0701() { var queue = new PriorityQueue<string, int>(); queue.Enqueue("later", 9); queue.Enqueue("first", 1); return queue.Dequeue() == "first" && queue.Count == 1; }
    public static bool Case0702() { var queue = new PriorityQueue<int, int>(); queue.EnqueueRange(new[] { (702, 3), (7, 1), (2, 2) }); return queue.Dequeue() == 7 && queue.Dequeue() == 2 && queue.Dequeue() == 702; }
    public static bool Case0703() { var queue = new PriorityQueue<int, int>(Comparer<int>.Create((left, right) => right.CompareTo(left))); queue.Enqueue(7, 1); queue.Enqueue(703, 9); return queue.Dequeue() == 703; }
    public static bool Case0704() { var queue = new PriorityQueue<int, int>(); var capacity = queue.EnsureCapacity(704); return capacity >= 704 && queue.Count == 0; }
    public static bool Case0705() { var queue = new PriorityQueue<string, int>(); queue.Enqueue("remove", 1); queue.Enqueue("keep", 2); return queue.Remove("remove", out var removed, out var priority) && removed == "remove" && priority == 1 && queue.Peek() == "keep"; }
    public static bool Case0706() { var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Case"] = 706 }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase); return values["case"] == 706 && values.Count == 1; }
    public static bool Case0707() { var values = new[] { "A", "a", "B" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase); return values.Count == 2 && values.Contains("b"); }
    public static bool Case0708() { var values = ImmutableArray.Create(700, 8).Add(1); return values.Length == 3 && values.Sum() == 709; }
    public static bool Case0709() { var original = ImmutableList.Create(7, 0, 9); var changed = original.SetItem(1, 700); return original[1] == 0 && changed.Sum() == 716; }
    public static bool Case0710() { var values = ImmutableDictionary<string, int>.Empty.Add("case", 710); return values["case"] == 710 && values.SetItem("case", 7)["case"] == 7; }
    public static bool Case0711() { var values = ImmutableHashSet.Create(7, 1, 1).Union(new[] { 0, 7 }); return values.SetEquals(new[] { 0, 1, 7 }); }
    public static bool Case0712() { var queue = ImmutableQueue<int>.Empty.Enqueue(700).Enqueue(12); queue = queue.Dequeue(out var first); return first == 700 && queue.Peek() == 12; }
    public static bool Case0713() { var stack = ImmutableStack<int>.Empty.Push(700).Push(13); stack = stack.Pop(out var first); return first == 13 && stack.Peek() == 700; }
    public static bool Case0714() { var values = ImmutableSortedSet.Create(7, 1, 4, 7, 1, 4); return values.SequenceEqual(new[] { 1, 4, 7 }); }
    public static bool Case0715() { var values = ImmutableArray<int>.Empty; ImmutableInterlocked.Update(ref values, current => current.Add(715)); return values.Single() == 715; }
    public static bool Case0716() { var values = new SortedSet<int> { 7, 1, 6 }; values.UnionWith(new[] { 2, 7 }); return values.SequenceEqual(new[] { 1, 2, 6, 7 }); }
    public static bool Case0717() { var values = new SortedSet<int> { 7, 1, 7, 4 }; values.IntersectWith(new[] { 1, 4, 9 }); return values.SetEquals(new[] { 1, 4 }); }
    public static bool Case0718() { var values = new SortedDictionary<int, string> { [7] = "seven", [1] = "one", [8] = "eight" }; return values.Keys.SequenceEqual(new[] { 1, 7, 8 }); }
    public static bool Case0719() { var bits = new BitArray(8); bits[7] = true; bits[1] = true; bits[0] = true; return bits.Cast<bool>().Count(value => value) == 3; }
    public static bool Case0720() { var left = new BitArray(new[] { true, true, false }); var right = new BitArray(new[] { true, false, true }); left.And(right); return left.Cast<bool>().SequenceEqual(new[] { true, false, false }); }
    public static bool Case0721() { var left = new BitArray(new[] { true, true, false }); left.Xor(new BitArray(new[] { true, false, true })); return left.Cast<bool>().SequenceEqual(new[] { false, true, true }); }
    public static bool Case0722() { var values = new Collection<int> { 700, 20, 2 }; values.Insert(1, 1); values.RemoveAt(2); return values.SequenceEqual(new[] { 700, 1, 2 }); }
    public static bool Case0723() { var source = new List<int> { 7, 2, 3 }; var values = source.AsReadOnly(); source.Add(4); return values.Count == 4 && values[3] == 4; }
    public static bool Case0724() { var values = new ObservableCollection<int>(); var changes = 0; values.CollectionChanged += (_, args) => changes += args.NewItems?.Count ?? 0; values.Add(700); values.Add(24); return changes == 2 && values.Sum() == 724; }
    public static bool Case0725() { var values = new ModelCollection { new(7), new(2), new(5) }; return values[2].Id == 2 && values.Contains(5); }
    public static bool Case0726() { var values = new LinkedList<int>(); var middle = values.AddFirst(20); values.AddBefore(middle, 700); values.AddAfter(middle, 6); return values.SequenceEqual(new[] { 700, 20, 6 }); }
    public static bool Case0727() { var queue = new Queue<int>(); queue.EnsureCapacity(727); queue.Enqueue(700); queue.Enqueue(27); return queue.Dequeue() + queue.Peek() == 727; }
    public static bool Case0728() { var stack = new Stack<int>(); stack.EnsureCapacity(728); stack.Push(700); stack.Push(28); return stack.Pop() == 28 && stack.Peek() == 700; }
    public static bool Case0729() { var values = new Dictionary<int, int> { [7] = 700, [2] = 20, [9] = 9 }; return values.TryGetValue(7, out var first) && values.TryGetValue(9, out var last) && first + last == 709; }
    public static bool Case0730() { var values = new HashSet<int> { 7, 3, 0 }; values.SymmetricExceptWith(new[] { 3, 1 }); return values.SetEquals(new[] { 0, 1, 7 }); }
    public static bool Case0731() { var values = new SortedList<string, int> { ["b"] = 31, ["a"] = 700 }; return values.Keys[0] == "a" && values.Values.Sum() == 731; }
    public static bool Case0732() { var values = new Hashtable { ["case"] = 732, [7] = "seven" }; return (int)values["case"]! == 732 && (string)values[7]! == "seven"; }
    public static bool Case0733() { var values = new ArrayList { 700, 30, 3 }; values.Sort(); return values.Cast<int>().SequenceEqual(new[] { 3, 30, 700 }); }
    public static bool Case0734() { var values = new Queue(); values.Enqueue(700); values.Enqueue(34); return (int)values.Dequeue()! + (int)values.Peek()! == 734; }
    public static bool Case0735() { var values = new Stack(); values.Push(700); values.Push(35); return (int)values.Pop()! == 35 && (int)values.Peek()! == 700; }
    public static bool Case0736() { var values = new NameValueCollection { ["case"] = "700" }; values.Add("case", "36"); return values.GetValues("case")!.SequenceEqual(new[] { "700", "36" }); }
    public static bool Case0737() { var values = new StringCollection { "700", "30", "7" }; return values.Cast<string>().Sum(value => int.Parse(value)) == 737; }
    public static bool Case0738() { var values = new HybridDictionary(true) { ["CASE"] = 738 }; return (int)values["case"]! == 738; }
    public static bool Case0739() { var values = new ListDictionary { ["first"] = 700, ["second"] = 39 }; return values.Count == 2 && values.Values.Cast<int>().Sum() == 739; }
    public static bool Case0740() { var values = new OrderedDictionary { ["b"] = 40, ["a"] = 700 }; return (string)values.Cast<DictionaryEntry>().First().Key == "b" && values.Values.Cast<int>().Sum() == 740; }
    public static bool Case0741() { var table = new ConditionalWeakTable<object, Box>(); var key = new object(); table.Add(key, new Box(741)); return table.TryGetValue(key, out var value) && value.Value == 741; }
    public static bool Case0742() { var table = new ConditionalWeakTable<object, Box>(); var key = new object(); var first = table.GetValue(key, _ => new Box(742)); var second = table.GetValue(key, _ => new Box(0)); return ReferenceEquals(first, second) && second.Value == 742; }
    public static bool Case0743() { var values = new[] { "a", "bbbb", "cc" }; Array.Sort(values, Comparer<string>.Create((left, right) => left.Length.CompareTo(right.Length))); return values.SequenceEqual(new[] { "a", "cc", "bbbb" }); }
    public static bool Case0744() { var comparer = EqualityComparer<(int, string)>.Default; return comparer.Equals((744, "x"), (744, "x")) && !comparer.Equals((744, "x"), (7, "x")); }
    public static bool Case0745() { var comparer = StructuralComparisons.StructuralEqualityComparer; return comparer.Equals(new[] { 7, 4, 5 }, new[] { 7, 4, 5 }) && !comparer.Equals(new[] { 7 }, new[] { 4 }); }
    public static bool Case0746() { var comparer = StructuralComparisons.StructuralComparer; return comparer.Compare(Tuple.Create(7, 4, 6), Tuple.Create(7, 4, 7)) < 0; }
    public static bool Case0747() { var lookup = new[] { "ant", "ape", "bear", "bee" }.ToLookup(value => value[0]); return lookup['a'].Count() == 2 && lookup['b'].Count() == 2; }
    public static bool Case0748() { var totals = new[] { 700, 40, 8 }.GroupBy(value => value >= 100, (large, values) => (large, total: values.Sum())).ToDictionary(item => item.large, item => item.total); return totals[true] == 700 && totals[false] == 48; }
    public static bool Case0749() { var lookup = new string?[] { null, "x", null }.ToLookup(value => value); return lookup[null].Count() == 2 && lookup["x"].Single() == "x"; }
    public static bool Case0750() { int[] prefix = [7, 5]; int[] suffix = [0]; int[] values = [.. prefix, .. suffix]; return values.SequenceEqual(new[] { 7, 5, 0 }); }

    private sealed record Model(int Id);

    private sealed class ModelCollection : KeyedCollection<int, Model> {
        protected override int GetKeyForItem(Model item) => item.Id;
    }

    private sealed record Box(int Value);
}
