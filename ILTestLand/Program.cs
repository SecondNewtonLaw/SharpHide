using System.Globalization;
using System.Text;

namespace ILTestLand;

/*
 *  Welcome to the shitload of tests created to test random shit to make sure nothing breaks.
 *
 *  Without this junk exceptions would not exist when calls are hidden and whatnot, very important to keep this stuff and update it with new conditions when they crop up.
 *     — SecondNewtonLaw
 */

internal static class Program {
    private static int _failures;
    private static int _checks;

    private static int Main() {
        Section("strings");
        Check("str.concat", Concat("obf", "uscate"), "obfuscated");
        Check("str.manyliterals", string.Concat("a", "b", "c", "d", "e", "f"), "abcdef");
        Check("str.interpolation", Greet("world"), "hello, world!");
        Check("str.switch", Weekday("wed"), 3);
        Check("str.switch.default", Weekday("nope"), 0);
        Check("str.upper", "SharpHide".ToUpperInvariant(), "SHARPHIDE");
        Check("str.lower", "SharpHide".ToLowerInvariant(), "sharphide");
        Check("str.substring", "obfuscation".Substring(3, 4), "usca");
        Check("str.replace", "a.b.c".Replace('.', '/'), "a/b/c");
        Check("str.split", "1,2,3,4".Split(',').Length, 4);
        Check("str.join", string.Join("-", "x", "y", "z"), "x-y-z");
        Check("str.contains", "SharpHide".Contains("Hide"), true);
        Check("str.startswith", "SharpHide".StartsWith("Sharp"), true);
        Check("str.endswith", "SharpHide".EndsWith("Hide"), true);
        Check("str.indexof", "abcdef".IndexOf('d'), 3);
        Check("str.trim", "  pad  ".Trim(), "pad");
        Check("str.padleft", "7".PadLeft(3, '0'), "007");
        Check("str.reverse", new string("stressed".Reverse().ToArray()), "desserts");
        Check("str.format", string.Format("{0}-{1:D3}", "id", 5), "id-005");
        Check("str.interp.align", $"[{42,5}]", "[   42]");
        Check("str.compareordinal", Math.Sign(string.CompareOrdinal("a", "b")), -1);
        Check("str.isnullorempty", string.IsNullOrEmpty(""), true);
        Check("str.equals.ordinal", "Foo".Equals("Foo", StringComparison.Ordinal), true);
        Check("str.tochararray", "abc".ToCharArray().Length, 3);
        Check("str.builder", BuildRepeat("ab", 3), "ababab");
        Check("str.length.unicode", "héllo🦊".Length, 7);

        Section("chars");
        Check("char.isdigit", char.IsDigit('7'), true);
        Check("char.toupper", char.ToUpperInvariant('a'), 'A');
        Check("char.tolower", char.ToLowerInvariant('Z'), 'z');
        Check("char.arith", (char)('A' + 2), 'C');

        Section("numbers");
        Check("num.arithmetic", 6 * 7 + 100, 142);
        Check("num.bigexpr", 123 * 456 - 789 + 1000, 56299);
        Check("num.long", 9_000_000_000L + 1, 9_000_000_001L);
        Check("num.long.max", long.MaxValue, 9223372036854775807L);
        Check("num.hex", 0xFF + 0x01, 256);
        Check("num.binary", 0b1010, 10);
        Check("num.uint", 4_000_000_000u + 1u, 4_000_000_001u);
        Check("num.shift.left", 1 << 10, 1024);
        Check("num.shift.right", 1024 >> 3, 128);
        Check("num.and", 0b1100 & 0b1010, 0b1000);
        Check("num.or", 0b1100 | 0b1010, 0b1110);
        Check("num.xor", 0b1100 ^ 0b1010, 0b0110);
        Check("num.not", ~0, -1);
        Check("num.mod", 17 % 5, 2);
        Check("num.negdiv", -17 / 5, -3);
        Check("num.byte.overflow", unchecked((byte)(250 + 10)), (byte)4);
        Check("num.decimal", 1.10m + 2.20m, 3.30m);
        CheckClose("num.double", 0.1 + 0.2, 0.3);
        CheckClose("math.sqrt", Math.Sqrt(144), 12);
        CheckClose("math.pow", Math.Pow(2, 10), 1024);
        CheckClose("math.round", Math.Round(2.567, 2), 2.57);
        Check("math.abs", Math.Abs(-42), 42);
        Check("math.max", Math.Max(3, 7), 7);
        Check("math.min", Math.Min(3, 7), 3);
        Check("math.sign", Math.Sign(-5), -1);
        Check("math.floor", (int)Math.Floor(3.9), 3);
        Check("math.ceil", (int)Math.Ceiling(3.1), 4);

        Section("control-flow");
        Check("cf.while", WhileCount(), 5);
        Check("cf.dowhile", DoWhile(), 1);
        Check("cf.goto", GotoSum(), 10);
        Check("cf.nested.break", NestedBreak(), 6);
        Check("cf.continue", ContinueSum(), 25);
        Check("cf.switch.goto", SwitchGoto(), 3);
        Check("cf.ternary.chain", TernaryChain(75), 'C');
        Check("cf.loop.sum", SumTo(100), 5050);

        Section("recursion");
        Check("rec.factorial", Factorial(5), 120);
        Check("rec.fib", Fib(15), 610);
        Check("rec.ackermann", Ack(2, 3), 9);
        Check("rec.mutual", IsEvenRec(10), true);
        Check("rec.gcd", Gcd(48, 36), 12);
        Check("rec.localfn", LocalFn(6), 720);

        Section("generics");
        Check("gen.echo", Echo(1234), 1234);
        Check("gen.echo.string", Echo("gen"), "gen");
        Check("gen.max", Max(3, 9), 9);
        Check("gen.max.string", Max("apple", "banana"), "banana");
        Check("gen.pair", new Pair<int, string>(7, "x").First, 7);
        Check("gen.swap", GenSwap(), "b,a");
        Check("gen.stack", GenStack(), 3);

        Section("collections");
        Check("coll.list", ListOps(), 6);
        Check("coll.list.generic", SumList([3, 1, 4, 1, 5, 9, 2, 6]), 31);
        Check("coll.dict.count", LookupCount(), 3);
        Check("coll.dict.tryget", DictTryGet(), 20);
        Check("coll.hashset", HashSetDistinct(), 3);
        Check("coll.queue", QueueFifo(), 1);
        Check("coll.stack", StackLifo(), 3);
        Check("coll.sorteddict", SortedFirst(), "a");
        Check("coll.linkedlist", LinkedSum(), 6);
        Check("coll.array.sort", ArraySortFirst(), 1);
        Check("coll.array.indexof", Array.IndexOf(new[] { 5, 6, 7 }, 7), 2);
        Check("coll.array.reverse", ArrayReverseFirst(), 3);
        Check("coll.2d", Grid(), 6);
        Check("coll.jagged", Jagged(), 5);
        Check("coll.range", RangeSum(), 90);
        Check("coll.index.fromend", LastElem(), 9);
        Check("coll.stackalloc", StackAllocSum(), 15);

        Section("linq");
        Check("linq.where", Enumerable.Range(1, 10).Where(x => x % 2 == 0).Sum(), 30);
        Check("linq.select", Enumerable.Range(1, 10).Select(x => x * x).Sum(), 385);
        Check("linq.aggregate", Enumerable.Range(1, 10).Aggregate(0, (a, b) => a + b), 55);
        Check("linq.orderby.desc", Enumerable.Range(1, 10).OrderByDescending(x => x).First(), 10);
        Check("linq.groupby", Enumerable.Range(1, 10).GroupBy(x => x % 2).Count(), 2);
        Check("linq.any", Enumerable.Range(1, 10).Any(x => x > 9), true);
        Check("linq.all", Enumerable.Range(1, 10).All(x => x > 0), true);
        Check("linq.distinct", new[] { 1, 1, 2, 2, 3 }.Distinct().Count(), 3);
        Check("linq.take.skip", Enumerable.Range(1, 10).Skip(2).Take(3).Sum(), 12);
        Check("linq.minmax", Enumerable.Range(1, 10).Max() - Enumerable.Range(1, 10).Min(), 9);
        CheckClose("linq.average", Enumerable.Range(1, 10).Average(), 5.5);
        Check("linq.zip", new[] { 1, 2, 3 }.Zip(new[] { 10, 20, 30 }, (a, b) => a + b).Sum(), 66);
        Check("linq.selectmany", new[] { "ab", "cd" }.SelectMany(s => s).Count(), 4);
        Check("linq.todict", Enumerable.Range(1, 3).ToDictionary(x => x, x => x * x)[3], 9);
        Check("linq.first.predicate", Enumerable.Range(1, 10).First(x => x > 5), 6);
        Check("linq.last", Enumerable.Range(1, 10).Last(), 10);
        Check("linq.count.predicate", Enumerable.Range(1, 10).Count(x => x % 3 == 0), 3);
        Check("linq.contains", Enumerable.Range(1, 10).Contains(7), true);

        Section("exceptions");
        Check("exc.divzero", DivideSafe(10, 0), -1);
        Check("exc.finally", WithFinally(), "try+finally");
        Check("exc.custom", CatchCustom(), "boom");
        Check("exc.filter", ExcFilter(), "filtered");
        Check("exc.rethrow", ExcRethrow(), 2);
        Check("exc.nested", ExcNested(), "inner+outer");
        Check("exc.nullref", ExcNullRef(), true);
        Check("exc.indexoob", ExcIndex(), true);
        Check("exc.checked.overflow", CheckedOverflows(), true);
        Check("exc.format", ExcFormat(), true);
        Check("exc.in.loop", ExcLoopCount(), 3);

        Section("delegates");
        Check("del.lambda", Apply(x => x * x, 9), 81);
        Check("del.methodgroup", MethodGroup(), 27);
        Check("del.predicate", Pred(), true);
        Check("del.multicast", Multicast(), 11);
        Check("del.closure", Adder(100)(23), 123);
        Check("del.event", EventRaise(), 2);
        Check("del.func.compose", Compose(), 20);

        Section("interfaces-oop");
        Check("oop.iface.dispatch", TotalArea(), 37);
        Check("oop.iface.explicit", ExplicitImpl(), 99);
        Check("oop.virtual", VirtualDispatch(), "dog:woof");
        Check("oop.abstract", AbstractArea(), 50);
        Check("oop.base.call", BaseCall(), 15);
        Check("oop.iface.generic", GenericIface(), 42);
        Check("oop.iface.default", DefaultIfaceMethod(), 7);
        Check("oop.indexer", new Ring(4)[6], 4);
        Check("oop.extension.int", 21.Twice(), 42);
        Check("oop.extension.string", "hi".Shout(), "HI!");

        Section("structs-tuples");
        Check("struct.box", BoxRoundTrip(42), 42);
        Check("struct.mutate", StructMutate(), 10);
        Check("struct.readonly", ReadonlyStruct(), 7);
        Check("struct.operator.add", (new Vec(1, 2) + new Vec(3, 4)).X, 4);
        Check("struct.operator.eq", new Vec(5, 6) == new Vec(5, 6), true);
        Check("struct.nullable", ((Vec?)new Vec(3, 4))!.Value.X, 3);
        Check("tuple.swap", SwapPair((1, 2)), (2, 1));
        Check("tuple.named", NamedTuple().sum, 30);
        Check("tuple.deconstruct", TupleDeconstruct(), 3);
        Check("record.eq", new Point(1, 2) == new Point(1, 2), true);
        Check("record.with", (new Point(1, 2) with { Y = 5 }).Y, 5);
        Check("record.deconstruct", RecordDeconstruct(), 3);

        Section("patterns");
        Check("pat.type.big", Classify(500), "big");
        Check("pat.type.int", Classify(5), "int");
        Check("pat.type.longstr", Classify("hello"), "longstr");
        Check("pat.type.null", Classify(null!), "null");
        Check("pat.property", Origin(new Point(0, 0)), true);
        Check("pat.list.empty", ListPat([]), "empty");
        Check("pat.list.ends", ListPat([1, 2, 3, 4]), "ends:1,4");
        Check("pat.relational", Grade(85), 'B');
        Check("pat.nullable.coalesce", ((int?)null) ?? -7, -7);
        Check("pat.nullcond", NullCond(), -1);

        Section("enums");
        Check("enum.tostring", Color.Green.ToString(), "Green");
        Check("enum.cast", (int)Color.Blue, 2);
        Check("enum.parse", Enum.Parse<Color>("Red"), Color.Red);
        Check("enum.flags.hasflag", (Perm.Read | Perm.Write).HasFlag(Perm.Write), true);
        Check("enum.flags.value", (int)(Perm.Read | Perm.Write), 3);

        Section("iterators");
        Check("iter.fib", Fibs().Take(10).Sum(), 88);
        Check("iter.yield.break", YieldBreak().Count(), 3);
        Check("iter.infinite.take", Naturals().Take(5).Sum(), 15);
        Check("iter.countup", CountUp(5).Count(), 5);

        Section("conversions");
        Check("conv.parse.int", int.Parse("12345"), 12345);
        Check("conv.toint32", Convert.ToInt32("777"), 777);
        Check("conv.tryparse.fail", int.TryParse("abc", out _), false);
        CheckClose("conv.parse.double", double.Parse("3.14159", CultureInfo.InvariantCulture), 3.14159);
        Check("conv.tostring.hex", 255.ToString("X2"), "FF");
        Check("conv.tostring.d", 7.ToString("D3"), "007");
        Check("conv.bitconverter", BitConverter.ToInt32(BitConverter.GetBytes(123456789), 0), 123456789);
        Check("conv.base64", Convert.FromBase64String(Convert.ToBase64String(new byte[] { 1, 2, 3 }))[2], (byte)3);

        Section("runtime");
        Check("rt.typeof.name", typeof(int).Name, "Int32");
        Check("rt.typeof.generic", typeof(List<int>).GetGenericArguments()[0], typeof(int));
        Check("rt.gettype.name", 5.GetType().Name, "Int32");
        Check("rt.nameof.local", NameofCheck(), "value");
        Check("rt.array.rva", SumBytes(Payload), 36);
        Check("rt.static.ctor", Counter.Value, 55);
        Check("rt.datetime.month", new DateTime(2020, 1, 1).AddDays(45).Month, 2);
        Check("rt.datetime.dow", new DateTime(2020, 1, 1).DayOfWeek, DayOfWeek.Wednesday);
        CheckClose("rt.timespan", TimeSpan.FromHours(2.5).TotalMinutes, 150);
        Check("rt.guid.roundtrip", GuidRoundTrip(), true);
        Check("rt.encoding.utf8", Utf8Round("héllo🦊"), "héllo🦊");
        Check("rt.encoding.bytes", Encoding.UTF8.GetBytes("A").Length, 1);

        Console.WriteLine(_failures == 0
            ? $"ALL PASS ({_checks} checks)"
            : $"{_failures}/{_checks} FAILURE(S)");
        return _failures;
    }

    private static void Section(string name) => Console.WriteLine($"-- {name} --");

    private static void Check<T>(string name, T actual, T expected) {
        _checks++;
        var ok = EqualityComparer<T>.Default.Equals(actual, expected);
        if (!ok) _failures++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name} => {actual}");
    }

    private static void CheckClose(string name, double actual, double expected, double epsilon = 1e-9) {
        _checks++;
        var ok = Math.Abs(actual - expected) <= epsilon;
        if (!ok) _failures++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name} => {actual}");
    }

    private static string Concat(string a, string b) => a + b + "d";
    private static string Greet(string who) => $"hello, {who}!";

    private static int Weekday(string day) => day switch {
        "mon" => 1,
        "tue" => 2,
        "wed" => 3,
        "thu" => 4,
        "fri" => 5,
        _ => 0,
    };

    private static string BuildRepeat(string unit, int times) {
        var builder = new StringBuilder();
        for (var i = 0; i < times; i++) builder.Append(unit);
        return builder.ToString();
    }

    private static int WhileCount() {
        var n = 0;
        while (n < 5) n++;
        return n;
    }

    private static int DoWhile() {
        var n = 0;
        do {
            n++;
        } while (false);

        return n;
    }

    private static int GotoSum() {
        int i = 0, total = 0;
        loop:
        if (i < 5) {
            total += i;
            i++;
            goto loop;
        }

        return total;
    }

    private static int NestedBreak() {
        var count = 0;
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++) {
            if (j == 2) break;
            count++;
        }

        return count;
    }

    private static int ContinueSum() {
        var total = 0;
        for (var i = 1; i <= 10; i++) {
            if (i % 2 == 0) continue;
            total += i;
        }

        return total;
    }

    private static int SwitchGoto() {
        int n = 0, x = 1;
        switch (x) {
            case 1:
                n++;
                goto case 2;
            case 2:
                n++;
                goto case 3;
            case 3:
                n++;
                break;
        }

        return n;
    }

    private static char TernaryChain(int score) => score >= 90 ? 'A' : score >= 80 ? 'B' : score >= 70 ? 'C' : 'F';

    private static int SumTo(int n) {
        var total = 0;
        for (var i = 1; i <= n; i++) total += i;
        return total;
    }

    private static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
    private static int Fib(int n) => n < 2 ? n : Fib(n - 1) + Fib(n - 2);
    private static int Ack(int m, int n) => m == 0 ? n + 1 : n == 0 ? Ack(m - 1, 1) : Ack(m - 1, Ack(m, n - 1));
    private static bool IsEvenRec(int n) => n == 0 || IsOddRec(n - 1);
    private static bool IsOddRec(int n) => n != 0 && IsEvenRec(n - 1);
    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    private static int LocalFn(int n) {
        int Fact(int k) => k <= 1 ? 1 : k * Fact(k - 1);
        return Fact(n);
    }

    private static T Echo<T>(T value) => value;
    private static T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0 ? a : b;

    private static string GenSwap() {
        var (x, y) = Swap("a", "b");
        return x + "," + y;
    }

    private static (T, T) Swap<T>(T a, T b) => (b, a);

    private static int GenStack() {
        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        return stack.Count;
    }

    private static int ListOps() {
        var list = new List<int> { 1, 2, 3 };
        var total = 0;
        foreach (var value in list) total += value;
        return total;
    }

    private static int SumList(List<int> values) {
        var total = 0;
        foreach (var value in values) total += value;
        return total;
    }

    private static int LookupCount() {
        var map = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
        return map.Count;
    }

    private static int DictTryGet() {
        var map = new Dictionary<string, int> { ["k"] = 20 };
        return map.TryGetValue("k", out var value) ? value : -1;
    }

    private static int HashSetDistinct() => new HashSet<int> { 1, 2, 2, 3, 3, 3 }.Count;

    private static int QueueFifo() {
        var queue = new Queue<int>();
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3);
        return queue.Dequeue();
    }

    private static int StackLifo() {
        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        return stack.Pop();
    }

    private static string SortedFirst() {
        var map = new SortedDictionary<string, int> { ["b"] = 2, ["a"] = 1, ["c"] = 3 };
        foreach (var key in map.Keys) return key;
        return string.Empty;
    }

    private static int LinkedSum() {
        var list = new LinkedList<int>();
        list.AddLast(1);
        list.AddLast(2);
        list.AddLast(3);
        var total = 0;
        foreach (var value in list) total += value;
        return total;
    }

    private static int ArraySortFirst() {
        var array = new[] { 3, 1, 2 };
        Array.Sort(array);
        return array[0];
    }

    private static int ArrayReverseFirst() {
        var array = new[] { 1, 2, 3 };
        Array.Reverse(array);
        return array[0];
    }

    private static int Grid() {
        var grid = new int[2, 3];
        var n = 1;
        for (var r = 0; r < 2; r++)
        for (var c = 0; c < 3; c++)
            grid[r, c] = n++;
        return grid[1, 2];
    }

    private static int Jagged() {
        var jagged = new[] { new[] { 1, 2 }, new[] { 3, 4, 5 } };
        return jagged[1][2];
    }

    private static int RangeSum() {
        int[] array = { 10, 20, 30, 40, 50 };
        var total = 0;
        foreach (var value in array[1..4]) total += value;
        return total;
    }

    private static int LastElem() {
        int[] array = { 7, 8, 9 };
        return array[^1];
    }

    private static int StackAllocSum() {
        Span<int> span = stackalloc int[5];
        for (var i = 0; i < span.Length; i++) span[i] = i + 1;
        var total = 0;
        foreach (var value in span) total += value;
        return total;
    }

    private static int DivideSafe(int a, int b) {
        try {
            return a / b;
        }
        catch (DivideByZeroException) {
            return -1;
        }
    }

    private static string WithFinally() {
        var builder = new StringBuilder();
        try {
            builder.Append("try");
        }
        finally {
            builder.Append("+finally");
        }

        return builder.ToString();
    }

    private static string CatchCustom() {
        try {
            throw new TestError("boom");
        }
        catch (TestError error) {
            return error.Message;
        }
    }

    private static string ExcFilter() {
        try {
            throw new InvalidOperationException("x");
        }
        catch (Exception error) when (error.Message == "x") {
            return "filtered";
        }
    }

    private static int ExcRethrow() {
        var n = 0;
        try {
            try {
                throw new Exception();
            }
            catch {
                n++;
                throw;
            }
        }
        catch {
            n++;
        }

        return n;
    }

    private static string ExcNested() {
        var builder = new StringBuilder();
        try {
            try {
                throw new Exception("i");
            }
            catch {
                builder.Append("inner");
                throw new Exception("o");
            }
        }
        catch {
            builder.Append("+outer");
        }

        return builder.ToString();
    }

    private static bool ExcNullRef() {
        try {
            string value = null!;
            return value.Length == 0;
        }
        catch (NullReferenceException) {
            return true;
        }
    }

    private static bool ExcIndex() {
        try {
            var array = new[] { 1 };
            return array[5] == 0;
        }
        catch (IndexOutOfRangeException) {
            return true;
        }
    }

    private static bool CheckedOverflows() {
        try {
            checked {
                var value = int.MaxValue;
                value++;
                return value == 0;
            }
        }
        catch (OverflowException) {
            return true;
        }
    }

    private static bool ExcFormat() {
        try {
            int.Parse("not-a-number");
            return false;
        }
        catch (FormatException) {
            return true;
        }
    }

    private static int ExcLoopCount() {
        var count = 0;
        foreach (var text in new[] { "1", "x", "2", "y", "z" }) {
            try {
                int.Parse(text);
            }
            catch {
                count++;
            }
        }

        return count;
    }

    // delegates
    private static int Apply(Func<int, int> function, int value) => function(value);

    private static int MethodGroup() {
        Func<int, int> cube = Cube;
        return cube(3);
    }

    private static int Cube(int x) => x * x * x;

    private static bool Pred() {
        Predicate<int> even = IsEven;
        return even(4);
    }

    private static bool IsEven(int x) => x % 2 == 0;

    private static int _multicast;

    private static int Multicast() {
        _multicast = 0;
        Action action = () => _multicast += 1;
        action += () => _multicast += 10;
        action();
        return _multicast;
    }

    private static Func<int, int> Adder(int n) => x => x + n;

    private static int EventRaise() {
        var bell = new Bell();
        bell.Ring();
        bell.Ring();
        return bell.Count;
    }

    private static int Compose() {
        Func<int, int> twice = x => x * 2;
        Func<int, int> incr = x => x + 1;
        Func<int, int> composed = x => twice(incr(x));
        return composed(9);
    }

    // interfaces / oop
    private static readonly byte[] Payload = { 1, 2, 3, 4, 5, 6, 7, 8 };

    private static int SumBytes(byte[] data) {
        var total = 0;
        foreach (var value in data) total += value;
        return total;
    }

    private static int TotalArea() {
        IShape[] shapes = [new Square(5), new Rectangle(3, 4)];
        var total = 0;
        foreach (var shape in shapes) total += shape.Area();
        return total;
    }

    private static int ExplicitImpl() => ((ICode)new CodeImpl()).Code();

    private static string VirtualDispatch() => new Dog().Speak();

    private static int AbstractArea() => new AbsSquare(5).Area() + new AbsSquare(5).Area();

    private static int BaseCall() => new Derived().Val();

    private static int GenericIface() => new Box<int>(42).Get();

    private static int DefaultIfaceMethod() => ((IWithDefault)new WithDefault()).Plus();

    private static int BoxRoundTrip(int value) {
        object boxed = value;
        return (int)boxed;
    }

    private static int StructMutate() {
        var mutable = new Mutable { Value = 9 };
        mutable.Increment();
        return mutable.Value;
    }

    private static int ReadonlyStruct() => new ReadOnlyPoint(3).Plus(4);

    private static (int, int) SwapPair((int First, int Second) pair) => (pair.Second, pair.First);

    private static (int sum, int prod) NamedTuple() => (sum: 10 + 20, prod: 200);

    private static int TupleDeconstruct() {
        var (a, b) = (1, 2);
        return a + b;
    }

    private static int RecordDeconstruct() {
        var (x, y) = new Point(1, 2);
        return x + y;
    }

    private static string Classify(object value) => value switch {
        int and > 100 => "big",
        int => "int",
        string text when text.Length > 3 => "longstr",
        string => "str",
        null => "null",
        _ => "other",
    };

    private static bool Origin(Point point) => point is { X: 0, Y: 0 };

    private static string ListPat(int[] values) => values switch {
        [] => "empty",
        [var only] => "one:" + only,
        [var first, .., var last] => $"ends:{first},{last}",
    };

    private static char Grade(int score) => score switch {
        >= 90 => 'A',
        >= 80 => 'B',
        >= 70 => 'C',
        _ => 'F',
    };

    private static int NullCond() {
        string? value = null;
        return value?.Length ?? -1;
    }

    private static IEnumerable<int> Fibs() {
        int a = 0, b = 1;
        while (true) {
            yield return a;
            (a, b) = (b, a + b);
        }
    }

    private static IEnumerable<int> YieldBreak() {
        for (var i = 1; i <= 10; i++) {
            if (i > 3) yield break;
            yield return i;
        }
    }

    private static IEnumerable<int> Naturals() {
        var i = 1;
        while (true) yield return i++;
    }

    private static IEnumerable<int> CountUp(int n) {
        for (var i = 1; i <= n; i++) yield return i;
    }

    private static string NameofCheck() {
        var value = 5;
        return nameof(value);
    }

    private static bool GuidRoundTrip() {
        var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
        return guid == new Guid(guid.ToString());
    }

    private static string Utf8Round(string text) => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text));
}

internal sealed class TestError(string message) : Exception(message);

internal interface IShape {
    int Area();
}

internal sealed class Square(int side) : IShape {
    public int Area() => side * side;
}

internal sealed class Rectangle(int width, int height) : IShape {
    public int Area() => width * height;
}

internal static class Counter {
    public static readonly int Value;

    static Counter() {
        for (var i = 1; i <= 10; i++) Value += i;
    }
}

internal interface ICode {
    int Code();
}

internal sealed class CodeImpl : ICode {
    int ICode.Code() => 99;
}

internal abstract class Animal {
    public abstract string Sound();
    public virtual string Name() => "animal";
    public string Speak() => Name() + ":" + Sound();
}

internal sealed class Dog : Animal {
    public override string Sound() => "woof";
    public override string Name() => "dog";
}

internal abstract class AbstractShape {
    public abstract int Area();
}

internal sealed class AbsSquare(int side) : AbstractShape {
    public override int Area() => side * side;
}

internal class BaseValue {
    public virtual int Val() => 10;
}

internal sealed class Derived : BaseValue {
    public override int Val() => base.Val() + 5;
}

internal interface IBox<out T> {
    T Get();
}

internal sealed class Box<T>(T value) : IBox<T> {
    public T Get() => value;
}

internal interface IWithDefault {
    int Base();
    int Plus() => Base() + 2;
}

internal sealed class WithDefault : IWithDefault {
    public int Base() => 5;
}

internal sealed class Ring {
    private readonly int[] _data;

    public Ring(int size) {
        _data = new int[size];
        for (var i = 0; i < size; i++) _data[i] = i * i;
    }

    public int this[int index] => _data[index % _data.Length];
}

internal static class Extensions {
    public static int Twice(this int value) => value * 2;
    public static string Shout(this string text) => text.ToUpperInvariant() + "!";
}

internal struct Mutable {
    public int Value;
    public void Increment() => Value++;
}

internal readonly struct ReadOnlyPoint(int x) {
    public int Plus(int y) => x + y;
}

internal readonly struct Vec(int x, int y) {
    public readonly int X = x;
    public readonly int Y = y;

    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static bool operator ==(Vec a, Vec b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec a, Vec b) => !(a == b);
    public override bool Equals(object? other) => other is Vec vec && this == vec;
    public override int GetHashCode() => X * 31 + Y;
}

internal sealed record Point(int X, int Y);

internal sealed class Bell {
    public int Count;
    public event Action? Rung;

    public Bell() => Rung += () => Count++;

    public void Ring() => Rung?.Invoke();
}

internal sealed class Pair<T1, T2>(T1 first, T2 second) {
    public T1 First { get; } = first;
    public T2 Second { get; } = second;
}

internal enum Color {
    Red,
    Green,
    Blue,
}

[Flags]
internal enum Perm {
    None = 0,
    Read = 1,
    Write = 2,
    Exec = 4,
}