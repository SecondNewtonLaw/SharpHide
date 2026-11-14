using System.Runtime.CompilerServices;

namespace SharpHide.Tests;

public static class VmReferenceCallSamples {
    private static ref int Fresh(ref int calls) => ref new VmReferenceHolder<int> { Value = ++calls }.Value;
    private static ref int Identity(ref int value) => ref value;
    private static ref bool Accept(ref int trace) {
        trace = trace * 10 + 2;
        return ref new VmReferenceHolder<bool> { Value = true }.Value;
    }

    [Virtualize]
    public static int Reuse(int count) {
        var calls = 0;
        ref var first = ref Fresh(ref calls);
        ref var current = ref first;
        var result = 0;
        for (var i = 0; i < count; i++) {
            current = ref Fresh(ref calls);
            if ((i & 15) == 0)
                GC.Collect(2, GCCollectionMode.Forced, true, true);
            current += i;
            result += current;
        }
        first += 7;
        return result + first + calls;
    }

    [Virtualize]
    public static unsafe int Pin(int value) {
        var calls = value;
        ref var reference = ref Fresh(ref calls);
        fixed (int* pointer = &reference) {
            var before = (nint)Unsafe.AsPointer(ref reference);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            if (before != (nint)Unsafe.AsPointer(ref reference))
                return -999;
            *pointer += 17;
            return reference;
        }
    }

    [Virtualize]
    public static unsafe int Indirect(int value) {
        delegate* managed<ref int, ref int> function = &Identity;
        ref var reference = ref function(ref value);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        reference += 3;
        return value;
    }

    [Virtualize]
    public static int RepeatedException(int value) {
        var error = new InvalidOperationException();
        var trace = 0;
        var calls = 0;
        var result = 0;
        ref var reference = ref Fresh(ref calls);
        for (var i = 0; i <= (value & 7); i++) {
            try {
                throw error;
            }
            catch (InvalidOperationException) when (Accept(ref trace)) {
                reference = ref Fresh(ref calls);
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                reference += trace;
                result += reference;
            }
        }
        return result + calls;
    }

    [Virtualize]
    public static int Unwind(int value) {
        var calls = 0;
        var trace = 0;
        ref var reference = ref Fresh(ref calls);
        try {
            try {
                throw new ArgumentException();
            }
            finally {
                reference = ref Fresh(ref calls);
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                trace = trace * 10 + 1;
                if ((value & 1) == 0)
                    throw new InvalidOperationException();
            }
        }
        catch (InvalidOperationException) when (Accept(ref trace) && 12 / (value & 3) > 0) {
            reference += 7;
            trace = trace * 10 + 3;
        }
        catch (Exception) when (Accept(ref trace)) {
            reference += 9;
            trace = trace * 10 + 4;
        }
        finally {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            reference += 13;
            trace = trace * 10 + 5;
        }
        return trace * 100 + reference + calls;
    }

    [Virtualize]
    public static int Regions(int value) {
        var calls = 0;
        var trace = 0;
        ref var reference = ref Fresh(ref calls);
        try {
            try {
                trace = 1;
                if (value == 0)
                    throw new InvalidOperationException();
                reference += value;
            }
            finally {
                reference = ref Fresh(ref calls);
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                trace = trace * 10 + 3;
            }
        }
        catch (InvalidOperationException) when (Accept(ref trace)) {
            reference += 4;
            trace = trace * 10 + 4;
        }
        return trace * 100 + reference + calls;
    }
}
