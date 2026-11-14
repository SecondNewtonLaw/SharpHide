using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Immutable;

internal static class LanguageSemanticsCases
{
    public static bool Case2851() { int? value = 2851; return value.HasValue && value.Value == 2851; }
    public static bool Case2852() { int? value = null; return value.GetValueOrDefault(2852) == 2852; }
    public static bool Case2853() { return ((int?)2853 ?? 0) == 2853; }
    public static bool Case2854() { return ((int?)null ?? 2854) == 2854; }
    public static bool Case2855() { var value = (left: 28, right: 55); return value.left + value.right == 83; }
    public static bool Case2856() { var value = (28, 56); return value.Item1 * 100 + value.Item2 == 2856; }
    public static bool Case2857() { var (left, right) = (28, 57); return left + right == 85; }
    public static bool Case2858() { var value = new KeyValuePair<int, string>(2858, "x"); return value.Key == 2858; }
    public static bool Case2859() { var value = new ValueTuple<int, int>(28, 59); return value.Item1 + value.Item2 == 87; }
    public static bool Case2860() { return EqualityComparer<int>.Default.Equals(2860, 2860); }
    public static bool Case2861() { return Comparer<int>.Default.Compare(2861, 2860) > 0; }
    public static bool Case2862() { return ReferenceEquals(null, null); }
    public static bool Case2863() { var value = new object(); return ReferenceEquals(value, value); }
    public static bool Case2864() { return object.Equals(2864, 2864); }
    public static bool Case2865() { return object.ReferenceEquals("x", "x"); }
    public static bool Case2866() { return new[] { 1, 2, 3 } is { Length: 3 }; }
    public static bool Case2867() { object value = 2867; return value is int number && number == 2867; }
    public static bool Case2868() { object value = "2868"; return value is string { Length: 4 }; }
    public static bool Case2869() { return 2869 switch { > 0 => true, _ => false }; }
    public static bool Case2870() { return 2870 switch { 2870 => true, _ => false }; }
    public static bool Case2871() { var value = new[] { 2, 8, 7, 1 }; return value switch { [2, 8, 7, 1] => true, _ => false }; }
    public static bool Case2872() { var value = new[] { 2, 8, 7, 2 }; return value is [2, .., 2]; }
    public static bool Case2873() { var value = new Dictionary<string, int> { ["x"] = 2873 }; return value is { Count: 1 } && value["x"] == 2873; }
    public static bool Case2874() { var value = (x: 28, y: 74); return value is { x: 28, y: 74 }; }
    public static bool Case2875() { return new[] { 1, 2, 3 } is IEnumerable<int>; }
    public static bool Case2876() { return typeof(int).IsAssignableTo(typeof(object)); }
    public static bool Case2877() { return typeof(object).IsAssignableFrom(typeof(string)); }
    public static bool Case2878() { return typeof(int).IsEquivalentTo(typeof(int)); }
    public static bool Case2879() { return typeof(int).IsValueType; }
    public static bool Case2880() { return typeof(string).IsClass; }
    public static bool Case2881() { return new List<int> { 1, 2 }.AsReadOnly().Count == 2; }
    public static bool Case2882() { var value = new ReadOnlyCollection<int>(new[] { 2, 8, 8, 2 }); return value[2] == 8; }
    public static bool Case2883() { var value = new ObservableCollection<int> { 2883 }; return value.Count == 1; }
    public static bool Case2884() { var value = new Collection<int> { 2884 }; return value[0] == 2884; }
    public static bool Case2885() { var value = new Queue<int>(); value.Enqueue(2885); return value.TryDequeue(out var item) && item == 2885; }
    public static bool Case2886() { var value = new Stack<int>(); value.Push(2886); return value.TryPop(out var item) && item == 2886; }
    public static bool Case2887() { var value = new LinkedList<int>(); value.AddFirst(2887); return value.First!.Value == 2887; }
    public static bool Case2888() { var value = new LinkedList<int>(); value.AddLast(2888); return value.Last!.Value == 2888; }
    public static bool Case2889() { var value = new HashSet<int> { 2889 }; return value.Remove(2889) && value.Count == 0; }
    public static bool Case2890() { var value = new SortedSet<int> { 3, 1, 2 }; return value.Min == 1 && value.Max == 3; }
    public static bool Case2891() { var value = new Dictionary<int, int> { [2891] = 1 }; return value.Remove(2891) && value.Count == 0; }
    public static bool Case2892() { var value = new ConcurrentDictionary<int, int>(); return value.TryAdd(2892, 2) && value[2892] == 2; }
    public static bool Case2893() { var value = new WeakReference(new object()); return value.IsAlive; }
    public static bool Case2894() { var value = new WeakReference<string>("2894"); return value.TryGetTarget(out var item) && item == "2894"; }
    public static bool Case2895() { return new Lazy<int>(() => 2895).Value == 2895; }
    public static bool Case2896() { return new ThreadLocal<int>(() => 2896).Value == 2896; }
    public static bool Case2897() { return new AsyncLocal<int> { Value = 2897 }.Value == 2897; }
    public static bool Case2898() { return new Tuple<int, int>(28, 98).Item1 == 28; }
    public static bool Case2899() { return new Tuple<string, int>("2899", 1).Item1 == "2899"; }
    public static bool Case2900() { var value = ImmutableArray.CreateBuilder<int>(); value.Add(2900); return value.ToImmutable().Single() == 2900; }
}
