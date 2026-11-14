using System.Collections;
using System.Numerics;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1500, BogusStates = 6, OpaqueDepth = 4,
    DecryptHashChance = 0.7)]
internal static class ObjectModelCases {
    public static bool Case0951() { Animal animal = new Dog(951); return animal.Speak() == "dog-951"; }
    public static bool Case0952() { Shape shape = new Rectangle(28, 34); return shape.Area == 952; }
    public static bool Case0953() { IDefaultValue value = new DefaultValue(); return value.Read() == 953; }
    public static bool Case0954() { IExplicitValue value = new ExplicitValue(); return value.Read() == 954 && ((ExplicitValue)value).Read() == -1; }
    public static bool Case0955() { IEnumerable<string> strings = new List<string> { "955" }; IEnumerable<object> objects = strings; return (string)objects.Single() == "955"; }
    public static bool Case0956() { IComparer<object> objects = Comparer<object>.Create((left, right) => string.CompareOrdinal(left.ToString(), right.ToString())); IComparer<string> strings = objects; return strings.Compare("956", "957") < 0; }
    public static bool Case0957() { var value = Create<Box>(); value.Value = 957; return value.Value == 957; }
    public static bool Case0958() { return ReadClass(new Box { Value = 958 }) == 958; }
    public static bool Case0959() { return ReadStruct(new Point(900, 59)) == 959; }
    public static bool Case0960() { return AddGeneric(900, 60) == 960 && AddGeneric(900L, 60L) == 960L; }
    public static bool Case0961() { var repository = new Repository<int>(); repository.Add(900); repository.Add(61); return repository.Items.Sum() == 961; }
    public static bool Case0962() { var grid = new Grid(2, 2); grid[1, 0] = 962; return grid[1, 0] == 962 && grid[0, 0] == 0; }
    public static bool Case0963() { var source = new EventSource(); var total = 0; source.Changed += value => total += value; source.Raise(900); source.Raise(63); return total == 963; }
    public static bool Case0964() { var value = new InitModel { Id = 964, Name = "case" }; return value.Id == 964 && value.Name == "case"; }
    public static bool Case0965() { var value = new RequiredModel { Id = 965, Name = "required" }; return value.Id == 965 && value.Name.Length == 8; }
    public static bool Case0966() { var first = new Person("Ada", 966); var second = new Person("Ada", 966); return first == second && first.GetHashCode() == second.GetHashCode(); }
    public static bool Case0967() { var original = new Person("Ada", 900); var changed = original with { Age = 67 }; return original.Age == 900 && changed.Age == 67 && changed.Name == "Ada"; }
    public static bool Case0968() { var value = new Measurement(900, 68); var copy = value with { Minor = 69 }; return value.Total == 968 && copy.Total == 969; }
    public static bool Case0969() { var value = new ReadonlyPoint(900, 69); return value.Sum == 969; }
    public static bool Case0970() { var result = new Amount(900) + new Amount(70); return result.Value == 970; }
    public static bool Case0971() { Amount value = 971; int result = value; return result == 971; }
    public static bool Case0972() { var value = (Amount)972.9; return value.Value == 972; }
    public static bool Case0973() { var point = new Point(900, 73); var (x, y) = point; return x + y == 973; }
    public static bool Case0974() { var tuple = (Id: 974, Name: "case"); return tuple.Id == 974 && tuple.Name == "case"; }
    public static bool Case0975() { var first = new Identity(975); var second = new Identity(975); return first.Equals(second) && !first.Equals(new Identity(0)); }
    public static bool Case0976() { var values = new[] { new Sortable(76), new Sortable(900), new Sortable(7) }; Array.Sort(values); return values[^1].Value == 900 && values[1].Value == 76; }
    public static bool Case0977() { var values = new[] { "a", "cccc", "bb" }; Array.Sort(values, new LengthComparer()); return values.SequenceEqual(new[] { "a", "bb", "cccc" }); }
    public static bool Case0978() { var values = new RangeCollection(3).ToArray(); return values.SequenceEqual(new[] { 0, 1, 2 }); }
    public static bool Case0979() { return YieldSquares(5).SequenceEqual(new[] { 0, 1, 4, 9, 16 }); }
    public static bool Case0980() { using var enumerator = YieldSquares(3).GetEnumerator(); return enumerator.MoveNext() && enumerator.Current == 0 && enumerator.MoveNext() && enumerator.Current == 1; }
    public static bool Case0981() { return 981.TimesTwo() == 1962; }
    public static bool Case0982() { var value = new Outer(900).CreateInner(82); return value.Sum == 982; }
    public static bool Case0983() { return StaticValue<StaticNumber>() == 983; }
    public static bool Case0984() { return CombineStatic<StaticNumber>(900, 84) == 984; }
    public static bool Case0985() { Result result = new Success(985); return result switch { Success { Value: 985 } => true, Failure => false, _ => false }; }
    public static bool Case0986() { Node node = new AddNode(new ValueNode(900), new ValueNode(86)); return node.Evaluate() == 986; }
    public static bool Case0987() { IText text = new BracketText(new PlainText("987")); return text.Render() == "[987]"; }
    public static bool Case0988() { var calculator = new Calculator((left, right) => left + right); return calculator.Execute(900, 88) == 988; }
    public static bool Case0989() { var state = 900; ICommand command = new AddCommand(89); command.Execute(ref state); return state == 989; }
    public static bool Case0990() { var subject = new Subject(); var observer = new Observer(); subject.Subscribe(observer); subject.Publish(990); return observer.Last == 990; }
    public static bool Case0991() { var factory = new ValueFactory(); return factory.Create("int", 991) is int value && value == 991; }
    public static bool Case0992() { var resource = new DerivedResource(); resource.Dispose(); return resource.BaseDisposed && resource.DerivedDisposed; }
    public static bool Case0993() { var resource = new AsyncResource(); resource.DisposeAsync().AsTask().GetAwaiter().GetResult(); return resource.Disposed; }
    public static bool Case0994() { var original = new CloneModel { Value = 994 }; var copy = (CloneModel)original.Clone(); copy.Value = 1; return original.Value == 994 && copy.Value == 1; }
    public static bool Case0995() { var original = new CopyModel(995); var copy = original.Copy(); return !ReferenceEquals(original, copy) && copy.Value == 995; }
    public static bool Case0996() { var values = new[] { 900, 96 }; ref var item = ref Find(values, 96); item = 996; return values[1] == 996; }
    public static bool Case0997() { Span<int> values = stackalloc[] { 900, 90, 7 }; var view = new StackView(values); return view.Sum() == 997; }
    public static bool Case0998() { var value = 998; ref readonly var alias = ref ReadOnlyAlias(in value); return alias == 998; }
    public static bool Case0999() { var values = new[] { 900, 90, 9 }; return SumScoped(values) == 999; }
    public static bool Case1000() { var value = new GenericOuter<int>.GenericInner<string>(1000, "case"); return value.First == 1000 && value.Second == "case"; }

    private static T Create<T>() where T : new() => new();
    private static int ReadClass<T>(T value) where T : Box => value.Value;
    private static int ReadStruct<T>(T value) where T : struct, IPoint => value.X + value.Y;
    private static T AddGeneric<T>(T left, T right) where T : INumber<T> => left + right;
    private static int StaticValue<T>() where T : IStaticNumber<T> => T.Value;
    private static int CombineStatic<T>(int left, int right) where T : IStaticNumber<T> => T.Combine(left, right);
    private static IEnumerable<int> YieldSquares(int count) { for (var value = 0; value < count; value++) yield return value * value; }
    private static ref int Find(int[] values, int expected) { for (var index = 0; index < values.Length; index++) if (values[index] == expected) return ref values[index]; throw new InvalidOperationException(); }
    private static ref readonly int ReadOnlyAlias(in int value) => ref value;
    private static int SumScoped(scoped ReadOnlySpan<int> values) { var total = 0; foreach (var value in values) total += value; return total; }

    private abstract class Animal(int id) { protected int Id { get; } = id; public abstract string Speak(); }
    private sealed class Dog(int id) : Animal(id) { public override string Speak() => "dog-" + Id; }
    private abstract class Shape { public abstract int Area { get; } }
    private sealed class Rectangle(int width, int height) : Shape { public override int Area => width * height; }
    private interface IDefaultValue { int Read() => 953; }
    private sealed class DefaultValue : IDefaultValue;
    private interface IExplicitValue { int Read(); }
    private sealed class ExplicitValue : IExplicitValue { int IExplicitValue.Read() => 954; public int Read() => -1; }
    private class Box { public int Value { get; set; } }
    private interface IPoint { int X { get; } int Y { get; } }
    private readonly record struct Point(int X, int Y) : IPoint;
    private sealed class Repository<T> { public List<T> Items { get; } = []; public void Add(T value) => Items.Add(value); }
    private sealed class Grid(int width, int height) { private readonly int[,] _values = new int[width, height]; public int this[int x, int y] { get => _values[x, y]; set => _values[x, y] = value; } }
    private sealed class EventSource { public event Action<int>? Changed; public void Raise(int value) => Changed?.Invoke(value); }
    private sealed class InitModel { public int Id { get; init; } public string Name { get; init; } = ""; }
    private sealed class RequiredModel { public required int Id { get; init; } public required string Name { get; init; } }
    private sealed record Person(string Name, int Age);
    private readonly record struct Measurement(int Major, int Minor) { public int Total => Major + Minor; }
    private readonly struct ReadonlyPoint(int x, int y) { public int Sum => x + y; }
    private readonly record struct Amount(int Value) { public static Amount operator +(Amount left, Amount right) => new(left.Value + right.Value); public static implicit operator Amount(int value) => new(value); public static implicit operator int(Amount value) => value.Value; public static explicit operator Amount(double value) => new((int)value); }
    private readonly record struct Identity(int Value);
    private sealed record Sortable(int Value) : IComparable<Sortable> { public int CompareTo(Sortable? other) => Value.CompareTo(other?.Value); }
    private sealed class LengthComparer : IComparer<string> { public int Compare(string? left, string? right) => (left?.Length ?? 0).CompareTo(right?.Length ?? 0); }
    private sealed class RangeCollection(int count) : IEnumerable<int> { public IEnumerator<int> GetEnumerator() { for (var value = 0; value < count; value++) yield return value; } IEnumerator IEnumerable.GetEnumerator() => GetEnumerator(); }
    private static int TimesTwo(this int value) => value * 2;
    private sealed class Outer(int value) { public Inner CreateInner(int second) => new(value, second); public sealed record Inner(int First, int Second) { public int Sum => First + Second; } }
    private interface IStaticNumber<TSelf> where TSelf : IStaticNumber<TSelf> { static abstract int Value { get; } static abstract int Combine(int left, int right); }
    private sealed class StaticNumber : IStaticNumber<StaticNumber> { public static int Value => 983; public static int Combine(int left, int right) => left + right; }
    private abstract record Result;
    private sealed record Success(int Value) : Result;
    private sealed record Failure(string Error) : Result;
    private abstract record Node { public abstract int Evaluate(); }
    private sealed record ValueNode(int Value) : Node { public override int Evaluate() => Value; }
    private sealed record AddNode(Node Left, Node Right) : Node { public override int Evaluate() => Left.Evaluate() + Right.Evaluate(); }
    private interface IText { string Render(); }
    private sealed record PlainText(string Value) : IText { public string Render() => Value; }
    private sealed record BracketText(IText Inner) : IText { public string Render() => "[" + Inner.Render() + "]"; }
    private sealed class Calculator(Func<int, int, int> operation) { public int Execute(int left, int right) => operation(left, right); }
    private interface ICommand { void Execute(ref int value); }
    private sealed record AddCommand(int Amount) : ICommand { public void Execute(ref int value) => value += Amount; }
    private interface IObserver { void Update(int value); }
    private sealed class Observer : IObserver { public int Last { get; private set; } public void Update(int value) => Last = value; }
    private sealed class Subject { private readonly List<IObserver> _observers = []; public void Subscribe(IObserver observer) => _observers.Add(observer); public void Publish(int value) { foreach (var observer in _observers) observer.Update(value); } }
    private sealed class ValueFactory { public object Create(string kind, int value) => kind == "int" ? value : value.ToString(); }
    private class BaseResource : IDisposable { public bool BaseDisposed { get; private set; } public virtual void Dispose() => BaseDisposed = true; }
    private sealed class DerivedResource : BaseResource { public bool DerivedDisposed { get; private set; } public override void Dispose() { DerivedDisposed = true; base.Dispose(); } }
    private sealed class AsyncResource : IAsyncDisposable { public bool Disposed { get; private set; } public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; } }
    private sealed class CloneModel : ICloneable { public int Value { get; set; } public object Clone() => MemberwiseClone(); }
    private sealed class CopyModel(int value) { public int Value { get; } = value; public CopyModel Copy() => (CopyModel)MemberwiseClone(); }
    private readonly ref struct StackView
    {
        private readonly ReadOnlySpan<int> _values;

        public StackView(ReadOnlySpan<int> values)
        {
            _values = values;
        }

        public int Sum()
        {
            var total = 0;
            foreach (var value in _values)
            {
                total += value;
            }

            return total;
        }
    }
    private sealed class GenericOuter<T> { public sealed record GenericInner<TSecond>(T First, TSecond Second); }
}
