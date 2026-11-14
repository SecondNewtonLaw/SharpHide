using SharpHide.Vendor;

namespace ILTestLand;

internal static class VirtualizationCases {
    [Virtualize]
    public static int Arithmetic(int count) {
        var sum = 0;
        for (var i = 0; i <= count; i++)
            sum += (i & 1) == 0 ? i * i : -i;
        return sum;
    }

    [Virtualize]
    private static ref T Element<T>(T[] values, int index) => ref values[index];

    [Virtualize]
    private static ref Span<T> SpanReference<T>(ref Span<T> value) => ref value;

    [Virtualize]
    public static string Aliases() {
        var values = new[] { "first", "second" };
        ref var reference = ref Element(values, 1);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        reference = "changed";
        return values[1];
    }

    [Virtualize]
    public static int Exceptions(bool fail) {
        var trace = 1;
        try {
            try {
                if (fail)
                    throw new InvalidOperationException();
            }
            finally {
                trace = trace * 10 + 3;
            }
        }
        catch (InvalidOperationException) when ((trace = trace * 10 + 2) > 0) {
            trace = trace * 10 + 4;
        }
        return trace;
    }

    [Virtualize]
    public static unsafe int StackMemory(int start) {
        var values = stackalloc int[8];
        for (var i = 0; i < 8; i++)
            values[i] = start + i;
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var sum = 0;
        for (var i = 0; i < 8; i++)
            sum += values[i];
        return sum;
    }

    [Virtualize]
    public static long Wide(long value) => unchecked((value << 13) + (value >> 7));

    [Virtualize]
    public static int TypedReference(int value) {
        TypedReference reference = __makeref(value);
        __refvalue(reference, int) = unchecked(value + 7);
        return __reftype(reference) == typeof(int) ? value : -1;
    }

    [Virtualize]
    public static string Spans(string text) {
        Span<string> values = new[] { text, "second" }.AsSpan();
        Span<string> copy = values;
        ref var reference = ref SpanReference(ref values);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        reference = new[] { "replacement" }.AsSpan();
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        copy[0] += "!";
        return copy[0] + ":" + values[0];
    }
}
