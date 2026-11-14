namespace SharpHide.Tests;

public static class VmReferenceReturnSamples {
    private static int _static;

    [Virtualize]
    private static ref int Select(int[] values, ref int other, bool first) {
        if (first)
            return ref values[1];
        return ref other;
    }

    [Virtualize]
    private static ref T Field<T>(VmReferenceHolder<T> holder) => ref holder.Value;

    [Virtualize]
    private static ref int Static() => ref _static;

    [Virtualize]
    private static ref readonly int ReadOnly(int[] values) => ref values[0];

    [Virtualize]
    private static unsafe ref int Pointer(int* value) => ref *value;

    [Virtualize]
    private static unsafe ref int NewInterior(int value) {
        var holder = new VmReferenceHolder<int> { Value = value };
        fixed (int* pointer = &holder.Value) {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            return ref *pointer;
        }
    }

    [Virtualize]
    public static unsafe string Run(int value) {
        var values = new[] { value, value * 2 };
        var other = value;
        ref var array = ref Select(values, ref other, true);
        ref var argument = ref Select(values, ref other, false);
        var holder = new VmReferenceHolder<string> { Value = "before" };
        ref var field = ref Field(holder);
        ref var shared = ref Static();
        ref readonly var readOnly = ref ReadOnly(values);
        var native = value;
        ref var pointer = ref Pointer(&native);
        ref var interior = ref NewInterior(value);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        array += 3;
        argument += 5;
        field = "after";
        shared = 11;
        pointer += 7;
        interior += 9;
        return values[1] + ":" + other + ":" + holder.Value + ":" + _static + ":" + readOnly + ":" + native + ":" + interior;
    }
}

public class VmReferenceHolder<T> {
    public T Value = default!;
}
