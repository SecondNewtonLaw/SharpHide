using System.Numerics;

namespace SharpHide.Vm;

public static class VmArithmetic {
    public static VmValue Execute(byte instruction, VmValue left, VmValue right) {
        var unsigned = instruction is >= VmOpcodes.AddUnsignedChecked and <= VmOpcodes.RemainderUnsigned;
        object result;
        if (left.Value is double || right.Value is double)
            result = Apply<double>(instruction, left.Value!, right.Value);
        else if (left.Value is float || right.Value is float)
            result = Apply<float>(instruction, left.Value!, right.Value);
        else if (left.Value is nint || right.Value is nint)
            result = unsigned
                ? (object)Apply<nuint>(instruction, left.Value!, right.Value)
                : Apply<nint>(instruction, left.Value!, right.Value);
        else if (left.Value is long || right.Value is long)
            result = unsigned
                ? (object)Apply<ulong>(instruction, left.Value!, right.Value)
                : Apply<long>(instruction, left.Value!, right.Value);
        else
            result = unsigned
                ? (object)Apply<uint>(instruction, left.Value!, right.Value)
                : Apply<int>(instruction, left.Value!, right.Value);
        return VmValue.Load(result, result.GetType());
    }

    private static T Read<T>(object value) where T : INumber<T> => value switch {
        int number => T.CreateTruncating(number),
        long number => T.CreateTruncating(number),
        nint number => T.CreateTruncating(number),
        float number => T.CreateTruncating(number),
        double number => T.CreateTruncating(number),
        _ => VmCrash.Fail<T>(),
    };

    private static T Apply<T>(byte instruction, object leftValue, object? rightValue) where T : INumber<T> {
        var left = Read<T>(leftValue);
        if (instruction == VmOpcodes.Negate)
            return unchecked(-left);
        var right = Read<T>(rightValue!);
        return instruction switch {
            VmOpcodes.Add => unchecked(left + right),
            VmOpcodes.Subtract => unchecked(left - right),
            VmOpcodes.Multiply => unchecked(left * right),
            VmOpcodes.Divide or VmOpcodes.DivideUnsigned => left / right,
            VmOpcodes.Remainder or VmOpcodes.RemainderUnsigned => left % right,
            VmOpcodes.AddChecked or VmOpcodes.AddUnsignedChecked => checked(left + right),
            VmOpcodes.SubtractChecked or VmOpcodes.SubtractUnsignedChecked => checked(left - right),
            VmOpcodes.MultiplyChecked or VmOpcodes.MultiplyUnsignedChecked => checked(left * right),
            _ => VmCrash.Fail<T>(),
        };
    }
}
