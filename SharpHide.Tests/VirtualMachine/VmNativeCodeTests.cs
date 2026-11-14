using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using SharpHide.Vm;

namespace SharpHide.Tests;

public class VmNativeCodeTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Machine_Code_Matches_Clr_Integer_Operations(bool wide) {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture is not (Architecture.X86 or Architecture.X64) ||
            wide && RuntimeInformation.ProcessArchitecture != Architecture.X64)
            return;
        var operations = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(OpCode)).Select(f => (OpCode)f.GetValue(null)!).Where(VmNativeCode.IsSupported);
        long[] values = wide
            ? [long.MinValue, long.MinValue + 1, int.MinValue, -65, -1, 0, 1, 31, 32, 63, 64, 65, int.MaxValue, long.MaxValue]
            : [int.MinValue, int.MinValue + 1, -65, -1, 0, 1, 31, 32, 63, 64, 65, int.MaxValue];
        foreach (var op in operations)
            foreach (var first in values)
                foreach (var second in values) {
                    var unary = op == OpCodes.Neg || op == OpCodes.Not;
                    var shift = op == OpCodes.Shl || op == OpCodes.Shr || op == OpCodes.Shr_Un;
                    var inputs = new VmValue[unary ? 1 : 2];
                    inputs[0] = wide ? VmValue.Load(first, typeof(long)) : VmValue.Load((int)first, typeof(int));
                    if (!unary)
                        inputs[1] = wide && !shift ? VmValue.Load(second, typeof(long)) : VmValue.Load((int)second, typeof(int));
                    var expected = inputs.ToArray();
                    var sp = expected.Length;
                    VmOperations.Execute(op, null, expected, ref sp);
                    Assert.True(VmNativeCode.TryExecute(op, inputs, out var actual));
                    Assert.Equal(expected[0].Type, actual.Type);
                    Assert.Equal(expected[0].Value, actual.Value);
                }
    }
}
