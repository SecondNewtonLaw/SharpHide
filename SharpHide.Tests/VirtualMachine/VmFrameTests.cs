using SharpHide.Vm;

namespace SharpHide.Tests;

public class VmFrameTests {
    public static ref int Null() => ref System.Runtime.CompilerServices.Unsafe.NullRef<int>();

    [Fact]
    public void Compiler_Bridge_Controls_Operation_Result() {
        var registers = new[] { VmValue.Load(17, typeof(int)) };
        var operation = new VmOperation(typeof(int), -1, (inputs, _) => (int)inputs[0]! + 1);

        Assert.True(VmOperations.Execute(operation, registers, 0, 1));
        Assert.Equal(18, registers[0].Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filter_Integrity_Faults_Are_Distinguished_From_User_Errors(bool integrity) {
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            var operation = new VmOperation(typeof(int), -1,
                (_, _) => integrity ? VmCrash.Fail<object>() : throw new BadImageFormatException());
            var code = new byte[256 + 13];
            for (var index = 0; index < 256; index++)
                code[index] = (byte)index;
            code[256] = VmOpcodes.Operation;
            BitConverter.TryWriteBytes(code.AsSpan(257), 0);
            BitConverter.TryWriteBytes(code.AsSpan(261), 1);
            BitConverter.TryWriteBytes(code.AsSpan(265), 0);
            var region = new VmExceptionRegion {
                TryStart = 256,
                TryEnd = code.Length,
                HandlerStart = code.Length,
                HandlerEnd = code.Length,
                FilterStart = 256,
                Kind = 1,
            };
            using var pins = new VmPins();
            var frame = new VmFrame(code, [typeof(int), operation], Enumerable.Repeat(true, code.Length).ToArray(), [], [], [region], pins, 256, 1,
                false, [typeof(TypedReference)], [-1], VmReference.CreateScope());
            frame.BeginSearch(new InvalidOperationException());
            Assert.True(frame.SearchStep());
            if (integrity)
                Assert.Throws<BadImageFormatException>(() => frame.SearchStep());
            else {
                Assert.True(frame.SearchStep());
                Assert.False(frame.SearchStep());
            }
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }
}
