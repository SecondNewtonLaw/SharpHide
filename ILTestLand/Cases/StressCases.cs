using System.Globalization;
using SharpHide.Vendor;

namespace ILTestLand;

internal static class StressCases {
    public const int CheckCount = 3000;

    public static void Run(Action<string, object?, object?> check, int limit) {
        var emitted = 0;
        void Emit(string name, object? actual, object? expected) {
            if (emitted++ < limit)
                check(name, actual, expected);
        }

        for (var input = 0; input < 200; input++) {
            Emit($"stress.vm.arithmetic.{input}", VirtualizedStressCases.Arithmetic(input),
                unchecked((((input * 17) + 31) << 3) ^ (((input * 17) + 31) >> 2)) - input % 7);
            Emit($"stress.vm.branch.{input}", VirtualizedStressCases.Branch(input),
                Enumerable.Range(0, input % 11 + 1).Sum(value => value % 3 == 0 ? value * 2 : value % 3 == 1 ? -value : value + 5));
            Emit($"stress.vm.wide.{input}", VirtualizedStressCases.Wide(input),
                unchecked(((long)input << 37) ^ ((long)input * 1_000_003L) ^ 0x55AA55AA55AA55AAL));
            Emit($"stress.vm.array.{input}", VirtualizedStressCases.Array(input), input * 5 + 30);
            Emit($"stress.vm.text.{input}", VirtualizedStressCases.Text(input),
                string.Format(CultureInfo.InvariantCulture, "{0:X4}:{1}:{2}", input, input % 9, input * input));
            Emit($"stress.vm.exception.{input}", VirtualizedStressCases.Exception(input),
                input % 17 == 0 ? -input : 1200 / (input % 17));
            Emit($"stress.vm.generic.{input}", VirtualizedStressCases.Generic(input), input * 2 + 1);
            Emit($"stress.vm.nullable.{input}", VirtualizedStressCases.Nullable(input), input % 4 == 0 ? -1 : input);
            Emit($"stress.vm.method.{input}", MixedStressCases.Virtualized(input), input * input - input * 3 + 9);
            Emit($"stress.vm.region.{input}", MixedStressCases.Region(input), ((input * 3) ^ 0x55) + 5);
            Emit($"stress.clr.dictionary.{input}", MixedStressCases.Dictionary(input), input + 7);
            Emit($"stress.clr.rejected-region.{input}", MixedStressCases.RejectedRegion(input), input + 11);
            Emit($"stress.clr.decimal.{input}", MixedStressCases.Decimal(input), input / 3m + 1.25m);
            Emit($"stress.clr.sequence.{input}", MixedStressCases.Sequence(input), ExpectedSequence(input));
        }
    }

    private static int ExpectedSequence(int input) {
        var total = 0;
        for (var value = 0; value <= input % 20; value++) {
            if ((value & 1) == 0)
                total += value * value;
        }
        return total;
    }
}

[Virtualize]
internal static class VirtualizedStressCases {
    public static int Arithmetic(int input) {
        var value = unchecked(input * 17 + 31);
        return unchecked(((value << 3) ^ (value >> 2)) - input % 7);
    }

    public static int Branch(int input) {
        var total = 0;
        for (var value = 0; value <= input % 11; value++) {
            total += (value % 3) switch {
                0 => value * 2,
                1 => -value,
                _ => value + 5,
            };
        }
        return total;
    }

    public static long Wide(int input) => unchecked(((long)input << 37) ^
        ((long)input * 1_000_003L) ^ 0x55AA55AA55AA55AAL);

    public static int Array(int input) {
        var values = new int[5];
        for (var index = 0; index < values.Length; index++)
            values[index] = input + index * 3;
        return values.Sum();
    }

    public static string Text(int input) => $"{input:X4}:{input % 9}:{input * input}";

    public static int Exception(int input) {
        try {
            return 1200 / (input % 17);
        }
        catch (DivideByZeroException) {
            return -input;
        }
    }

    public static int Generic(int input) => Echo(input * 2 + 1);

    public static int Nullable(int input) {
        int? value = input % 4 == 0 ? null : input;
        return value ?? -1;
    }

    private static T Echo<T>(T value) => value;

    [DoNotVirtualize]
    public static int ExplicitlyPlain(int input) => input + 1;
}

[ControlFlow(MinBlocks = 2, MinSize = 4, MaxSize = 2400, BogusStates = 8, OpaqueDepth = 5,
    DecryptHashChance = 0.8)]
internal static class MixedStressCases {
    private static int _regionValue;

    [Virtualize]
    public static int Virtualized(int input) => input * input - input * 3 + 9;

    public static int Region(int input) {
        ObfuscationMacros.VMBegin();
        _regionValue = (input * 3) ^ 0x55;
        ObfuscationMacros.VMEnd();
        return _regionValue + 5;
    }

    public static int Dictionary(int input) {
        var values = new Dictionary<int, int> { [input] = input + 7 };
        return values[input];
    }

    [DoNotControlFlow]
    public static int RejectedRegion(int input) {
        try {
            ObfuscationMacros.VMBegin();
            return input + 11;
        }
        finally {
            ObfuscationMacros.VMEnd();
        }
    }

    public static decimal Decimal(int input) => input / 3m + 1.25m;

    public static int Sequence(int input) => Enumerable.Range(0, input % 20 + 1)
        .Where(value => (value & 1) == 0)
        .Select(value => value * value)
        .Sum();
}
