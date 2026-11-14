using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class CacheStateCases
{
    public static bool Case1451() { var cache = new Dictionary<string, int>(); cache.TryAdd("1451", 1451); return cache["1451"] == 1451; }
    public static bool Case1452() { var cache = new Dictionary<string, int>(); cache["x"] = 1; cache["x"] = 1452; return cache["x"] == 1452; }
    public static bool Case1453() { var cache = new Dictionary<string, int>(); return cache.GetValueOrDefault("missing", 1453) == 1453; }
    public static bool Case1454() { var cache = new Dictionary<string, int> { ["x"] = 1454 }; return cache.Remove("x") && cache.Count == 0; }
    public static bool Case1455() { var cache = new Dictionary<string, int> { ["x"] = 1455 }; return cache.ContainsKey("x") && cache.ContainsValue(1455); }
    public static bool Case1456() { var cache = new OrderedDictionary<string, int> { ["a"] = 1, ["b"] = 1456 }; return cache.Keys.SequenceEqual(new[] { "a", "b" }); }
    public static bool Case1457() { var cache = new ReadOnlyDictionary<string, int>(new Dictionary<string, int> { ["x"] = 1457 }); return cache["x"] == 1457; }
    public static bool Case1458() { var cache = new ReadOnlyCollection<int>([1458]); return cache.Single() == 1458; }
    public static bool Case1459() { var cache = new KeyValuePair<string, int>("x", 1459); return cache.Key == "x" && cache.Value == 1459; }
    public static bool Case1460() { var cache = new Collection<int> { 1460 }; cache.Add(1); return cache[0] == 1460; }
    public static bool Case1461() { var value = new ObservableCollection<int> { 1461 }; var count = 0; value.CollectionChanged += (_, _) => count++; value.Add(1); return count == 1; }
    public static bool Case1462() { var value = new BindingList<int> { 1462 }; var count = 0; value.ListChanged += (_, _) => count++; value.Add(1); return count == 1; }
    public static bool Case1463() { var value = new PropertyChangedValue(); var changed = false; value.PropertyChanged += (_, eventArgs) => changed = eventArgs.PropertyName == nameof(PropertyChangedValue.Value); value.Value = 1463; return changed; }
    public static bool Case1464() { var value = new DataErrorValue(); return value["x"] == "error"; }
    public static bool Case1465() { var value = new VersionedValue(1465); var copy = value with { Number = 1 }; return value.Number == 1465 && copy.Number == 1; }
    public static bool Case1466() { var value = new CacheEntry<int>(1466, true); return value.Value == 1466 && value.Hit; }
    public static bool Case1467() { var value = new CacheEntry<int>(1467, false); return !value.Hit; }
    public static bool Case1468() { var value = new ExpiringValue(1468, DateTimeOffset.UtcNow.AddMinutes(1)); return !value.IsExpired; }
    public static bool Case1469() { var value = new ExpiringValue(1469, DateTimeOffset.UtcNow.AddMinutes(-1)); return value.IsExpired; }
    public static bool Case1470() { var value = new LruCache<int, string>(2); value.Set(1, "a"); value.Set(2, "b"); value.Get(1); value.Set(3, "c"); return value.Contains(3); }
    public static bool Case1471() { var value = new LruCache<int, string>(1); value.Set(1471, "x"); return value.Get(1471) == "x"; }
    public static bool Case1472() { var value = new Counter(); value.Increment(); value.Increment(1472); return value.Value == 1473; }
    public static bool Case1473() { var value = new Counter(); value.Decrement(); value.Decrement(1473); return value.Value == -1474; }
    public static bool Case1474() { var value = new StateMachine(); return value.Step() == "one" && value.Step() == "two" && value.Step() == "done"; }
    public static bool Case1475() { var value = new StateMachine(); value.Step(); value.Step(); return value.Step() == "done"; }
    public static bool Case1476() { var value = new RetryState(3); return value.Next() && value.Next() && value.Next() && !value.Next(); }
    public static bool Case1477() { var value = new RetryState(1477); return value.Remaining == 1477; }
    public static bool Case1478() { var value = new ToggleState(); return value.Next() && !value.Next() && value.Next(); }
    public static bool Case1479() { var value = new ToggleState(); return value.Value == false; }
    public static bool Case1480() { var value = new RingBuffer<int>(3); value.Add(1); value.Add(2); value.Add(1480); return value.ToArray().SequenceEqual(new[] { 1, 2, 1480 }); }
    public static bool Case1481() { var value = new RingBuffer<int>(2); value.Add(1); value.Add(2); value.Add(1481); return value.ToArray().SequenceEqual(new[] { 2, 1481 }); }
    public static bool Case1482() { var value = new Batch<int>(2); value.Add(1); return !value.IsFull; }
    public static bool Case1483() { var value = new Batch<int>(2); value.Add(1); value.Add(1483); return value.IsFull; }
    public static bool Case1484() { var value = new Batch<int>(2); value.Add(1); value.Add(2); value.Add(1484); return value.Count == 2; }
    public static bool Case1485() { var value = new IdGenerator(1485); return value.Next() == 1485 && value.Next() == 1486; }
    public static bool Case1486() { var value = new IdGenerator(1); return value.Next() == 1 && value.Next() == 2; }
    public static bool Case1487() { var value = new VersionStamp(); return value.Next() == 1 && value.Next() == 2; }
    public static bool Case1488() { var value = new VersionStamp(); value.Reset(); return value.Next() == 1; }
    public static bool Case1489() { var value = new Snapshot<int>(1489); return value.Value == 1489; }
    public static bool Case1490() { var value = new Snapshot<int>(1490); var copy = value.Copy(); return copy.Value == 1490 && !ReferenceEquals(value, copy); }
    public static bool Case1491() { var value = new AggregateState(); value.Apply(1); value.Apply(1490); return value.Total == 1491; }
    public static bool Case1492() { var value = new AggregateState(); value.Apply(1492); value.Rollback(2); return value.Total == 1490; }
    public static bool Case1493() { var value = new AggregateState(); value.Apply(1493); var snapshot = value.Save(); value.Apply(1); value.Restore(snapshot); return value.Total == 1493; }
    public static bool Case1494() { var value = new AggregateState(); value.Apply(1494); return value.Save().Total == 1494; }
    public static bool Case1495() { var value = new NullableState<int>(1495); return value.HasValue && value.Value == 1495; }
    public static bool Case1496() { var value = new NullableState<int>(); return !value.HasValue; }
    public static bool Case1497() { var value = new Result<int>(1497); return value.IsSuccess && value.Value == 1497; }
    public static bool Case1498() { var value = Result<int>.Failure("1498"); return !value.IsSuccess && value.Error == "1498"; }
    public static bool Case1499() { var value = new Either<int, string>(1499); return value.Left == 1499; }
    public static bool Case1500() { var value = new Either<int, string>("1500"); return value.Right == "1500"; }

    private sealed record VersionedValue(int Number);
    private readonly record struct CacheEntry<T>(T Value, bool Hit);
    private sealed record ExpiringValue(int Value, DateTimeOffset Expires) { public bool IsExpired => Expires <= DateTimeOffset.UtcNow; }
    private sealed class PropertyChangedValue : INotifyPropertyChanged { private int _value; public event PropertyChangedEventHandler? PropertyChanged; public int Value { get => _value; set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } } }
    private sealed class DataErrorValue : IDataErrorInfo { public string this[string columnName] => "error"; public string Error => "error"; }
    private sealed class Counter { public int Value { get; private set; } public void Increment(int amount = 1) => Value += amount; public void Decrement(int amount = 1) => Value -= amount; }
    private sealed class StateMachine { private int _state; public string Step() => ++_state switch { 1 => "one", 2 => "two", _ => "done" }; }
    private sealed class RetryState(int count) { public int Remaining { get; private set; } = count; public bool Next() => Remaining-- > 0; }
    private sealed class ToggleState { public bool Value { get; private set; } public bool Next() => Value = !Value; }
    private sealed class LruCache<TKey, TValue>(int capacity) where TKey : notnull { private readonly Dictionary<TKey, TValue> _values = []; public void Set(TKey key, TValue value) { if (_values.Count >= capacity) _values.Remove(_values.Keys.First()); _values[key] = value; } public TValue? Get(TKey key) => _values.GetValueOrDefault(key); public bool Contains(TKey key) => _values.ContainsKey(key); }
    private sealed class RingBuffer<T>(int capacity) { private readonly Queue<T> _values = []; public void Add(T value) { if (_values.Count == capacity) _values.Dequeue(); _values.Enqueue(value); } public T[] ToArray() => _values.ToArray(); }
    private sealed class Batch<T>(int capacity) { private readonly List<T> _values = []; public int Count => _values.Count; public bool IsFull => Count >= capacity; public void Add(T value) { if (!IsFull) _values.Add(value); } }
    private sealed class IdGenerator(int start) { private int _next = start; public int Next() => _next++; }
    private sealed class VersionStamp { private int _value; public int Next() => ++_value; public void Reset() => _value = 0; }
    private sealed record Snapshot<T>(T Value) { public Snapshot<T> Copy() => this with { }; }
    private sealed class AggregateState { public int Total { get; private set; } public void Apply(int value) => Total += value; public void Rollback(int value) => Total -= value; public AggregateState Save() => new() { Total = Total }; public void Restore(AggregateState state) => Total = state.Total; }
    private readonly struct NullableState<T>
    {
        public bool HasValue { get; }
        public T Value { get; }
        public NullableState(T value) { HasValue = true; Value = value; }
        public NullableState() { HasValue = false; Value = default!; }
    }
    private readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string? Error { get; }
        public Result(T value) { Value = value; IsSuccess = true; Error = null; }
        private Result(string error) { Value = default!; IsSuccess = false; Error = error; }
        public static Result<T> Failure(string error) => new(error);
    }
    private readonly struct Either<TLeft, TRight> { public TLeft? Left { get; } public TRight? Right { get; } public Either(TLeft value) { Left = value; } public Either(TRight value) { Right = value; } }
}
