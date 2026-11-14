namespace SharpHide.Tests;

public static class VmNativeSamples {
    [Virtualize]
    private static Span<T> Slice<T>(T[] values) => values.AsSpan(1);

    [Virtualize]
    private static void Replace<T>(ref Span<T> values, T[] replacement) => values = replacement.AsSpan(1);

    [Virtualize]
    private static ref Span<T> Alias<T>(ref Span<T> values) => ref values;

    [Virtualize]
    private static string Read(TypedReference value) {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        return __refvalue(value, string);
    }

    [Virtualize]
    private static string Consume(Span<string> values) {
        var original = values;
        values = new[] { "local" }.AsSpan();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        original[0] += "!";
        return original[0] + ":" + values[0];
    }

    [Virtualize]
    public static string Spans(string text) {
        Span<string> first = Slice(new[] { "unused", text });
        Span<string> second = first;
        Replace(ref first, new[] { "unused", "replacement" });
        Span<int> numbers = stackalloc int[3];
        numbers[0] = 10;
        numbers[1] = 20;
        numbers[2] = 12;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var result = Consume(second);
        return result + ":" + second[0] + ":" + first[0] + ":" + (numbers[0] + numbers[1] + numbers[2]);
    }

    [Virtualize]
    public static string TypedLocals(string text) {
        var first = new[] { text };
        var second = new[] { "replacement" };
        TypedReference original = __makeref(first[0]);
        TypedReference copy = original;
        original = __makeref(second[0]);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        __refvalue(copy, string) += "!";
        return Read(copy) + ":" + Read(original);
    }

    [Virtualize]
    public static string ReferenceReturn(string text) {
        Span<string> values = Slice(new[] { "unused", text });
        ref var reference = ref Alias(ref values);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        reference = new[] { text + "!" }.AsSpan();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        return values[0];
    }
}

public static class VmAllowRefStructSamples {
    [Virtualize]
    private static T Identity<T>(T value) where T : allows ref struct {
        T copy = value;
        return copy;
    }

    public static int Run() {
        Span<int> values = stackalloc int[3];
        values[0] = 17;
        values[1] = 19;
        values[2] = 23;
        return Identity(values).Length + Identity(values)[1] + Identity(7);
    }
}

public static class VmVarArgSamples {
    [Virtualize]
    public static int Sum(int expected, __arglist) {
        var arguments = new ArgIterator(__arglist);
        var total = 0;
        while (arguments.GetRemainingCount() != 0) {
            var argument = arguments.GetNextArg();
            total += __refvalue(argument, int);
        }
        return total + expected;
    }
}
