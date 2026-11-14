namespace SharpHide.Tests;

public static class VmConstructorSamples {
    public static string Events = "";

    [Virtualize]
    public static string Run(int value) {
        Events = "";
        try {
            var instance = new VmConstructed(value);
            var pair = new VmConstructedPair<string>(instance.Text, value);
            return instance.Value + ":" + pair.Value + ":" + pair.Count + ":" + Events;
        }
        catch (ArgumentOutOfRangeException) {
            return "range:" + Events;
        }
        catch (OverflowException) {
            return "overflow:" + Events;
        }
    }
}

public class VmConstructorBase {
    public readonly string Text;

    [Virtualize]
    protected VmConstructorBase(int value) {
        VmConstructorSamples.Events += "base;";
        Text = value + ":" + Read();
    }

    protected virtual int Read() => 0;
}

public sealed class VmConstructed : VmConstructorBase {
    private readonly int _initial = 11;
    public readonly int Value;

    [Virtualize]
    public VmConstructed(int value) : this(value, 2) {
        VmConstructorSamples.Events += "outer;";
    }

    [Virtualize]
    private VmConstructed(int value, int offset) : base(checked(value * 3)) {
        try {
            VmConstructorSamples.Events += "inner;";
            Value = value + offset;
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
        }
        finally {
            VmConstructorSamples.Events += "finally;";
        }
    }

    protected override int Read() => _initial;
}

public readonly struct VmConstructedPair<T> {
    public readonly T Value;
    public readonly int Count;

    [Virtualize]
    public VmConstructedPair(T value, int count) {
        Value = value;
        Count = count;
        VmConstructorSamples.Events += "struct;";
    }
}
