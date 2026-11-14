using System.Reflection;
using System.Runtime.CompilerServices;
using SharpHide.Vendor;

namespace ILTestLand;

[Virtualize]
[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 1800, BogusStates = 6, OpaqueDepth = 4, DecryptHashChance = 0.65)]
internal static class ContractReflectionCases
{
    public static bool Case1551() { return (int)Status.Created == 1; }
    public static bool Case1552() { return Enum.IsDefined(Status.Closed); }
    public static bool Case1553() { return Enum.Parse<Status>("Open") == Status.Open; }
    public static bool Case1554() { return Enum.GetName(Status.Created) == "Created"; }
    public static bool Case1555() { return Enum.GetValues<Status>().Length == 3; }
    public static bool Case1556() { return (Permissions.Read | Permissions.Write).HasFlag(Permissions.Write); }
    public static bool Case1557() { return (Permissions.Read | Permissions.Write).ToString() == "Read, Write"; }
    public static bool Case1558() { return typeof(ContractReflectionCases).IsClass; }
    public static bool Case1559() { return typeof(ContractReflectionCases).IsAbstract && typeof(ContractReflectionCases).IsSealed; }
    public static bool Case1560() { var value = typeof(ContractReflectionCases).GetMethods(BindingFlags.Public | BindingFlags.Static); return value.Any(method => method.Name == nameof(Case1560)); }
    public static bool Case1561() { var value = typeof(ContractReflectionCases).GetMethod(nameof(Case1561))!; return value.IsStatic && value.ReturnType == typeof(bool); }
    public static bool Case1562() { var value = typeof(ContractReflectionCases).GetMethod(nameof(Case1562))!.GetParameters(); return value.Length == 0; }
    public static bool Case1563() { var value = typeof(Pair).GetProperties(); return value.Length == 2; }
    public static bool Case1564() { var value = typeof(Pair).GetProperty(nameof(Pair.Left))!; return value.PropertyType == typeof(int); }
    public static bool Case1565() { var value = typeof(Pair).GetConstructor([typeof(int), typeof(int)]); return value != null; }
    public static bool Case1566() { var value = Activator.CreateInstance(typeof(Pair), 15, 66); return value is Pair pair && pair.Left + pair.Right == 81; }
    public static bool Case1567() { var value = Activator.CreateInstance(typeof(List<int>)); return value is List<int>; }
    public static bool Case1568() { var value = typeof(List<>).MakeGenericType(typeof(int)); return value.GetGenericArguments()[0] == typeof(int); }
    public static bool Case1569() { var value = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(int)); return value.GetGenericArguments().Length == 2; }
    public static bool Case1570() { var value = typeof(List<int>).GetInterfaces(); return value.Contains(typeof(IEnumerable<int>)); }
    public static bool Case1571() { var value = typeof(Pair).GetFields(BindingFlags.Instance | BindingFlags.Public); return value.Length == 0; }
    public static bool Case1572() { var value = typeof(Pair).GetNestedTypes(BindingFlags.NonPublic); return value.Length == 0; }
    public static bool Case1573() { var value = typeof(ContractReflectionCases).Assembly.GetTypes(); return value.Any(type => type == typeof(ContractReflectionCases)); }
    public static bool Case1574() { var value = typeof(ContractReflectionCases).Module; return value.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase); }
    public static bool Case1575() { var value = typeof(ContractReflectionCases).Assembly.GetName().Version; return value != null; }
    public static bool Case1576() { var value = typeof(ContractReflectionCases).GetMethod(nameof(Case1576))!.GetMethodImplementationFlags(); return value == MethodImplAttributes.IL; }
    public static bool Case1577() { var value = typeof(ContractReflectionCases).GetMethod(nameof(Case1577))!.IsDefined(typeof(CompilerGeneratedAttribute)); return !value; }
    public static bool Case1578() { var method = typeof(ContractReflectionCases).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(item => item.ReturnType == typeof(int) && item.GetParameters().Length == 1); var value = Delegate.CreateDelegate(typeof(Func<int, int>), method); return ((Func<int, int>)value)(1578) == 1579; }
    public static bool Case1579() { Func<int, int> value = Add; return value(1578) == 1579; }
    public static bool Case1580() { var value = new Func<int, int>(Add).Method; return value.ReturnType == typeof(int) && value.GetParameters().Length == 1; }
    public static bool Case1581() { var value = new Action<int>(Store); value(1581); return _stored == 1581; }
    public static bool Case1582() { var value = new Predicate<int>(number => number == 1582); return value(1582); }
    public static bool Case1583() { var value = new Comparison<int>((left, right) => left.CompareTo(right)); return value(1, 1583) < 0; }
    public static bool Case1584() { var value = new Converter<int, string>(number => number.ToString()); return value(1584) == "1584"; }
    public static bool Case1585() { var value = new EventHandler((_, _) => _stored = 1585); value(null, EventArgs.Empty); return _stored == 1585; }
    public static bool Case1586() { var value = new MulticastDelegateHolder(); value.Changed += (_, _) => _stored = 1586; value.Raise(); return _stored == 1586; }
    public static bool Case1587() { var value = new GenericHolder<int>(1587); return value.Value == 1587; }
    public static bool Case1588() { var value = new GenericHolder<string>("1588"); return value.Value == "1588"; }
    public static bool Case1589() { var value = new GenericHolder<int>(1589).Map(number => number + 1); return value.Value == 1590; }
    public static bool Case1590() { var value = new GenericHolder<string>("1590").Map(text => text + "!"); return value.Value == "1590!"; }
    public static bool Case1591() { var value = new Pair(15, 91); return value is { Left: 15, Right: 91 }; }
    public static bool Case1592() { object value = new Pair(15, 92); return value is Pair { Right: 92 }; }
    public static bool Case1593() { var value = new object[] { 1593, "x" }; return value switch { [int number, string text] => number == 1593 && text == "x", _ => false }; }
    public static bool Case1594() { var value = new Pair(15, 94); return value switch { (15, 94) => true, _ => false }; }
    public static bool Case1595() { var value = new Pair(15, 95); return value.DeconstructValues() == (15, 95); }
    public static bool Case1596() { var value = new Pair(15, 96); return value with { Right = 1596 } == new Pair(15, 1596); }
    public static bool Case1597() { var value = new[] { new Pair(15, 97), new Pair(1, 2) }; return value.Contains(new Pair(15, 97)); }
    public static bool Case1598() { var value = new Pair(15, 98).GetHashCode(); return value != 0; }
    public static bool Case1599() { var value = new Pair(15, 99).ToString(); return value.Contains("99", StringComparison.Ordinal); }
    public static bool Case1600() { var value = new GenericHolder<Pair>(new Pair(16, 0)); return value.Value.Left + value.Value.Right == 16; }

    private static int _stored;
    private static int Add(int value) => value + 1;
    private static void Store(int value) => _stored = value;
    private enum Status { Created = 1, Open = 2, Closed = 3 }
    [Flags] private enum Permissions { None = 0, Read = 1, Write = 2 }
    private readonly record struct Pair(int Left, int Right)
    {
        public (int Left, int Right) DeconstructValues() => (Left, Right);
    }
    private sealed class MulticastDelegateHolder { public event EventHandler? Changed; public void Raise() => Changed?.Invoke(this, EventArgs.Empty); }
    private sealed class GenericHolder<T>(T value) { public T Value { get; } = value; public GenericHolder<TResult> Map<TResult>(Func<T, TResult> map) => new(map(Value)); }
}
