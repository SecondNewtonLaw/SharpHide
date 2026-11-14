namespace SharpHide.Tests;

public static class Sample {
    private static readonly byte[] Payload = { 3, 1, 4, 1, 5, 9, 2, 6 };

    public static int Compute(int seed) =>
        SumBytes(Payload) + Echo(seed) + Inner.Add(seed) + Greeting().Length;

    private static int SumBytes(byte[] data) {
        var total = 0;
        foreach (var value in data)
            total += value;
        return total;
    }

    private static T Echo<T>(T value) => value;

    private static string Greeting() => string.Concat("Sharp", "Hide");

    private static class Inner {
        public static int Add(int value) => value + 100;
    }
}

public static class Boot {
    public static int Ran;
    public static void Initialize() => Ran = 42;
}

// Small public methods used by control-flow tests.
public static class Flowy {
    public static int Sum(int n) {
        var total = 0;
        for (var i = 0; i < n; i++) {
            if ((i & 1) == 0)
                total += i;
            else
                total -= i;
        }
        return total;
    }

    public static int Fib(int n) {
        int a = 0, b = 1;
        for (var k = 0; k < n; k++) {
            var next = a + b;
            a = b;
            b = next;
        }
        return a;
    }

    public static int Classify(int n) {
        if (n < 0)
            return -1;
        if (n == 0)
            return 0;
        if (n % 2 == 0)
            return 2;
        return 1;
    }

    public static int Recur(int n) {
        if (n <= 0)
            return 0;

        return n + Recur(n - 1);
    }
}

public static class ResourceReader {
    public static int ReadFirst() {
        using var stream = typeof(ResourceReader).Assembly.GetManifestResourceStream("data")!;
        return stream.ReadByte();
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public sealed class VirtualizeAttribute : Attribute;

public static class Virtualized {
    [Virtualize]
    public static int Straight(int x) => x * x * 3 + x * 2 - 7;

    [Virtualize]
    public static int PolySum(int n) {
        var total = 0;
        for (var i = 0; i < n; i++) {
            if ((i & 1) == 0)
                total += i * 3;
            else
                total -= i;
        }

        return total;
    }
}
