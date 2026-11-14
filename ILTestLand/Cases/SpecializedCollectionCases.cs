using System.Collections;
using System.Collections.Specialized;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class SpecializedCollectionCases
{
    public static bool SpecializedCollection_1951() { var value = new BitArray(8); value.Set(1, true); return value[1] && !value[0]; }
    public static bool SpecializedCollection_1952() { var value = new BitArray([true, false, true]); value.And(new BitArray([true, true, false])); return value[0] && !value[1] && !value[2]; }
    public static bool SpecializedCollection_1953() { var value = new BitArray([true, false, true]); value.Or(new BitArray([false, true, false])); return value.Cast<bool>().All(item => item); }
    public static bool SpecializedCollection_1954() { var value = new BitArray([true, false, true]); value.Xor(new BitArray([true, true, false])); return !value[0] && value[1] && value[2]; }
    public static bool SpecializedCollection_1955() { var value = new BitArray([true, false, true]); value.Not(); return !value[0] && value[1] && !value[2]; }
    public static bool SpecializedCollection_1956() { var value = new BitArray(1956); return value.Length == 1956; }
    public static bool SpecializedCollection_1957() { var value = new BitArray([true, false]); value.Length = 3; return value.Length == 3 && !value[2]; }
    public static bool SpecializedCollection_1958() { var value = new BitArray([true, false]); var copy = (BitArray)value.Clone(); return copy[0] && !copy[1]; }
    public static bool SpecializedCollection_1959() { var value = new BitArray([true, false]); return value.Get(0) && !value.Get(1); }
    public static bool SpecializedCollection_1960() { var value = new BitArray([true, false]); value.SetAll(true); return value.Cast<bool>().All(item => item); }
    public static bool SpecializedCollection_1961() { var value = new NameValueCollection(); value.Add("x", "1961"); return value["x"] == "1961"; }
    public static bool SpecializedCollection_1962() { var value = new NameValueCollection(); value.Add("x", "a"); value.Add("x", "b"); return value["x"] == "a,b"; }
    public static bool SpecializedCollection_1963() { var value = new NameValueCollection(StringComparer.OrdinalIgnoreCase) { ["X"] = "1963" }; return value["x"] == "1963"; }
    public static bool SpecializedCollection_1964() { var value = new NameValueCollection { ["x"] = "1964" }; return value.AllKeys.Single() == "x"; }
    public static bool SpecializedCollection_1965() { var value = new NameValueCollection { ["x"] = "1965" }; value.Remove("x"); return value.Count == 0; }
    public static bool SpecializedCollection_1966() { var value = new NameValueCollection { ["x"] = "1966" }; var copy = new NameValueCollection(value); return copy["x"] == "1966"; }
    public static bool SpecializedCollection_1967() { var value = new NameValueCollection { ["x"] = "1967" }; return value.GetKey(0) == "x"; }
    public static bool SpecializedCollection_1968() { var value = new NameValueCollection { ["x"] = "1968" }; return value.GetValues(0)!.Single() == "1968"; }
    public static bool SpecializedCollection_1969() { var value = new NameValueCollection { ["x"] = "1969" }; value.Set("x", "y"); return value["x"] == "y"; }
    public static bool SpecializedCollection_1970() { var value = new NameValueCollection { ["x"] = "1970" }; value.Clear(); return value.Count == 0; }
    public static bool SpecializedCollection_1971() { var value = new OrderedDictionary(); value.Add("a", 1971); return (int)value["a"]! == 1971; }
    public static bool SpecializedCollection_1972() { var value = new OrderedDictionary { ["a"] = 1, ["b"] = 1972 }; return value.Keys.Cast<object>().Last()!.ToString() == "b"; }
    public static bool SpecializedCollection_1973() { var value = new OrderedDictionary { ["a"] = 1973 }; value.Insert(0, "z", 1); return value[0]!.Equals(1); }
    public static bool SpecializedCollection_1974() { var value = new OrderedDictionary { ["a"] = 1974 }; value.RemoveAt(0); return value.Count == 0; }
    public static bool SpecializedCollection_1975() { var value = new OrderedDictionary { ["a"] = 1975 }; return value.Contains("a"); }
    public static bool SpecializedCollection_1976() { var value = new OrderedDictionary { ["a"] = 1976 }; var copy = new OrderedDictionary { ["a"] = value["a"] }; return (int)copy["a"]! == 1976; }
    public static bool SpecializedCollection_1977() { var value = new HybridDictionary { ["a"] = 1977 }; return (int)value["a"]! == 1977; }
    public static bool SpecializedCollection_1978() { var value = new ListDictionary { ["a"] = 1978 }; return (int)value["a"]! == 1978; }
    public static bool SpecializedCollection_1979() { var value = new Hashtable { ["a"] = 1979 }; return (int)value["a"]! == 1979; }
    public static bool SpecializedCollection_1980() { var value = new Hashtable(StringComparer.OrdinalIgnoreCase) { ["A"] = 1980 }; return (int)value["a"]! == 1980; }
    public static bool SpecializedCollection_1981() { var value = new Queue(); value.Enqueue(1981); return value.Dequeue() is 1981; }
    public static bool SpecializedCollection_1982() { var value = new Stack(); value.Push(1982); return value.Pop() is 1982; }
    public static bool SpecializedCollection_1983() { var value = new ArrayList { 1, 1983 }; return (int)value[1]! == 1983; }
    public static bool SpecializedCollection_1984() { var value = new ArrayList { 1, 2, 3 }; value.Reverse(); return (int)value[0]! == 3; }
    public static bool SpecializedCollection_1985() { var value = new ArrayList { 3, 1, 2 }; value.Sort(); return (int)value[0]! == 1; }
    public static bool SpecializedCollection_1986() { var value = new ArrayList { 1, 2, 3 }; return value.Contains(2); }
    public static bool SpecializedCollection_1987() { var value = new ArrayList { 1, 2, 3 }; value.Remove(2); return value.Count == 2; }
    public static bool SpecializedCollection_1988() { var value = new ArrayList { 1, 2, 3 }; value.Insert(1, 1988); return (int)value[1]! == 1988; }
    public static bool SpecializedCollection_1989() { var value = new ArrayList { 1, 2, 3 }; return value.IndexOf(3) == 2; }
    public static bool SpecializedCollection_1990() { var value = new ArrayList { 1, 2, 3 }; var copy = value.Clone() as ArrayList; return copy!.Count == 3; }
    public static bool SpecializedCollection_1991() { var value = new CollectionBaseImpl(); value.Add(1991); return value.Count == 1 && value[0] == 1991; }
    public static bool SpecializedCollection_1992() { var value = new DictionaryBaseImpl(); value.Set("x", 1992); return value.Get("x") == 1992; }
    public static bool SpecializedCollection_1993() { var value = new ReadOnlyDictionaryImpl(new Dictionary<string, int> { ["x"] = 1993 }); return value["x"] == 1993; }
    public static bool SpecializedCollection_1994() { var value = new StringDictionary { ["x"] = "1994" }; return value["x"] == "1994"; }
    public static bool SpecializedCollection_1995() { var value = new StringCollection { "1995" }; return value[0] == "1995"; }
    public static bool SpecializedCollection_1996() { var value = new StringEnumeratorImpl(new[] { "1996" }); return value.MoveNext() && value.Current == "1996"; }
    public static bool SpecializedCollection_1997() { var value = new BitArray([true, true, false]); return value.Cast<bool>().Count(item => item) == 2; }
    public static bool SpecializedCollection_1998() { var value = new Hashtable { [1998] = "x" }; return value.ContainsKey(1998); }
    public static bool SpecializedCollection_1999() { var value = new Queue(); value.Enqueue(1999); return value.Count == 1 && value.Peek() is 1999; }
    public static bool SpecializedCollection_2000() { var value = new Stack(); value.Push(2000); return value.Count == 1 && value.Peek() is 2000; }

    private sealed class CollectionBaseImpl : CollectionBase { public void Add(int value) => List.Add(value); public int this[int index] => (int)List[index]!; }
    private sealed class DictionaryBaseImpl : DictionaryBase { public void Set(string key, int value) => Dictionary[key] = value; public int Get(string key) => (int)Dictionary[key]!; }
    private sealed class ReadOnlyDictionaryImpl(Dictionary<string, int> values) : IReadOnlyDictionary<string, int> { public int this[string key] => values[key]; public IEnumerable<string> Keys => values.Keys; public IEnumerable<int> Values => values.Values; public int Count => values.Count; public bool ContainsKey(string key) => values.ContainsKey(key); public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => values.GetEnumerator(); public bool TryGetValue(string key, out int value) => values.TryGetValue(key, out value); IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    private sealed class StringEnumeratorImpl(IEnumerable<string> values) : IEnumerator<string> { private readonly IEnumerator<string> _inner = values.GetEnumerator(); public string Current => _inner.Current; object IEnumerator.Current => Current; public bool MoveNext() => _inner.MoveNext(); public void Reset() => _inner.Reset(); public void Dispose() => _inner.Dispose(); }
}
