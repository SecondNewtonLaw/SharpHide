namespace SharpHide.Tests;

public static class VmTypedSamples {
    [Virtualize]
    public static long LongArithmetic(long a, long b) => unchecked((a + b) * (a - b)) ^ (a >>> (int)b);

    [Virtualize]
    public static ulong UnsignedLong(ulong a, ulong b) => a / b + a % b;

    [Virtualize]
    public static long CheckedLong(long a, long b) => checked(a * b);

    [Virtualize]
    public static double DoubleArithmetic(double a, double b) => (a + b) / (a - b);

    [Virtualize]
    public static float SingleArithmetic(float a, float b) => (a + b) * (a - b);

    [Virtualize]
    public static int FloatBranches(double a, double b) {
        var result = 0;
        if (a >= b)
            result |= 1;
        if (a <= b)
            result |= 2;
        if (a > b)
            result |= 4;
        if (a < b)
            result |= 8;
        if (a == b)
            result |= 16;
        if (a != b)
            result |= 32;
        return result;
    }

    [Virtualize]
    public static nint Native(nint a, int b) => (a << b) ^ (a >>> b);

    [Virtualize]
    public static string Calls(string value, int count) => string.Concat(value.ToUpperInvariant(), Math.Abs(count), "!");

    [Virtualize]
    public static int Recursive(int value) => value <= 1 ? 1 : value * Recursive(value - 1);

    [Virtualize]
    public static int Arrays(int size, int index) {
        var values = new int[size];
        values[index] = size * 7;
        return values[index] + values.Length;
    }

    [Virtualize]
    public static string References(string value) {
        object[] values = new string[1];
        values[0] = value;
        return (string)values[0];
    }

    [Virtualize]
    public static bool ReferenceIdentity(object a, object b) => a == b;

    [Virtualize]
    public static object Box(uint value) => value;

    [Virtualize]
    public static int Unbox(object value) => (int)value;

    [Virtualize]
    public static bool TypeTest(object value) => value is string;

    [Virtualize]
    public static Type Token() => typeof(VmTypedSamples);

    [Virtualize]
    public static decimal Struct(decimal a, decimal b) => a + b;

    [Virtualize]
    public static int InstanceCalls(int value) {
        var instance = new VmDispatchDerived();
        instance.Set(value);
        return instance.Read() + instance.ReadBase();
    }

    [Virtualize]
    private static int Aliased(ref int a, ref int b) {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        a += 3;
        b *= 2;
        return a + b;
    }

    [Virtualize]
    public static int ArgumentAddress(int value) => Aliased(ref value, ref value) + value;

    [Virtualize]
    public static int LocalAddress(int value) {
        var local = value * 3;
        ref var alias = ref local;
        alias++;
        return Aliased(ref local, ref alias) + local;
    }

    [Virtualize]
    public static int FieldAddress(int value) {
        var instance = new VmDispatchDerived();
        instance.Value = value;
        return Aliased(ref instance.Value, ref instance.Value) + instance.Value;
    }

    [Virtualize]
    public static int ArrayAddress(int value) {
        var array = new int[1];
        array[0] = value;
        return Aliased(ref array[0], ref array[0]) + array[0];
    }

    [Virtualize]
    public static string ValueTypeCall(int value) => value.ToString();

    [Virtualize]
    public static int StructMutation(int value) {
        VmMutable local = default;
        local.Value = value;
        var copy = local;
        copy.Add(7);
        return local.Value * 100 + copy.Value;
    }

    [Virtualize]
    public static T Identity<T>(T value) => value;

    [Virtualize]
    public static string GenericString<T>(T value) => value!.ToString()!;

    [Virtualize]
    public static int GenericCalls(int value) {
        var list = new List<int>();
        list.Add(Identity(value));
        return list[0] + list.Count;
    }

    private static List<T> Wrap<T>(T value) => [value];

    [Virtualize]
    public static int NestedGenericReturn(int value) => Wrap(value)[0];

    [Virtualize]
    public static int NestedGenericField(int value) {
        var box = new VmGenericBox<int> { Values = [value] };
        return box.Values[0];
    }

    [Virtualize]
    public static int Nullable(int? value) => value.GetValueOrDefault(-10);

    public static int ExternalAlias(int value) {
        var array = new int[1];
        array[0] = value;
        return Aliased(ref array[0], ref array[0]) + array[0];
    }

    [Virtualize]
    private static void Out(out int value) => value = 42;

    public static int ExternalOut() {
        Out(out var value);
        return value;
    }

    [Virtualize]
    private static void RefString(ref string value) {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        value += "!";
    }

    public static string ExternalReference(string value) {
        var array = new string[1];
        array[0] = value;
        RefString(ref array[0]);
        return array[0];
    }

    [Virtualize]
    public static int Delegates(int value) {
        var instance = new VmDispatchDerived();
        instance.Value = value;
        Func<int> read = instance.Read;
        Func<int, int> staticCall = Math.Abs;
        return read() + staticCall(value);
    }

    [Virtualize]
    public static int Closure(int value) {
        Func<int, int> call = n => n + value;
        return call(7);
    }

    [Virtualize]
    public static int VolatileAccess(int value) {
        var state = new VmVolatileState();
        state.Value = value;
        VmVolatileState.Last = value + 1;
        return state.Value + VmVolatileState.Last;
    }

    [Virtualize]
    public static bool LinkedCancellationToken() {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken(true));
        return source.Token.IsCancellationRequested;
    }

    [Virtualize]
    public static bool CancellationRegistration() {
        using var source = new CancellationTokenSource();
        var called = 0;
        using var registration = source.Token.Register(() => called++);
        source.Cancel();
        return called == 1;
    }
}

public sealed class VmGenericBox<T> {
    public List<T> Values = [];
}

public class VmVolatileState {
    public volatile int Value;
    public static volatile int Last;
}

public struct VmMutable {
    public int Value;
    [Virtualize]
    public void Add(int value) => Value += value;
}

public class VmDispatchBase {
    public int Value;
    public static int Last;

    [Virtualize]
    public void Set(int value) {
        Value = value;
        Last = value + 1;
    }

    [Virtualize]
    public virtual int Read() => Value + Last;
}

public class VmDispatchDerived : VmDispatchBase {
    [Virtualize]
    public override int Read() => Value * 3;

    [Virtualize]
    public int ReadBase() => base.Read();
}
