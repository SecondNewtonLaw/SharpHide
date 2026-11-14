using System.Numerics;

namespace SharpHide.Vm;

public static class VmBitwise {
    public static VmValue Execute(byte instruction, VmValue left, VmValue right) => left.Value switch {
        int value => new VmValue(Apply(instruction, value, right), typeof(int)),
        long value => new VmValue(Apply(instruction, value, right), typeof(long)),
        nint value => new VmValue(Apply(instruction, value, right), typeof(nint)),
        _ => VmCrash.Fail<VmValue>(),
    };

    private static T Apply<T>(byte instruction, T left, VmValue right) where T : IBinaryInteger<T> {
        if (instruction == VmOpcodes.BitNot)
            return ~left;
        if (instruction is VmOpcodes.ShiftLeft or VmOpcodes.ShiftRight or VmOpcodes.ShiftRightUnsigned) {
            var count = right.Value switch {
                int value => value,
                nint value => unchecked((int)value),
                _ => VmCrash.Fail<int>(),
            };
            return instruction switch {
                VmOpcodes.ShiftLeft => left << count,
                VmOpcodes.ShiftRight => left >> count,
                _ => left >>> count,
            };
        }
        var operand = right.Value switch {
            int value => T.CreateTruncating(value),
            long value => T.CreateTruncating(value),
            nint value => T.CreateTruncating(value),
            _ => VmCrash.Fail<T>(),
        };
        return instruction switch {
            VmOpcodes.BitAnd => left & operand,
            VmOpcodes.BitOr => left | operand,
            VmOpcodes.BitXor => left ^ operand,
            _ => VmCrash.Fail<T>(),
        };
    }
}
