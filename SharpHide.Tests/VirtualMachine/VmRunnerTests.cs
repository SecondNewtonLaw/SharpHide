using SharpHide.Vm;

namespace SharpHide.Tests;

public class VmRunnerTests {
    [Fact]
    public void Operations_Run_Precompiled_Handler() {
        var operation = new VmOperation(typeof(int), -1, (inputs, _) => (int)inputs[0]! + 1);
        var program = Program([
            .. Encode(VmOpcodes.Ldc, 0, 41),
            .. Encode(VmOpcodes.Operation, 0, 1, 1),
            .. Encode(VmOpcodes.Ret),
        ], stack: 1);

        Assert.Equal(42, VmRunner.Run(program, [], [typeof(int), operation]));
    }

    [Fact]
    public void Bytecode_Encryption_Is_Reversible_Without_Growth() {
        var plain = Enumerable.Range(0, 513).Select(index => (byte)index).ToArray();
        var encrypted = VmRunner.Encrypt(plain, 0x123456789ABCDE, 0x23456789ABCDEF, 32);

        Assert.Equal(plain.Length, encrypted.Length);
        Assert.NotEqual(plain, encrypted);
        Assert.Equal(plain, VmRunner.Encrypt(encrypted, 0x123456789ABCDE, 0x23456789ABCDEF, 32));
    }

    [Fact]
    public void Aes_Payload_Is_Authenticated() {
        var plain = Enumerable.Range(0, 513).Select(index => (byte)index).ToArray();
        var payload = Convert.FromBase64String(VmRunner.SealAesVersioned(plain, 11, 17, 23));
        Assert.Equal(new byte[] { (byte)'S', (byte)'H', (byte)'B', (byte)'1' }, payload[^42..^38]);
        Assert.Equal(2, payload[^37]);

        var encrypted = VmRunner.Encrypt(Convert.FromBase64String(Program([
            .. Encode(VmOpcodes.Ldc, 0, 42),
            .. Encode(VmOpcodes.Ret),
        ], stack: 1)), 11, 17, 32);
        var executable = VmRunner.SealAesVersioned(encrypted, 11, 17, 23);
        using var executionPins = new VmPins();
        var frame = VmRunner.CreateFrame(executable, [], [typeof(int)], executionPins,
            [typeof(TypedReference)], [-1], 0, 11, 17, 32, 23);
        while (frame.Ready())
            frame.Step();
        Assert.Equal(42, frame.ValueResult());

        var tampered = (byte[])payload.Clone();
        tampered[0] ^= 1;
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            static void Reject(string encoded, long key0 = 11) {
                using var pins = new VmPins();
                Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(encoded, [], [], pins,
                    [typeof(TypedReference)], [], 0, key0, 17, 0, 23));
            }

            Reject(Convert.ToBase64String(tampered));
            Reject(Convert.ToBase64String(payload), 12);
            var nonce = (byte[])payload.Clone();
            nonce[^30] ^= 1;
            Reject(Convert.ToBase64String(nonce));
            var tag = (byte[])payload.Clone();
            tag[^1] ^= 1;
            Reject(Convert.ToBase64String(tag));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Oversized_Payload_Fails_Before_Decoding() {
        var payload = Convert.ToBase64String(new byte[16_777_216 + 47]);
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            Assert.Throws<BadImageFormatException>(() => VmRunner.Run(payload, [], [typeof(int), typeof(int)]));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Frame_Setup_Faults_Use_Vm_Integrity_Path() {
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            Assert.Throws<BadImageFormatException>(() => VmRunner.Run(Program([VmOpcodes.Ret]), null!, [typeof(int)]));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Native_Banks_Must_Match_Declared_Types() {
        var operation = new VmOperation(typeof(int), 1, (_, _) => 1);
        var program = Program(Encode(VmOpcodes.Operation, 0, 1, 0) , stack: 1);
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(program, [], [typeof(int), operation], pins,
                [typeof(TypedReference), typeof(long)], [-1], 0, 0, 0, 0));
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(Program([VmOpcodes.Ret]), [], [typeof(int)], pins,
                [typeof(TypedReference), typeof(long)], [1], 0, 0, 0, 0));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Keyed_Scrambling_Is_Per_Key_Reversible_Without_Growth() {
        var plain = Enumerable.Range(0, 513).Select(index => (byte)index).ToArray();
        var first = VmRunner.Scramble(plain, 0x123456789ABCDE);
        var second = VmRunner.Scramble(plain, 0x23456789ABCDEF);

        Assert.Equal(plain.Length, first.Length);
        Assert.NotEqual(first, second);
        Assert.Equal(plain, VmRunner.Scramble(first, 0x123456789ABCDE));
    }

    private static string Program(byte[] body, int stack = 2, int locals = 0, int args = 0) {
        var bytes = new List<byte>(Enumerable.Range(0, 256).Select(i => (byte)i));
        foreach (var value in new[] { VmOpcodes.Magic, stack, locals, args, 0 })
            for (var shift = 0; shift < 32; shift += 8)
                bytes.Add((byte)(value >> shift));

        bytes.AddRange(body);
        return Convert.ToBase64String(VmRunner.Scramble(bytes.ToArray()));
    }

    private static byte[] Encode(byte opcode, int register = 0, params int[] operands) {
        var bytes = new List<byte> { opcode };
        foreach (var operand in operands.Prepend(register))
            for (var shift = 0; shift < 32; shift += 8)
                bytes.Add((byte)(operand >> shift));
        return bytes.ToArray();
    }

    public static IEnumerable<object[]> InvalidPrograms() {
        yield return [Program([255])];
        yield return [Program([VmOpcodes.Ldc, 1])];
        yield return [Program(Encode(VmOpcodes.Ldloc, 0, 0))];
        yield return [Program(Encode(VmOpcodes.Ldarg, 0, 0))];
        yield return [Program(Encode(VmOpcodes.Br, 0, 1))];
        yield return [Program(Encode(VmOpcodes.Br, 0, -1))];
        yield return [Program(Encode(VmOpcodes.Switch, 0, int.MaxValue))];
        yield return [Program(Encode(VmOpcodes.Switch, 0, -1))];
        yield return [Program(Encode(VmOpcodes.Operation, 0, -1, 0))];
        yield return [Program(Encode(VmOpcodes.Operation, 1, 0, 2))];
        yield return [Program(Encode(VmOpcodes.Operation, 0, 0, 0))];
        yield return [Program(Encode(VmOpcodes.Operation, 0, int.MaxValue, 0))];
        yield return [Program(Encode(VmOpcodes.Endfinally))];
        yield return [Program(Encode(VmOpcodes.Endfilter))];
        yield return [Program(Encode(VmOpcodes.Rethrow))];
        yield return [Program(Encode(VmOpcodes.Operation, 0, 0))];
        yield return [Program([.. Encode(VmOpcodes.Operation, 0, 0, 0), .. Encode(VmOpcodes.Ret)])];
        yield return [Program([VmOpcodes.Ret])];
        yield return [Program(Encode(VmOpcodes.Ldc, 0, 1))];
        yield return [Program(Encode(VmOpcodes.Dup), stack: 1)];
        yield return [Program(Encode(VmOpcodes.Ldc, -1, 1))];
        yield return [Program(Encode(VmOpcodes.BitAnd, 1))];
        yield return [Program(Encode(VmOpcodes.BitNot, -1))];
        yield return [Program([VmOpcodes.Ret], stack: -1)];
        yield return [Program([VmOpcodes.Ret], stack: 65536)];
        yield return [Program([VmOpcodes.Ret], locals: -1)];
        yield return [Program([VmOpcodes.Ret], args: 1)];
        yield return [Convert.ToBase64String(new byte[256])];
        var wrongVersion = VmRunner.Scramble(Convert.FromBase64String(Program([VmOpcodes.Ret])));
        wrongVersion[256] ^= 1;
        yield return [Convert.ToBase64String(VmRunner.Scramble(wrongVersion))];
    }

    [Theory]
    [MemberData(nameof(InvalidPrograms))]
    public void Malformed_Programs_Fail_Explicitly(string program) {
        Assert.Throws<BadImageFormatException>(() => VmRunner.Run(program, [], [typeof(int), typeof(int)]));
    }

    [Fact]
    public void Register_Operands_Do_Not_Depend_On_Execution_Order() {
        var program = Program([
            .. Encode(VmOpcodes.Ldc, 2, 42),
            .. Encode(VmOpcodes.Ldc, 0, 10),
            .. Encode(VmOpcodes.Ret, 2),
        ], stack: 3);
        Assert.Equal(42, VmRunner.Run(program, [], [typeof(int)]));
    }

    [Fact]
    public void Zero_Case_Switch_Falls_Through() {
        var program = Program([
            .. Encode(VmOpcodes.Ldc, 0, -1),
            .. Encode(VmOpcodes.Switch, 0, 0),
            .. Encode(VmOpcodes.Ldc, 0, 42),
            .. Encode(VmOpcodes.Ret),
        ]);
        Assert.Equal(42, VmRunner.Run(program, [], [typeof(int)]));
    }

    [Fact]
    public void Locals_Start_At_Zero_And_Do_Not_Leak_Between_Calls() {
        var program = Program([
            .. Encode(VmOpcodes.Ldloc, 0, 0),
            .. Encode(VmOpcodes.Ldc, 1, 1),
            .. Encode(VmOpcodes.Add),
            .. Encode(VmOpcodes.Dup),
            .. Encode(VmOpcodes.Stloc, 1, 0),
            .. Encode(VmOpcodes.Ret),
        ], locals: 1);
        Parallel.For(0, 100, _ => Assert.Equal(1, VmRunner.Run(program, [], [typeof(int), typeof(int)])));
    }

    [Fact]
    public void Null_Managed_References_Are_False() {
        Assert.False(VmValue.Load(VmReference.Pointer(IntPtr.Zero, typeof(int)), typeof(int).MakeByRefType()).IsTrue());
    }
}
