using System.Reflection;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;
using SharpHide.Vm;
using Emit = System.Reflection.Emit;

namespace SharpHide.Tests;

public class VirtualizationTests {
    [Fact]
    public void Nested_Generic_Signatures_Substitute_Recursively() {
        using var module = NewTarget();
        var source = new GenericInstSig(new ClassSig(module.CorLibTypes.GetTypeRef("System.Collections.Generic", "List`1")),
            new GenericVar(0));
        var signature = new ByRefSig(new SZArraySig(source));
        var resolved = VmSignatureResolver.Substitute(signature, [module.CorLibTypes.Int32], null);
        var expected = new ByRefSig(new SZArraySig(new GenericInstSig(
            new ClassSig(module.CorLibTypes.GetTypeRef("System.Collections.Generic", "List`1")),
            module.CorLibTypes.Int32)));
        var function = new FnPtrSig(MethodSig.CreateStatic(new GenericVar(0), new GenericVar(0)));
        var resolvedFunction = VmSignatureResolver.Substitute(function, [module.CorLibTypes.Int32], null);

        Assert.True(new SigComparer().Equals(resolved, expected));
        var functionSignature = ((FnPtrSig)resolvedFunction).MethodSig;
        Assert.Equal(ElementType.I4, functionSignature.RetType.ElementType);
        Assert.Equal(ElementType.I4, functionSignature.Params[0].ElementType);
    }

    [Fact]
    public void Native_Banks_Do_Not_Conflate_Closed_Generic_Types() {
        using var module = NewTarget();
        var method = NewMethod(module, "NativeBanks");
        var nativeTypes = new List<TypeSig> {
            module.Import(typeof(TypedReference)).ToTypeSig(),
            module.Import(typeof(Span<int>)).ToTypeSig(),
            module.Import(typeof(Span<byte>)).ToTypeSig(),
        };

        Assert.Equal(1, VmTypeLayout.Bank(method, nativeTypes[1], nativeTypes));
        Assert.Equal(2, VmTypeLayout.Bank(method, nativeTypes[2], nativeTypes));
    }

    [Fact]
    public void VmReference_Creates_Isolated_Scopes() {
        Assert.NotSame(VmReference.CreateScope(), VmReference.CreateScope());
    }

    [Fact]
    public void Generated_Host_Binds_Scope_And_Registrations_To_Same_Runtime_Clone() {
        using var module = NewTarget();
        module.Inject(typeof(VmTypedSamples));
        var type = module.GetTypes().Single(candidate => candidate.FullName == typeof(VmTypedSamples).FullName);
        var method = type.Methods.Single(candidate => candidate.Name == nameof(VmTypedSamples.ArgumentAddress));
        var context = new ObfuscationContext(module, new Random(127));
        Assert.Equal(1, VirtualizationProtection.Virtualize(context, [method]));

        var host = type.Methods.Single(candidate => candidate.Name == method.Name + "$vmhost");
        var scope = method.Body.Instructions.Select(instruction => instruction.Operand as IMethod)
            .Single(target => target?.Name.String == nameof(VmReference.CreateScope))!;
        var registrations = method.Body.Instructions.Select(instruction => instruction.Operand as IMethod)
            .Where(target => target?.Name.String is nameof(VmReference.Register) or nameof(VmReference.RegisterNative))
            .ToArray();
        Assert.NotEmpty(registrations);
        var scopeType = scope.DeclaringType!.ResolveTypeDef();
        Assert.All(registrations, target => Assert.Same(scopeType, target!.DeclaringType.ResolveTypeDef()));
        var register = scopeType!.Methods.Single(candidate => candidate.Name == nameof(VmReference.Register) && candidate.MethodSig.Params.Count == 4);
        Assert.All(register.Body.Instructions.Select(instruction => instruction.Operand as IField)
                .Where(field => field?.Name.String == "DefaultScope"),
            field => Assert.Same(scopeType, field!.DeclaringType.ResolveTypeDef()));
    }

    private static ModuleDef NewTarget() {
        var name = "Vm_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) {
            Kind = ModuleKind.Dll,
            RuntimeVersion = "v4.0.30319",
        };
        var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
        assembly.Modules.Add(module);
        return module;
    }

    private static Assembly Write(ModuleDef module) {
        using var stream = new MemoryStream();
        new ModuleWriter(module, new ModuleWriterOptions(module)).Write(stream);
        return Assembly.Load(stream.ToArray());
    }

    [Theory]
    [InlineData(0, 4000, false)]
    [InlineData(0.5, 4000, false)]
    [InlineData(1, 4000, false)]
    [InlineData(1, 4000, true)]
    [InlineData(1, 0, false)]
    public void String_Handlers_Can_Be_Independently_Virtualized(double chance, int maximumSize, bool rename) {
        using var module = NewTarget();
        var type = new TypeDefUser("Example", "Strings", module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = dnlib.DotNet.TypeAttributes.Public,
        };
        module.Types.Add(type);
        for (var index = 0; index < 16; index++) {
            var method = new MethodDefUser("Read" + index, MethodSig.CreateStatic(module.CorLibTypes.String),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) {
                Body = new CilBody(),
            };
            method.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction("text-\u03bb-" + index));
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            type.Methods.Add(method);
        }
        var context = new ObfuscationContext(module, new Random(2026));
        var protection = new StringEncryptionProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["handlers"] = 8,
            ["junk-chance"] = 0,
            ["virtualize-chance"] = chance,
            ["virtualize-max-size"] = maximumSize,
        };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        protection.Initialize(context);

        var handlers = context.Runtime.SelectMany(container => container.Methods)
            .Where(method => method.Name == "Decrypt").ToArray();
        Assert.Equal(chance > 0 && maximumSize > 0 ? 9 : 8, handlers.Length);
        var virtualized = handlers.Count(method => method.Body.Instructions.Any(instruction =>
            instruction.Operand is IMethod target && target.DeclaringType.FullName == typeof(VmRunner).FullName &&
            target.Name == nameof(VmRunner.CreateFrame)));
        if (chance == 0 || maximumSize == 0) {
            Assert.Equal(0, virtualized);
            Assert.DoesNotContain(module.GetTypes(), definition => definition.FullName == typeof(VmRunner).FullName);
        }
        else if (chance == 1)
            Assert.Equal(8, virtualized);
        else
            Assert.InRange(virtualized, 1, 7);

        Assert.DoesNotContain(context.VirtualMachineTypes.SelectMany(container => container.Methods)
            .Where(method => method.HasBody).SelectMany(method => method.Body.Instructions),
            instruction => instruction.OpCode == OpCodes.Ldstr &&
                (string)instruction.Operand == "Invalid VM frame.");

        if (rename) {
            var enumFields = context.VirtualMachineTypes.Where(container => container.IsEnum)
                .SelectMany(container => container.Fields).Where(field => field.IsLiteral)
                .ToDictionary(field => field, field => field.Name.String);
            new RenameInjectedProtection().Initialize(context);
            Assert.All(enumFields, entry => Assert.NotEqual(entry.Value, entry.Key.Name.String));
        }
        var loaded = Write(module).GetType(type.FullName)!;
        for (var index = 0; index < 16; index++)
            Assert.Equal("text-\u03bb-" + index, loaded.GetMethod("Read" + index)!.Invoke(null, null));
    }

    [Theory]
    [InlineData("handlers", 0d)]
    [InlineData("handlers", 33d)]
    [InlineData("junk-chance", -0.1d)]
    [InlineData("virtualize-chance", 1.1d)]
    [InlineData("virtualize-max-size", 100001d)]
    public void String_Encryption_Rejects_Invalid_Growth_Options(string name, double value) {
        using var module = NewTarget();
        var protection = new StringEncryptionProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { [name] = value };
        var context = new ObfuscationContext(module, new Random(2026));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
        Assert.Empty(context.Runtime);
    }

    [Theory]
    [InlineData("handlers", 0d)]
    [InlineData("handlers", 33d)]
    [InlineData("max-size", 0d)]
    [InlineData("max-size", 100001d)]
    public void Number_Encryption_Rejects_Invalid_Growth_Options(string name, double value) {
        using var module = NewTarget();
        var protection = new NumberEncryptionProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { [name] = value };
        var context = new ObfuscationContext(module, new Random(2026));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
        Assert.Empty(context.Runtime);
    }

    [Fact]
    public void Virtualized_Method_Runs_As_Bytecode_And_Matches() {
        var module = NewTarget();
        module.Inject(typeof(Virtualized));

        var context = new ObfuscationContext(module, new Random(1));
        new VirtualizationProtection().Initialize(context);
        var methods = module.GetTypes().SelectMany(t => t.Methods).ToList();
        Assert.DoesNotContain(methods.Where(method => method.HasBody).SelectMany(method => method.Body.Instructions),
            instruction => instruction.Operand is IMethod target &&
                target.Name.String is "get_StackBehaviourPop" or "get_StackBehaviourPush");
        Assert.DoesNotContain(methods.Where(method => method.HasBody &&
                (method.Name.String.Contains("$vmhost", StringComparison.Ordinal) ||
                 method.Name.String.Contains("$vmbridge", StringComparison.Ordinal)))
            .SelectMany(method => method.Body.Instructions),
            instruction => instruction.Operand is IMethod target &&
                target.Name.String is nameof(Type.MakeByRefType) or nameof(Type.MakePointerType));
        var polySum = methods.First(m => m.Name == nameof(Virtualized.PolySum));
        var straightDef = methods.First(m => m.Name == nameof(Virtualized.Straight));
        Assert.Contains(polySum.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        Assert.DoesNotContain(polySum.Body.Instructions, i => i.OpCode == OpCodes.Mul);

        byte[] Remap(MethodDef m) =>
            VmRunner.Scramble(Convert.FromBase64String(
                (string)m.Body.Instructions.First(i => i.OpCode == OpCodes.Ldstr).Operand))[..256];
        Assert.NotEqual(Remap(polySum), Remap(straightDef));
        Assert.Equal(256, Remap(polySum).Distinct().Count());

        var type = Write(module).GetType(typeof(Virtualized).FullName!)!;
        var straight = type.GetMethod(nameof(Virtualized.Straight))!;
        var poly = type.GetMethod(nameof(Virtualized.PolySum))!;
        for (var n = -2; n <= 30; n++) {
            Assert.Equal(Virtualized.Straight(n), straight.Invoke(null, new object[] { n }));
            Assert.Equal(Virtualized.PolySum(n), poly.Invoke(null, new object[] { n }));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Runtime_Integrity_Errors_Are_Selectable(bool enabled) {
        using var module = NewTarget();
        var method = NewMethod(module, "RuntimeErrors");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["runtime-errors"] = enabled ? 1 : 0,
        };
        var context = new ObfuscationContext(module, new Random(1));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        protection.Initialize(context);

        var assignment = method.Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Stsfld &&
            instruction.Operand is IField field && field.DeclaringType.FullName == typeof(VmCrash).FullName &&
            field.Name == nameof(VmCrash.Diagnostics));
        var value = method.Body.Instructions[method.Body.Instructions.IndexOf(assignment) - 1];
        Assert.Equal(enabled ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, value.OpCode);
    }

    [Fact]
    public void Runtime_Integrity_Errors_Default_To_FailFast() {
        using var module = NewTarget();
        var method = NewMethod(module, "RuntimeErrorsDefault");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(1)));

        var assignment = method.Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Stsfld &&
            instruction.Operand is IField field && field.DeclaringType.FullName == typeof(VmCrash).FullName &&
            field.Name == nameof(VmCrash.Diagnostics));
        Assert.Equal(OpCodes.Ldc_I4_0, method.Body.Instructions[method.Body.Instructions.IndexOf(assignment) - 1].OpCode);
    }

    [Theory]
    [InlineData("bytecode-rounds", -1d)]
    [InlineData("bytecode-rounds", 65d)]
    [InlineData("instances", 0d)]
    [InlineData("instances", 17d)]
    [InlineData("instances", 1.5d)]
    [InlineData("payload-auth", 2d)]
    [InlineData("payload-algorithm", 0d)]
    [InlineData("payload-algorithm", 3d)]
    [InlineData("payload-algorithm", 1.5d)]
    [InlineData("regions-strict", -1d)]
    [InlineData("payload-max-size", 0d)]
    [InlineData("payload-max-size", 16_777_217d)]
    [InlineData("payload-max-size", 1.5d)]
    public void Invalid_Virtualization_Bounds_Are_Rejected(string name, double value) {
        using var module = NewTarget();
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { [name] = value };
        var context = new ObfuscationContext(module, new Random(2));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }

    [Fact]
    public void Oversized_Payload_Retains_Original_Method() {
        using var module = NewTarget();
        var method = NewMethod(module, "PayloadLimit");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["payload-max-size"] = 1 };
        var context = new ObfuscationContext(module, new Random(5));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        protection.Initialize(context);

        Assert.DoesNotContain(method.Body.Instructions, instruction => instruction.Operand is IMethod target &&
            target.Name == nameof(VmRunner.CreateFrame));
    }

    [Fact]
    public void Payload_Authentication_Uses_Marked_Tagged_Envelope() {
        using var module = NewTarget();
        var method = NewMethod(module, "AuthenticatedPayload");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["payload-auth"] = 1 };
        var context = new ObfuscationContext(module, new Random(3));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        protection.Initialize(context);
        var payload = method.Body.Instructions.Select(instruction => instruction.Operand as string)
            .First(value => value is not null);
        var bytes = Convert.FromBase64String(payload!);
        Assert.Equal("SHB1", Encoding.ASCII.GetString(bytes[^46..^42]));
        Assert.Equal(1, bytes[^42]);
        Assert.Equal(1, bytes[^41]);
        Assert.NotEqual(0L, BitConverter.ToInt64(bytes, bytes.Length - 40));
        Assert.Equal(32, bytes[^32..].Length);
    }

    [Fact]
    public void Payload_Authentication_Algorithm_Can_Select_AesGcm() {
        using var module = NewTarget();
        var method = NewMethod(module, "AesPayload");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["payload-auth"] = 1,
            ["payload-algorithm"] = 2,
        };
        var context = new ObfuscationContext(module, new Random(4));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        protection.Initialize(context);

        var payload = method.Body.Instructions.Select(instruction => instruction.Operand as string)
            .First(value => value is not null);
        var bytes = Convert.FromBase64String(payload!);
        Assert.Equal("SHB1", Encoding.ASCII.GetString(bytes[^42..^38]));
        Assert.Equal(2, bytes[^37]);
    }

    [Fact]
    public void Tampered_Authenticated_Payload_Is_Rejected() {
        var payload = Convert.FromBase64String(VmRunner.Seal([], 11, 17));
        payload[0] ^= 1;
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(Convert.ToBase64String(payload), [], [], pins,
                [typeof(TypedReference)], [], 0, 11, 17, 0));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Authenticated_Payload_Mutations_Are_Rejected(int mutation) {
        var payload = Convert.FromBase64String(VmRunner.Seal([1], 11, 17));
        switch (mutation) {
            case 0:
                payload[^1] ^= 1;
                break;
            case 1:
                payload = payload[..^1];
                break;
            case 2:
                break;
        }
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(Convert.ToBase64String(payload), [], [], pins,
                [typeof(TypedReference)], [], 0, mutation == 2 ? 12 : 11, 17, 0));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Authenticated_Payload_Binding_Prevents_Replay() {
        var payload = VmRunner.SealVersioned([1], 11, 17, 23);
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(payload, [], [], pins,
                [typeof(TypedReference)], [], 0, 11, 17, 0, 24));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Versioned_Authenticated_Header_Is_Authenticated() {
        var bytes = Convert.FromBase64String(VmRunner.SealVersioned([1], 11, 17, 23));
        bytes[^41] = 2;
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame(Convert.ToBase64String(bytes), [], [], pins,
                [typeof(TypedReference)], [], 0, 11, 17, 0, 23));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Invalid_Base64_Uses_Vm_Integrity_Path() {
        var previous = VmCrash.Diagnostics;
        VmCrash.Diagnostics = true;
        try {
            using var pins = new VmPins();
            Assert.Throws<BadImageFormatException>(() => VmRunner.CreateFrame("!", [], [], pins,
                [typeof(TypedReference)], [], 0, 0, 0, 0));
        }
        finally {
            VmCrash.Diagnostics = previous;
        }
    }

    [Fact]
    public void Vm_Instances_And_Opcode_Scrambling_Are_Selectable() {
        using var module = NewTarget();
        var methods = Enumerable.Range(0, 4).Select(index => {
            var method = NewMethod(module, "Instance" + index);
            method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(100 + index));
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            return method;
        }).ToArray();
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["instances"] = 3,
            ["opcode-scrambling"] = 0,
            ["bytecode-scrambling"] = 0,
            ["bytecode-rounds"] = 0,
        };
        var context = new ObfuscationContext(module, new Random(81));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        Assert.Equal(4, VirtualizationProtection.Virtualize(context, methods));
        var runners = methods.Select(method => method.Body.Instructions
            .Select(instruction => instruction.Operand as IMethod)
            .Single(target => target?.Name == nameof(VmRunner.CreateFrame))!.DeclaringType).ToArray();
        Assert.Equal(3, runners.Distinct().Count());
        foreach (var method in methods) {
            var program = VmRunner.Scramble(Convert.FromBase64String(
                (string)method.Body.Instructions.First(instruction => instruction.OpCode == OpCodes.Ldstr).Operand));
            Assert.Equal(Enumerable.Range(0, 256).Select(index => (byte)index), program[..256]);
        }
        var loaded = Write(module).GetType("Tests.Program")!;
        for (var index = 0; index < methods.Length; index++)
            Assert.Equal(100 + index, loaded.GetMethod(methods[index].Name)!.Invoke(null, null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(77)]
    [InlineData(2026)]
    public void Integer_Operations_Match_Clr_Results_And_Exceptions(int seed) {
        using var module = NewTarget();
        module.Inject(typeof(VmSamples));
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(seed)));

        var definitions = module.GetTypes().Single(t => t.FullName == typeof(VmSamples).FullName);
        foreach (var method in definitions.Methods.Where(m => m.IsPublic && !m.IsConstructor))
            Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod callee && callee.Name == nameof(VmRunner.CreateFrame));

        Assert.DoesNotContain(module.GetAssemblyRefs(), a => a.Name == "SharpHide.Vm");
        var loaded = Write(module).GetType(typeof(VmSamples).FullName!)!;
        int[] values = [int.MinValue, int.MinValue + 1, -65536, -32769, -129, -1, 0, 1, 2, 31, 32, 127, 128, 255, 256, 32767, 65535, int.MaxValue];
        foreach (var original in typeof(VmSamples).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            var actual = loaded.GetMethod(original.Name)!;
            var parameters = original.GetParameters();
            if (parameters.Length > 2) {
                foreach (var n in new[] { 0, 1, 5, 30 })
                    AssertSameResult(original, actual, [n, 3, 7, 9, 111, -500]);
                continue;
            }

            foreach (var a in values)
                foreach (var b in parameters.Length == 2 ? values : new[] { 0 }) {
                    var args = new object[parameters.Length];
                    for (var i = 0; i < args.Length; i++) {
                        var value = i == 0 ? a : b;
                        args[i] = parameters[i].ParameterType == typeof(uint) ? (object)unchecked((uint)value) : value;
                    }

                    AssertSameResult(original, actual, args);
                }
        }
    }

    [Theory]
    [InlineData(nameof(VmSamples.Shift), typeof(VmBitwise), new byte[] { VmOpcodes.BitAnd, VmOpcodes.BitXor, VmOpcodes.ShiftLeft, VmOpcodes.ShiftRight, VmOpcodes.ShiftRightUnsigned })]
    [InlineData(nameof(VmSamples.Add), typeof(VmArithmetic), new byte[] { VmOpcodes.Add })]
    [InlineData(nameof(VmSamples.Subtract), typeof(VmArithmetic), new byte[] { VmOpcodes.Subtract })]
    [InlineData(nameof(VmSamples.Multiply), typeof(VmArithmetic), new byte[] { VmOpcodes.Multiply })]
    [InlineData(nameof(VmSamples.Negate), typeof(VmArithmetic), new byte[] { VmOpcodes.Negate })]
    [InlineData(nameof(VmSamples.Divide), typeof(VmArithmetic), new byte[] { VmOpcodes.Divide })]
    [InlineData(nameof(VmSamples.Remainder), typeof(VmArithmetic), new byte[] { VmOpcodes.Remainder })]
    [InlineData(nameof(VmSamples.CheckedAdd), typeof(VmArithmetic), new byte[] { VmOpcodes.AddChecked })]
    [InlineData(nameof(VmSamples.CheckedSub), typeof(VmArithmetic), new byte[] { VmOpcodes.SubtractChecked })]
    [InlineData(nameof(VmSamples.CheckedMul), typeof(VmArithmetic), new byte[] { VmOpcodes.MultiplyChecked })]
    [InlineData(nameof(VmSamples.CheckedUnsignedAdd), typeof(VmArithmetic), new byte[] { VmOpcodes.AddUnsignedChecked })]
    [InlineData(nameof(VmSamples.CheckedUnsignedSub), typeof(VmArithmetic), new byte[] { VmOpcodes.SubtractUnsignedChecked })]
    [InlineData(nameof(VmSamples.CheckedUnsignedMul), typeof(VmArithmetic), new byte[] { VmOpcodes.MultiplyUnsignedChecked })]
    [InlineData(nameof(VmSamples.UnsignedDivide), typeof(VmArithmetic), new byte[] { VmOpcodes.DivideUnsigned })]
    [InlineData(nameof(VmSamples.UnsignedRemainder), typeof(VmArithmetic), new byte[] { VmOpcodes.RemainderUnsigned })]
    public void Primitive_Instructions_Use_Explicit_Registers_And_Precompiled_Handlers(string methodName, Type handlerType, byte[] expectedOpcodes) {
        using var module = NewTarget();
        module.Inject(typeof(VmSamples));
        var context = new ObfuscationContext(module, new Random(37));
        new VirtualizationProtection().Initialize(context);
        var method = module.GetTypes().SelectMany(type => type.Methods)
            .Single(candidate => candidate.Name == methodName);
        var code = VmRunner.Scramble(Convert.FromBase64String(
            (string)method.Body.Instructions.First(instruction => instruction.OpCode == OpCodes.Ldstr).Operand));
        var position = VmOpcodes.HeaderSize;
        var handlers = new HashSet<byte>();
        while (position < code.Length) {
            var opcode = code[code[position++]];
            var register = BitConverter.ToInt32(code, position);
            position += 4;
            Assert.True(opcode is VmOpcodes.Ldarg or VmOpcodes.Ldc or VmOpcodes.Ret || expectedOpcodes.Contains(opcode));
            if (opcode == VmOpcodes.Ret)
                continue;
            if (opcode is not (VmOpcodes.Ldarg or VmOpcodes.Ldc)) {
                handlers.Add(opcode);
                Assert.InRange(register, 0, 2);
            }
            else
                position += 4;
        }
        Assert.Equal(code.Length, position);
        Assert.Equal(expectedOpcodes.Order(), handlers.Order());
        var definition = context.VirtualMachineTypes.Single(type => type.FullName == handlerType.FullName);
        Assert.DoesNotContain(definition.Methods.Where(candidate => candidate.HasBody)
            .SelectMany(candidate => candidate.Body.Instructions), instruction =>
                instruction.Operand is IMethod target && target.DeclaringType.FullName == typeof(Emit.DynamicMethod).FullName);
    }

    [Theory]
    [InlineData(false, 0L)]
    [InlineData(false, long.MinValue)]
    [InlineData(false, 0x7FF0000000000000L)]
    [InlineData(false, 0x7FF8000000000011L)]
    [InlineData(true, 0L)]
    [InlineData(true, (long)int.MinValue)]
    [InlineData(true, 0x7F800000L)]
    [InlineData(true, 0x7FC00011L)]
    public void Floating_Negation_Preserves_Zero_Signs_And_Nan_Payloads(bool single, long bits) {
        using var module = NewTarget();
        var method = NewMethod(module, "Negate");
        var type = single ? module.CorLibTypes.Single : module.CorLibTypes.Double;
        method.MethodSig = MethodSig.CreateStatic(type, type);
        method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Neg.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(55)));

        object value = single ? (object)BitConverter.Int32BitsToSingle((int)bits) : BitConverter.Int64BitsToDouble(bits);
        var result = Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!.Invoke(null, [value]);
        if (single)
            Assert.Equal(BitConverter.SingleToInt32Bits(-(float)value), BitConverter.SingleToInt32Bits((float)result!));
        else
            Assert.Equal(BitConverter.DoubleToInt64Bits(-(double)value), BitConverter.DoubleToInt64Bits((double)result!));
    }

    [Fact]
    public void String_Encryption_Widens_Branches_Before_Inserting_Decoders() {
        using var module = NewTarget();
        var method = NewMethod(module, "Read", 1);
        var finish = OpCodes.Ldc_I4_0.ToInstruction();
        var branch = OpCodes.Brfalse_S.ToInstruction(finish);
        method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        method.Body.Instructions.Add(branch);
        for (var index = 0; index < 20; index++) {
            method.Body.Instructions.Add(OpCodes.Ldstr.ToInstruction("literal" + index));
            method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
        }
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        method.Body.Instructions.Add(finish);
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var context = new ObfuscationContext(module, new Random(66));
        var protection = new StringEncryptionProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["handlers"] = 1,
            ["junk-chance"] = 0,
        };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        protection.Initialize(context);
        Assert.Equal(OpCodes.Brfalse, branch.OpCode);
        var loaded = Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!;
        Assert.Equal(0, loaded.Invoke(null, [0]));
        Assert.Equal(1, loaded.Invoke(null, [1]));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(94)]
    public void Typed_Operations_Calls_And_References_Match_Clr(int seed) {
        using var module = NewTarget();
        module.Inject(typeof(VmTypedSamples));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        foreach (var method in marked.Where(m => m.DeclaringType.IsValueType && m.MethodSig.HasThis))
            Assert.True(method.Parameters[0].Type.IsByRef, method.Parameters[0].Type.FullName);
        var context = new ObfuscationContext(module, new Random(seed));
        new VirtualizationProtection().Initialize(context);
        foreach (var method in marked)
            Assert.True(method.Body.Instructions.Any(i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)), method.FullName);
        Assert.DoesNotContain(module.GetAssemblyRefs(), a => a.Name == "SharpHide.Vm");
        Assert.All(module.GetTypes().Where(t => t.Namespace == "SharpHide.Vm"), t => Assert.Contains(t, context.Runtime));
        if (seed == 94) {
            var options = new Dictionary<string, IReadOnlyDictionary<string, double>> {
                ["number-encryption"] = new Dictionary<string, double> { ["handlers"] = 2, ["max-size"] = 2000, ["layers"] = 1 },
                ["string-encryption"] = new Dictionary<string, double> { ["handlers"] = 2, ["junk-chance"] = 0 },
                ["control-flow"] = new Dictionary<string, double> {
                    ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 2,
                    ["opaque-depth"] = 1, ["decrypt-hash-chance"] = 0.2, ["handlers"] = 2,
                },
            };
            typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context, options);
            new NumberEncryptionProtection().Initialize(context);
            new StringEncryptionProtection().Initialize(context);
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
            new ControlFlowProtection().Initialize(context);
        }
        var loaded = Write(module).GetType(typeof(VmTypedSamples).FullName!)!;
        var shared = new object();
        (string Method, object[] Args)[] cases = [
            (nameof(VmTypedSamples.LongArithmetic), [long.MinValue, 65L]),
            (nameof(VmTypedSamples.LongArithmetic), [long.MaxValue, -123456789012345L]),
            (nameof(VmTypedSamples.UnsignedLong), [ulong.MaxValue, 3UL]),
            (nameof(VmTypedSamples.UnsignedLong), [ulong.MaxValue, 0UL]),
            (nameof(VmTypedSamples.CheckedLong), [long.MaxValue, 2L]),
            (nameof(VmTypedSamples.CheckedLong), [100L, -123L]),
            (nameof(VmTypedSamples.SingleArithmetic), [1.25f, 2.75f]),
            (nameof(VmTypedSamples.Native), [(nint)(-123), 40]),
            (nameof(VmTypedSamples.Calls), ["text", -42]),
            (nameof(VmTypedSamples.Calls), [null!, 0]),
            (nameof(VmTypedSamples.Recursive), [8]),
            (nameof(VmTypedSamples.Arrays), [5, 3]),
            (nameof(VmTypedSamples.Arrays), [0, 0]),
            (nameof(VmTypedSamples.Arrays), [-1, 0]),
            (nameof(VmTypedSamples.References), ["reference"]),
            (nameof(VmTypedSamples.References), [null!]),
            (nameof(VmTypedSamples.ReferenceIdentity), [shared, shared]),
            (nameof(VmTypedSamples.ReferenceIdentity), [1, 1]),
            (nameof(VmTypedSamples.ReferenceIdentity), [null!, null!]),
            (nameof(VmTypedSamples.Box), [uint.MaxValue]),
            (nameof(VmTypedSamples.Unbox), [123]),
            (nameof(VmTypedSamples.Unbox), ["wrong"]),
            (nameof(VmTypedSamples.Unbox), [null!]),
            (nameof(VmTypedSamples.TypeTest), ["yes"]),
            (nameof(VmTypedSamples.TypeTest), [123]),
            (nameof(VmTypedSamples.Struct), [123.45m, -500.25m]),
            (nameof(VmTypedSamples.InstanceCalls), [17]),
            (nameof(VmTypedSamples.ArgumentAddress), [17]),
            (nameof(VmTypedSamples.LocalAddress), [17]),
            (nameof(VmTypedSamples.FieldAddress), [17]),
            (nameof(VmTypedSamples.ArrayAddress), [17]),
            (nameof(VmTypedSamples.ValueTypeCall), [-123]),
            (nameof(VmTypedSamples.StructMutation), [17]),
            (nameof(VmTypedSamples.GenericCalls), [17]),
            (nameof(VmTypedSamples.NestedGenericReturn), [17]),
            (nameof(VmTypedSamples.NestedGenericField), [17]),
            (nameof(VmTypedSamples.Nullable), [17]),
            (nameof(VmTypedSamples.Nullable), [null!]),
            (nameof(VmTypedSamples.ExternalAlias), [17]),
            (nameof(VmTypedSamples.ExternalOut), []),
            (nameof(VmTypedSamples.ExternalReference), ["pinned"]),
            (nameof(VmTypedSamples.Delegates), [-17]),
            (nameof(VmTypedSamples.Closure), [17]),
            (nameof(VmTypedSamples.VolatileAccess), [17]),
            (nameof(VmTypedSamples.LinkedCancellationToken), []),
            (nameof(VmTypedSamples.CancellationRegistration), []),
        ];
        foreach (var (method, args) in cases)
            AssertSameResult(typeof(VmTypedSamples).GetMethod(method)!, loaded.GetMethod(method)!, args);
        double[] numbers = [double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.0, 0.0, -1.0, 1.0, double.Epsilon];
        foreach (var a in numbers)
            foreach (var b in numbers)
                foreach (var name in new[] { nameof(VmTypedSamples.DoubleArithmetic), nameof(VmTypedSamples.FloatBranches) })
                    AssertSameResult(typeof(VmTypedSamples).GetMethod(name)!, loaded.GetMethod(name)!, [a, b]);
        Assert.Equal(loaded, loaded.GetMethod(nameof(VmTypedSamples.Token))!.Invoke(null, null));
        foreach (var value in new object[] { 123, "generic", 45.6m })
            foreach (var name in new[] { nameof(VmTypedSamples.Identity), nameof(VmTypedSamples.GenericString) })
                AssertSameResult(typeof(VmTypedSamples).GetMethod(name)!.MakeGenericMethod(value.GetType()),
                    loaded.GetMethod(name)!.MakeGenericMethod(value.GetType()), [value]);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(22)]
    public void Exception_Regions_Preserve_Filter_Order_And_Unwinding(int seed) {
        using var module = NewTarget();
        module.Inject(typeof(VmExceptionSamples));
        var context = new ObfuscationContext(module, new Random(seed));
        new VirtualizationProtection().Initialize(context);
        var definition = module.GetTypes().Single(t => t.FullName == typeof(VmExceptionSamples).FullName);
        foreach (var method in definition.Methods.Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")))
            Assert.True(method.Body.Instructions.Any(i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)), method.FullName);
        if (seed == 22) {
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
        }
        var loaded = Write(module).GetType(typeof(VmExceptionSamples).FullName!)!;
        (string Name, object[] Args)[] cases = [
            (nameof(VmExceptionSamples.Nested), [0]),
            (nameof(VmExceptionSamples.Nested), [5]),
            (nameof(VmExceptionSamples.Caller), []),
            (nameof(VmExceptionSamples.NativeCaller), []),
            (nameof(VmExceptionSamples.FilterThrows), []),
            (nameof(VmExceptionSamples.FinallyThrows), []),
            (nameof(VmExceptionSamples.CatchInsideFinally), []),
            (nameof(VmExceptionSamples.Rethrow), [new InvalidOperationException("identity")]),
        ];
        foreach (var (name, args) in cases) {
            AssertSameResult(typeof(VmExceptionSamples).GetMethod(name)!, loaded.GetMethod(name)!, args);
            Assert.Equal(VmExceptionSamples.Trace, loaded.GetField(nameof(VmExceptionSamples.Trace))!.GetValue(null));
        }
    }

    [Fact]
    public void Fault_Runs_Only_On_Exceptional_Exit() {
        using var module = NewTarget();
        var method = NewMethod(module, "Fault", 1);
        var field = new FieldDefUser("Trace", new FieldSig(module.CorLibTypes.Int32),
            dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
        method.DeclaringType.Fields.Add(field);
        var result = new Local(module.CorLibTypes.Int32);
        method.Body.Variables.Add(result);
        var start = OpCodes.Ldc_I4.ToInstruction(100);
        var fault = OpCodes.Ldc_I4.ToInstruction(123);
        var end = OpCodes.Ldloc.ToInstruction(result);
        foreach (var instruction in new[] { start, OpCodes.Ldarg_0.ToInstruction(), OpCodes.Div.ToInstruction(),
                     OpCodes.Stloc.ToInstruction(result), OpCodes.Leave.ToInstruction(end), fault,
                     OpCodes.Stsfld.ToInstruction(field), OpCodes.Endfinally.ToInstruction(), end, OpCodes.Ret.ToInstruction() })
            method.Body.Instructions.Add(instruction);
        method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Fault) {
            TryStart = start, TryEnd = fault, HandlerStart = fault, HandlerEnd = end,
        });
        var before = Write(module).GetType("Tests.Program")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(321)));
        Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        var after = Write(module).GetType("Tests.Program")!;
        foreach (var value in new[] { 5, 0 }) {
            before.GetField("Trace")!.SetValue(null, 0);
            after.GetField("Trace")!.SetValue(null, 0);
            AssertSameResult(before.GetMethod("Fault")!, after.GetMethod("Fault")!, [value]);
            Assert.Equal(value == 0 ? 123 : 0, after.GetField("Trace")!.GetValue(null));
        }
    }

    [Fact]
    public void Readonly_Address_Preserves_Array_Covariance() {
        using var module = NewTarget();
        foreach (var readOnly in new[] { false, true }) {
            var method = NewMethod(module, readOnly ? "Readonly" : "Writable", 2);
            method.MethodSig.Params[0] = new SZArraySig(module.CorLibTypes.Object);
            method.MethodSig.RetType = module.CorLibTypes.Object;
            method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            if (readOnly)
                method.Body.Instructions.Add(OpCodes.Readonly.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Ldelema.ToInstruction(module.CorLibTypes.Object.TypeDefOrRef));
            method.Body.Instructions.Add(OpCodes.Ldind_Ref.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        }
        var before = Write(module).GetType("Tests.Program")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(811)));
        foreach (var method in module.GetTypes().Single(t => t.FullName == "Tests.Program").Methods.Where(method => method.IsPublic))
            Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        var after = Write(module).GetType("Tests.Program")!;
        foreach (var name in new[] { "Readonly", "Writable" })
            foreach (var args in new object[][] { [new[] { "value" }, 0], [new[] { "value" }, -1], [new object[] { 123 }, 0], [null!, 0] })
                AssertSameResult(before.GetMethod(name)!, after.GetMethod(name)!, args);
    }

    [Fact]
    public void Unaligned_Volatile_Indirect_Access_Matches_Clr() {
        using var module = NewTarget();
        var read = NewMethod(module, "Read", 1);
        var write = NewMethod(module, "Write", 2);
        foreach (var method in new[] { read, write }) {
            method.MethodSig.Params[0] = new ByRefSig(module.CorLibTypes.Byte);
            method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            if (method == write)
                method.Body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Volatile.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Unaligned.ToInstruction((byte)1));
            method.Body.Instructions.Add((method == read ? OpCodes.Ldind_I4 : OpCodes.Stind_I4).ToInstruction());
            if (method == write)
                method.Body.Instructions.Add(OpCodes.Ldarg_1.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        }
        var before = Write(module).GetType("Tests.Program")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(91)));
        Assert.All(new[] { read, write }, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        var after = Write(module).GetType("Tests.Program")!;
        var outcomes = new List<(int Read, int Write, byte[] Bytes)>();
        foreach (var type in new[] { before, after }) {
            var methods = new List<Delegate>();
            foreach (var name in new[] { "Read", "Write" }) {
                var parameters = name == "Read" ? new[] { typeof(byte[]) } : new[] { typeof(byte[]), typeof(int) };
                var thunk = new Emit.DynamicMethod("", typeof(int), parameters);
                var il = thunk.GetILGenerator();
                il.Emit(Emit.OpCodes.Ldarg_0);
                il.Emit(Emit.OpCodes.Ldc_I4_1);
                il.Emit(Emit.OpCodes.Ldelema, typeof(byte));
                if (name == "Write")
                    il.Emit(Emit.OpCodes.Ldarg_1);
                il.Emit(Emit.OpCodes.Call, type.GetMethod(name)!);
                il.Emit(Emit.OpCodes.Ret);
                methods.Add(thunk.CreateDelegate(name == "Read" ? typeof(Func<byte[], int>) : typeof(Func<byte[], int, int>)));
            }
            byte[] bytes = [0, 0x78, 0x56, 0x34, 0x12, 0];
            var result = ((Func<byte[], int>)methods[0])(bytes);
            var written = ((Func<byte[], int, int>)methods[1])(bytes, -123456);
            outcomes.Add((result, written, bytes));
        }
        Assert.Equal(outcomes[0].Read, outcomes[1].Read);
        Assert.Equal(outcomes[0].Write, outcomes[1].Write);
        Assert.Equal(outcomes[0].Bytes, outcomes[1].Bytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Indirect_Calls_Preserve_Managed_And_Unmanaged_Signatures(bool rename) {
        using var module = NewTarget();
        var targets = module.Inject(typeof(VmIndirectSamples));
        var dispatch = module.Inject(typeof(VmDispatchDerived));
        var methods = new List<MethodDef>();
        foreach (var name in new[] { "Managed", "Native", "ModifiedNative" }) {
            var native = name != "Managed";
            var method = NewMethod(module, name, 2);
            var target = targets.ResolveMethod(typeof(VmIndirectSamples).GetMethod(native ? nameof(VmIndirectSamples.NativeAdd) : nameof(VmIndirectSamples.Add))!)!;
            var signature = MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32, module.CorLibTypes.Int32);
            if (native)
                signature.CallingConvention = CallingConvention.C;
            if (name == "ModifiedNative") {
                signature.CallingConvention = CallingConvention.Unmanaged;
                signature.RetType = new CModOptSig(module.Import(typeof(System.Runtime.CompilerServices.CallConvCdecl)), signature.RetType);
            }
            foreach (var instruction in new[] { OpCodes.Ldarg_0.ToInstruction(), OpCodes.Ldarg_1.ToInstruction(),
                         OpCodes.Ldftn.ToInstruction(target), OpCodes.Calli.ToInstruction(signature), OpCodes.Ret.ToInstruction() })
                method.Body.Instructions.Add(instruction);
            methods.Add(method);
        }
        var reference = NewMethod(module, "Reference", 1);
        reference.Body.Instructions.Add(OpCodes.Ldarga.ToInstruction(reference.Parameters[0]));
        reference.Body.Instructions.Add(OpCodes.Ldftn.ToInstruction(targets.ResolveMethod(typeof(VmIndirectSamples).GetMethod(nameof(VmIndirectSamples.Increment))!)!));
        reference.Body.Instructions.Add(OpCodes.Calli.ToInstruction(MethodSig.CreateStatic(module.CorLibTypes.Int32, new ByRefSig(module.CorLibTypes.Int32))));
        reference.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        methods.Add(reference);
        var instance = NewMethod(module, "Instance", 1);
        instance.Body.Instructions.Add(OpCodes.Newobj.ToInstruction(dispatch.ResolveMethod(typeof(VmDispatchDerived).GetConstructor(Type.EmptyTypes)!)!));
        instance.Body.Instructions.Add(OpCodes.Dup.ToInstruction());
        instance.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        instance.Body.Instructions.Add(OpCodes.Stfld.ToInstruction(dispatch.ResolveField(typeof(VmDispatchBase).GetField(nameof(VmDispatchBase.Value))!)!));
        instance.Body.Instructions.Add(OpCodes.Dup.ToInstruction());
        instance.Body.Instructions.Add(OpCodes.Ldvirtftn.ToInstruction(dispatch.ResolveMethod(typeof(VmDispatchBase).GetMethod(nameof(VmDispatchBase.Read))!)!));
        instance.Body.Instructions.Add(OpCodes.Calli.ToInstruction(MethodSig.CreateInstance(module.CorLibTypes.Int32)));
        instance.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        methods.Add(instance);
        var before = Write(module).GetType("Tests.Program")!;
        var context = new ObfuscationContext(module, new Random(772));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        new VirtualizationProtection().Initialize(context);
        Assert.All(marked, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename) {
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
        }
        var assembly = Write(module);
        var after = assembly.GetType("Tests.Program")!;
        var indirect = assembly.GetType(typeof(VmIndirectSamples).FullName!)!;
        AssertSameResult(typeof(VmIndirectSamples).GetMethod(nameof(VmIndirectSamples.NativeIdentity))!,
            indirect.GetMethod(nameof(VmIndirectSamples.NativeIdentity))!, []);
        foreach (var name in new[] { nameof(VmIndirectSamples.ManagedPointer), nameof(VmIndirectSamples.UnmanagedPointer), nameof(VmIndirectSamples.PointerValues),
                     nameof(VmIndirectSamples.FixedStorage), nameof(VmIndirectSamples.FixedArray), nameof(VmIndirectSamples.StackMemory) })
            foreach (var value in new[] { -123, 0, 37 })
                AssertSameResult(typeof(VmIndirectSamples).GetMethod(name)!, indirect.GetMethod(name)!, [value]);
        foreach (var method in methods)
            foreach (var value in new[] { int.MinValue, -17, 0, 35, int.MaxValue })
                AssertSameResult(before.GetMethod(method.Name)!, after.GetMethod(method.Name)!,
                    method.MethodSig.Params.Count == 2 ? [value, 999] : [value]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Block_Memory_Operations_Preserve_Bytes_And_Bounds(bool prefixed) {
        using var module = NewTarget();
        var method = NewMethod(module, "Copy", 0);
        var bytes = new SZArraySig(module.CorLibTypes.Byte);
        method.MethodSig = MethodSig.CreateStatic(module.CorLibTypes.Int32, bytes, bytes, module.CorLibTypes.Int32);
        method.Parameters.UpdateParameterTypes();
        foreach (var copy in new[] { false, true }) {
            var il = method.Body.Instructions;
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add(OpCodes.Ldc_I4_1.ToInstruction());
            il.Add(OpCodes.Ldelema.ToInstruction(module.CorLibTypes.Byte.TypeDefOrRef));
            if (copy) {
                il.Add(OpCodes.Ldarg_1.ToInstruction());
                il.Add(OpCodes.Ldc_I4_1.ToInstruction());
                il.Add(OpCodes.Ldelema.ToInstruction(module.CorLibTypes.Byte.TypeDefOrRef));
                il.Add(OpCodes.Ldarg_2.ToInstruction());
            }
            else {
                il.Add(OpCodes.Ldc_I4.ToInstruction(0x1A5));
                il.Add(OpCodes.Ldc_I4.ToInstruction(14));
            }
            if (prefixed) {
                il.Add(OpCodes.Unaligned.ToInstruction((byte)1));
                il.Add(OpCodes.Volatile.ToInstruction());
            }
            il.Add((copy ? OpCodes.Cpblk : OpCodes.Initblk).ToInstruction());
        }
        method.Body.Instructions.Add(OpCodes.Ldarg_2.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var before = Write(module).GetType("Tests.Program")!.GetMethod("Copy")!;
        var context = new ObfuscationContext(module, new Random(826));
        new VirtualizationProtection().Initialize(context);
        Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        new RenameInjectedProtection().Initialize(context);
        var after = Write(module).GetType("Tests.Program")!.GetMethod("Copy")!;
        foreach (var count in new[] { 0, 1, 3, 7 }) {
            var source = Enumerable.Range(0, 16).Select(i => (byte)(i * 13)).ToArray();
            var expected = new byte[16];
            var actual = new byte[16];
            Assert.Equal(before.Invoke(null, [expected, source, count]), after.Invoke(null, [actual, source, count]));
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructors_Preserve_Initialization_Order_Readonly_Fields_And_Exceptions(bool rename) {
        using var module = NewTarget();
        module.Inject(typeof(VmConstructorSamples));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        Assert.Contains(marked, m => m.IsInstanceConstructor && m.DeclaringType.IsValueType);
        var context = new ObfuscationContext(module, new Random(192));
        new VirtualizationProtection().Initialize(context);
        Assert.All(marked, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename) {
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
        }
        var actual = Write(module).GetType(typeof(VmConstructorSamples).FullName!)!.GetMethod(nameof(VmConstructorSamples.Run))!;
        foreach (var value in new[] { int.MinValue, -2, 0, 5, int.MaxValue })
            AssertSameResult(typeof(VmConstructorSamples).GetMethod(nameof(VmConstructorSamples.Run))!, actual, [value]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reference_Returns_Preserve_Aliases_Across_Compacting_Gc(bool rename) {
        using var module = NewTarget();
        module.Inject(typeof(VmReferenceReturnSamples));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        var context = new ObfuscationContext(module, new Random(818));
        new VirtualizationProtection().Initialize(context);
        Assert.All(marked, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename) {
            var options = new Dictionary<string, IReadOnlyDictionary<string, double>> {
                ["number-encryption"] = new Dictionary<string, double> { ["handlers"] = 2, ["max-size"] = 2000, ["layers"] = 1 },
                ["string-encryption"] = new Dictionary<string, double> { ["handlers"] = 2, ["junk-chance"] = 0 },
                ["control-flow"] = new Dictionary<string, double> {
                    ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 2,
                    ["opaque-depth"] = 1, ["decrypt-hash-chance"] = 0.2, ["handlers"] = 2,
                },
            };
            typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context, options);
            new NumberEncryptionProtection().Initialize(context);
            new StringEncryptionProtection().Initialize(context);
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
            new ControlFlowProtection().Initialize(context);
        }
        var actual = Write(module).GetType(typeof(VmReferenceReturnSamples).FullName!)!.GetMethod(nameof(VmReferenceReturnSamples.Run))!;
        foreach (var value in new[] { -17, 0, 13 })
            AssertSameResult(typeof(VmReferenceReturnSamples).GetMethod(nameof(VmReferenceReturnSamples.Run))!, actual, [value]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reference_Calls_Preserve_Frame_Lifetimes_Pinning_And_Regions(bool rename) {
        using var module = NewTarget();
        module.Inject(typeof(VmReferenceCallSamples));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        var context = new ObfuscationContext(module, new Random(928));
        new VirtualizationProtection().Initialize(context);
        Assert.All(marked, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename) {
            new UserRenameProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
        }
        var loaded = Write(module).GetType(typeof(VmReferenceCallSamples).FullName!)!;
        foreach (var method in marked)
            foreach (var value in new[] { 0, 3, 40, 130 })
                AssertSameResult(typeof(VmReferenceCallSamples).GetMethod(method.Name)!, loaded.GetMethod(method.Name)!, [value]);
    }

    private static void AssertSameResult(MethodInfo original, MethodInfo actual, object[] args) {
        object? expected = null;
        var expectedError = Record.Exception(() => expected = original.Invoke(null, args));
        object? result = null;
        var actualError = Record.Exception(() => result = actual.Invoke(null, args));
        var expectedType = (expectedError as TargetInvocationException)?.InnerException?.GetType() ?? expectedError?.GetType();
        var actualType = (actualError as TargetInvocationException)?.InnerException?.GetType() ?? actualError?.GetType();
        Assert.True(expectedType == actualType,
            $"{original.Name}({string.Join(", ", args)}): expected {expectedType}, got {actualType}\n{actualError}");
        if (expectedError is null)
            Assert.Equal(expected, result);
    }

    [Fact]
    public void Field_Metadata_Tokens_Use_Field_Bridges() {
        using var module = NewTarget();
        var method = NewMethod(module, "FieldToken");
        var field = new FieldDefUser("Value", new FieldSig(module.CorLibTypes.Int32),
            dnlib.DotNet.FieldAttributes.Public | dnlib.DotNet.FieldAttributes.Static);
        method.DeclaringType.Fields.Add(field);
        method.Body.Instructions.Add(OpCodes.Ldtoken.ToInstruction(field));
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(typeof(FieldInfo).GetMethod(
            nameof(FieldInfo.GetFieldFromHandle), [typeof(RuntimeFieldHandle)])!)));
        method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(1203)));

        Assert.Contains(method.Body.Instructions, instruction => instruction.Operand is IMethod target &&
            target.Name == nameof(VmRunner.CreateFrame));
        Assert.Equal(1, Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!.Invoke(null, null));
    }

    private static MethodDef NewMethod(ModuleDef module, string name, int arguments = 0) {
        var type = module.Types.FirstOrDefault(t => t.Name == "Program");
        if (type is null) {
            type = new TypeDefUser("Tests", "Program", module.CorLibTypes.Object.TypeDefOrRef) {
                Attributes = dnlib.DotNet.TypeAttributes.Public,
            };
            module.Types.Add(type);
        }

        var method = new MethodDefUser(name,
            MethodSig.CreateStatic(module.CorLibTypes.Int32, Enumerable.Repeat<TypeSig>(module.CorLibTypes.Int32, arguments).ToArray()),
            dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) {
            Body = new CilBody { InitLocals = true },
        };
        type.Methods.Add(method);
        method.CustomAttributes.Add(new CustomAttribute((ICustomAttributeType)new Importer(module)
            .Import(typeof(VirtualizeAttribute).GetConstructor(Type.EmptyTypes)!)));
        return method;
    }

    [Fact]
    public void Frames_And_Indexes_Exceed_Former_Fixed_Limits() {
        using var module = NewTarget();
        var method = NewMethod(module, "LargeFrame", 300);
        for (var i = 0; i < 300; i++)
            method.Body.Variables.Add(new Local(module.CorLibTypes.Int32));

        var il = method.Body.Instructions;
        il.Add(OpCodes.Ldarg.ToInstruction(method.Parameters[299]));
        il.Add(OpCodes.Stloc.ToInstruction(method.Body.Variables[299]));
        for (var i = 0; i < 300; i++)
            il.Add(OpCodes.Ldc_I4.ToInstruction(i));
        for (var i = 0; i < 299; i++)
            il.Add(OpCodes.Add.ToInstruction());
        il.Add(OpCodes.Ldloc.ToInstruction(method.Body.Variables[299]));
        il.Add(OpCodes.Add.ToInstruction());
        il.Add(OpCodes.Ret.ToInstruction());

        var args = Enumerable.Repeat<object>(0, 300).ToArray();
        args[299] = 1000;
        var before = Write(module).GetType("Tests.Program")!.GetMethod("LargeFrame")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(3)));
        Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        var after = Write(module).GetType("Tests.Program")!.GetMethod("LargeFrame")!;
        AssertSameResult(before, after, args);
        Assert.Equal(45850, after.Invoke(null, args));
    }

    [Fact]
    public void Oversized_Native_Frames_Keep_Their_Original_Body() {
        using var module = NewTarget();
        var method = NewMethod(module, "LargeReferenceFrame");
        for (var i = 0; i < VmOpcodes.MaxFrameSize / 2; i++)
            method.Body.Variables.Add(new Local(module.CorLibTypes.Int32));
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(module.Import(typeof(VmFrameTests).GetMethod(nameof(VmFrameTests.Null))!)));
        method.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var body = method.Body;
        var count = module.GetTypes().Count();
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(9)));
        Assert.Same(body, method.Body);
        Assert.Equal(count, module.GetTypes().Count());
        Assert.Equal(1, Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!.Invoke(null, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Invalid_Stack_Depths_Are_Rejected_During_Compilation(int scenario) {
        using var module = NewTarget();
        var method = NewMethod(module, "InvalidStack");
        var body = method.Body;
        var finish = OpCodes.Ret.ToInstruction();
        if (scenario == 0)
            body.Instructions.Add(OpCodes.Pop.ToInstruction());
        body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        if (scenario == 2) {
            body.Instructions.Add(OpCodes.Dup.ToInstruction());
            body.Instructions.Add(OpCodes.Brtrue.ToInstruction(finish));
        }
        if (scenario != 0)
            body.Instructions.Add(OpCodes.Ldc_I4_2.ToInstruction());
        body.Instructions.Add(finish);

        var context = new ObfuscationContext(module, new Random(73));
        Assert.Equal(0, VirtualizationProtection.Virtualize(context, [method]));
        Assert.Same(body, method.Body);
        Assert.Empty(context.VirtualMachineTypes);
    }

    [Fact]
    public void Invalid_Operation_Prefixes_Are_Rejected_During_Compilation() {
        using var module = NewTarget();
        var method = NewMethod(module, "InvalidPrefix");
        method.Body.Instructions.Add(OpCodes.Ldc_I4_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Conv_I.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Unaligned.ToInstruction((byte)3));
        method.Body.Instructions.Add(OpCodes.Ldind_I4.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var body = method.Body;
        var instructions = body.Instructions.ToArray();

        var context = new ObfuscationContext(module, new Random(74));
        Assert.Equal(0, VirtualizationProtection.Virtualize(context, [method]));
        Assert.Same(body, method.Body);
        Assert.Equal(instructions, method.Body.Instructions);
        Assert.Empty(context.VirtualMachineTypes);
    }

    [Fact]
    public void Invalid_Arglist_Keeps_Its_Body_And_Does_Not_Inject_Runtime() {
        using var module = NewTarget();
        var invalid = NewMethod(module, "InvalidArglist");
        invalid.Body.Instructions.Add(OpCodes.Arglist.ToInstruction());
        invalid.Body.Instructions.Add(OpCodes.Pop.ToInstruction());
        invalid.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        invalid.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var body = invalid.Body;
        var instructions = invalid.Body.Instructions.ToArray();
        var count = module.GetTypes().Count();
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(9)));
        Assert.Equal(count, module.GetTypes().Count());
        Assert.Same(body, invalid.Body);
        Assert.Equal(instructions, invalid.Body.Instructions);
    }

    [Fact]
    public void Varargs_And_Arglist_Use_Captured_Runtime_Argument_Handle() {
        using var module = NewTarget();
        module.Inject(typeof(VmVarArgSamples));
        var definition = module.GetTypes().Single(type => type.FullName == typeof(VmVarArgSamples).FullName);
        var sum = definition.Methods.Single(method => method.Name == "Sum");
        Assert.All(sum.Body.Variables.Where(local => local.Type.FullName == typeof(ArgIterator).FullName),
            local => Assert.True(VmTypeLayout.RequiresNative(sum, local.Type), local.Type.AssemblyQualifiedName));
        var context = new ObfuscationContext(module, new Random(96));
        Assert.Equal(1, VirtualizationProtection.Virtualize(context, [sum]));
        Assert.Contains(sum.Body.Instructions,
            instruction => instruction.Operand is IMethod target && target.Name == nameof(VmRunner.CreateFrame));
        var loaded = Write(module).GetType(typeof(VmVarArgSamples).FullName!)!;
        int Invoke(Type type) {
            var thunk = new Emit.DynamicMethod("", typeof(int), Type.EmptyTypes);
            var il = thunk.GetILGenerator();
            foreach (var value in new[] { 3, 5, 7, 11 })
                il.Emit(Emit.OpCodes.Ldc_I4, value);
            il.EmitCall(Emit.OpCodes.Call, type.GetMethod(nameof(VmVarArgSamples.Sum))!,
                [typeof(int), typeof(int), typeof(int)]);
            il.Emit(Emit.OpCodes.Ret);
            return ((Func<int>)thunk.CreateDelegate(typeof(Func<int>)))();
        }
        Assert.Equal(Invoke(typeof(VmVarArgSamples)), Invoke(loaded));
    }

    [Fact]
    public void Varargs_Call_Sites_Preserve_Optional_Arguments() {
        using var module = NewTarget();
        var type = new TypeDefUser("Tests", "VarArgCall", module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = dnlib.DotNet.TypeAttributes.Public,
        };
        module.Types.Add(type);
        var targetSignature = MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32);
        targetSignature.CallingConvention = dnlib.DotNet.CallingConvention.VarArg;
        var target = new MethodDefUser("Target", targetSignature,
            dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
        target.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        target.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(target);

        var callSignature = targetSignature.Clone();
        callSignature.ParamsAfterSentinel = [module.CorLibTypes.Int32];
        var callTarget = new MemberRefUser(module, target.Name, callSignature, type);
        var caller = new MethodDefUser("Call", MethodSig.CreateStatic(module.CorLibTypes.Int32),
            dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
        caller.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(17));
        caller.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(23));
        caller.Body.Instructions.Add(OpCodes.Call.ToInstruction(callTarget));
        caller.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(caller);

        var context = new ObfuscationContext(module, new Random(97));
        Assert.Equal(1, VirtualizationProtection.Virtualize(context, [caller]));
        var loaded = Write(module).GetType(type.FullName)!;
        Assert.Equal(17, loaded.GetMethod(caller.Name)!.Invoke(null, null));
    }

    [Fact]
    public void Tail_Calls_And_Jumps_Lower_To_Static_Call_Bridges() {
        using var module = NewTarget();
        var target = NewMethod(module, "Target", 1);
        target.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        target.Body.Instructions.Add(OpCodes.Ldc_I4_2.ToInstruction());
        target.Body.Instructions.Add(OpCodes.Add.ToInstruction());
        target.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        var tail = NewMethod(module, "Tail", 1);
        tail.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        tail.Body.Instructions.Add(OpCodes.Tailcall.ToInstruction());
        tail.Body.Instructions.Add(OpCodes.Call.ToInstruction(target));
        tail.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        var jump = NewMethod(module, "Jump", 1);
        jump.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(29));
        jump.Body.Instructions.Add(OpCodes.Starg.ToInstruction(jump.Parameters[0]));
        jump.Body.Instructions.Add(OpCodes.Jmp.ToInstruction(target));

        var context = new ObfuscationContext(module, new Random(98));
        Assert.Equal(2, VirtualizationProtection.Virtualize(context, [tail, jump]));
        var loaded = Write(module).GetType("Tests.Program")!;
        Assert.Equal(19, loaded.GetMethod(tail.Name)!.Invoke(null, [17]));
        Assert.Equal(31, loaded.GetMethod(jump.Name)!.Invoke(null, [0]));
    }

    [Fact]
    public void VmBegin_And_VmEnd_Virtualize_Only_Selected_Region() {
        using var module = NewTarget();
        var method = NewMethod(module, "Selective", 1);
        var local = new Local(module.CorLibTypes.Int32);
        method.Body.Variables.Add(local);
        MethodDef Marker(string name) {
            var marker = new MethodDefUser(name, MethodSig.CreateStatic(module.CorLibTypes.Void),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
            marker.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            method.DeclaringType.Methods.Add(marker);
            return marker;
        }
        var begin = Marker("VMBegin");
        var end = Marker("VMEnd");
        method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Stloc_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(begin));
        method.Body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ldc_I4_3.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Mul.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Stloc_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(end));
        method.Body.Instructions.Add(OpCodes.Ldloc_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ldc_I4_2.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Add.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(99)));
        Assert.DoesNotContain(method.Body.Instructions, instruction => instruction.Operand is IMethod target &&
            target.Name.String is "VMBegin" or "VMEnd");
        Assert.Contains(method.Body.Instructions, instruction => instruction.OpCode == OpCodes.Add);
        Assert.DoesNotContain(method.Body.Instructions, instruction => instruction.OpCode == OpCodes.Mul);
        var helper = method.Body.Instructions.Select(instruction => instruction.Operand as IMethod)
            .Where(target => target?.Name.String.StartsWith("Selective$vmregion") == true)
            .Select(target => target!.ResolveMethodDef()).Single();
        Assert.Contains(helper.Body.Instructions,
            instruction => instruction.Operand is IMethod target && target.Name == nameof(VmRunner.CreateFrame));
        var loaded = Write(module).GetType("Tests.Program")!;
        foreach (var value in new[] { -4, 0, 7 })
            Assert.Equal(value * 3 + 2, loaded.GetMethod(method.Name)!.Invoke(null, [value]));
    }

    [Fact]
    public void Selective_Region_Strict_Mode_Rejects_Crossing_Branch() {
        using var module = NewTarget();
        var method = NewMethod(module, "StrictSelective", 1);
        MethodDef Marker(string name) {
            var marker = new MethodDefUser(name, MethodSig.CreateStatic(module.CorLibTypes.Void),
                dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
            marker.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            method.DeclaringType.Methods.Add(marker);
            return marker;
        }
        var begin = Marker("VMBegin");
        var end = Marker("VMEnd");
        var outside = OpCodes.Ldc_I4_0.ToInstruction();
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(begin));
        method.Body.Instructions.Add(OpCodes.Brtrue.ToInstruction(outside));
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Call.ToInstruction(end));
        method.Body.Instructions.Add(outside);
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["regions-strict"] = 1 };
        var context = new ObfuscationContext(module, new Random(101));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
        Assert.Throws<InvalidOperationException>(() => protection.Initialize(context));
        Assert.Contains(method.Body.Instructions, instruction => instruction.Operand is IMethod target && target.Name == "VMBegin");
    }

    [Fact]
    public void Managed_Reference_To_TypedReference_Return_Preserves_Alias() {
        using var module = NewTarget();
        var type = new TypeDefUser("Tests", "TypedReferenceReturn", module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = dnlib.DotNet.TypeAttributes.Public,
        };
        module.Types.Add(type);
        var referenceType = new ByRefSig(module.CorLibTypes.TypedReference);
        var identity = new MethodDefUser("Identity", MethodSig.CreateStatic(referenceType, referenceType),
            dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
        identity.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        identity.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(identity);

        var handleType = new ValueTypeSig(module.Import(typeof(RuntimeTypeHandle)));
        var caller = new MethodDefUser("ReadType", MethodSig.CreateStatic(handleType, module.CorLibTypes.Int32),
            dnlib.DotNet.MethodImplAttributes.IL | dnlib.DotNet.MethodImplAttributes.Managed,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) {
            Body = new CilBody { InitLocals = true },
        };
        var typedReference = new Local(module.CorLibTypes.TypedReference);
        caller.Body.Variables.Add(typedReference);
        caller.Body.Instructions.Add(OpCodes.Ldarga.ToInstruction(caller.Parameters[0]));
        caller.Body.Instructions.Add(OpCodes.Mkrefany.ToInstruction(module.CorLibTypes.Int32.TypeDefOrRef));
        caller.Body.Instructions.Add(OpCodes.Stloc.ToInstruction(typedReference));
        caller.Body.Instructions.Add(OpCodes.Ldloca.ToInstruction(typedReference));
        caller.Body.Instructions.Add(OpCodes.Call.ToInstruction(identity));
        caller.Body.Instructions.Add(OpCodes.Ldobj.ToInstruction(module.Import(typeof(TypedReference))));
        caller.Body.Instructions.Add(OpCodes.Refanytype.ToInstruction());
        caller.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(caller);

        var context = new ObfuscationContext(module, new Random(95));
        Assert.Equal(1, VirtualizationProtection.Virtualize(context, [identity]));
        var loaded = Write(module).GetType(type.FullName)!;
        Assert.Equal(typeof(int).TypeHandle, loaded.GetMethod(caller.Name)!.Invoke(null, [17]));
    }

    [Fact]
    public void Native_Type_Banks_Respect_Clr_Local_Limits() {
        using var module = NewTarget();
        var method = NewMethod(module, "LargeNativeFrame");
        for (var i = 0; i < 17000; i++)
            method.Body.Variables.Add(new Local(module.CorLibTypes.Int32));
        method.Body.Variables.Add(new Local(module.Import(typeof(Span<int>)).ToTypeSig()));
        method.Body.Variables.Add(new Local(module.Import(typeof(Span<string>)).ToTypeSig()));
        method.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var body = method.Body;
        var count = module.GetTypes().Count();
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(193)));
        Assert.Same(body, method.Body);
        Assert.Equal(count, module.GetTypes().Count());
        Assert.Equal(1, Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!.Invoke(null, null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_Locals_Preserve_Ref_Struct_And_Typed_Reference_Copies(bool rename) {
        using var module = NewTarget();
        module.Inject(typeof(VmNativeSamples));
        var marked = module.GetTypes().SelectMany(t => t.Methods)
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "VirtualizeAttribute")).ToArray();
        var context = new ObfuscationContext(module, new Random(392));
        new VirtualizationProtection().Initialize(context);
        Assert.All(marked, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename)
            new RenameInjectedProtection().Initialize(context);
        var loaded = Write(module).GetType(typeof(VmNativeSamples).FullName!)!;
        foreach (var method in marked.Where(m => m.IsPublic))
            foreach (var value in new[] { "alive", "", "héllo🦊" })
                AssertSameResult(typeof(VmNativeSamples).GetMethod(method.Name)!, loaded.GetMethod(method.Name)!, [value]);
    }

    [Fact]
    public void Allows_Ref_Struct_Generic_Parameters_Use_Native_Cells() {
        using var module = NewTarget();
        module.Inject(typeof(VmAllowRefStructSamples));
        var definition = module.GetTypes().Single(type => type.FullName == typeof(VmAllowRefStructSamples).FullName);
        var identity = definition.Methods.Single(method => method.Name == "Identity");
        var duplicate = new Local(module.Import(typeof(Span<int>)).ToTypeSig());
        identity.Body.Variables.Add(duplicate);
        identity.Body.Instructions.Insert(identity.Body.Instructions.Count - 1, OpCodes.Ldloc.ToInstruction(duplicate));
        identity.Body.Instructions.Insert(identity.Body.Instructions.Count - 1, OpCodes.Pop.ToInstruction());
        var context = new ObfuscationContext(module, new Random(394));
        Assert.Equal(1, VirtualizationProtection.Virtualize(context, [identity]));
        Assert.Contains(identity.Body.Instructions,
            instruction => instruction.Operand is IMethod target && target.Name == nameof(VmRunner.CreateFrame));
        var loaded = Write(module).GetType(typeof(VmAllowRefStructSamples).FullName!)!;
        Assert.Equal(VmAllowRefStructSamples.Run(), loaded.GetMethod(nameof(VmAllowRefStructSamples.Run))!.Invoke(null, null));
    }

    [Fact]
    public void Same_Seed_Produces_Same_Program_And_Different_Seeds_Vary() {
        string Program(int seed, string? key = null) {
            using var module = NewTarget();
            var method = NewMethod(module, "Constant");
            method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(42));
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            var context = new ObfuscationContext(module, new Random(seed));
            context.Settings.VirtualizationKey = key;
            var protection = new VirtualizationProtection();
            var options = new Dictionary<string, double>(protection.DefaultOptions) {
                ["bytecode-rounds"] = 32,
                ["payload-auth"] = 1,
            };
            typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
                new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });
            protection.Initialize(context);
            return (string)method.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldstr).Operand;
        }

        Assert.Equal(Program(1), Program(1));
        Assert.NotEqual(Program(1), Program(2));
        Assert.Equal(Program(1, "key-a"), Program(1, "key-a"));
        Assert.NotEqual(Program(1, "key-a"), Program(1, "key-b"));
    }

    [Fact]
    public void Bytecode_Encryption_Is_Bounded_And_Executed_By_Injected_Runtime() {
        using var module = NewTarget();
        var method = NewMethod(module, "Encrypted");
        method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(42));
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var protection = new VirtualizationProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) {
            ["bytecode-rounds"] = 32,
        };
        var context = new ObfuscationContext(module, new Random(94));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        protection.Initialize(context);

        var program = Convert.FromBase64String((string)method.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldstr).Operand);
        Assert.NotEqual(VmOpcodes.Magic, BitConverter.ToInt32(VmRunner.Scramble(program), 256));
        Assert.Equal(42, Write(module).GetType("Tests.Program")!.GetMethod(method.Name)!.Invoke(null, null));
    }

    [Fact]
    public void Unknown_Clr_Opcodes_Do_Not_Abort_Other_Methods() {
        using var module = NewTarget();
        var unsupported = NewMethod(module, "Unsupported");
        unsupported.Body.Instructions.Add(new Instruction(OpCodes.No, (byte)1));
        unsupported.Body.Instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
        unsupported.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var body = unsupported.Body;
        var instructions = body.Instructions.ToArray();
        var supported = NewMethod(module, "Supported");
        supported.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(42));
        supported.Body.Instructions.Add(OpCodes.Ret.ToInstruction());

        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(19)));

        Assert.Same(body, unsupported.Body);
        Assert.Equal(instructions, unsupported.Body.Instructions);
        Assert.Contains(supported.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        Assert.Equal(42, Write(module).GetType("Tests.Program")!.GetMethod(supported.Name)!.Invoke(null, null));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Typed_References_Preserve_Copies_Types_And_Aliases_Across_Gc(bool rename) {
        using var module = NewTarget();
        var methods = new List<MethodDef>();
        foreach (var name in new[] { "Read", "Mismatch", "Type" }) {
            var method = NewMethod(module, name);
            method.MethodSig = MethodSig.CreateStatic(
                name == "Type" ? module.Import(typeof(Type)).ToTypeSig() : module.CorLibTypes.String,
                new SZArraySig(module.CorLibTypes.String));
            var il = method.Body.Instructions;
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add(OpCodes.Ldc_I4_0.ToInstruction());
            il.Add(OpCodes.Ldelema.ToInstruction(module.CorLibTypes.String.TypeDefOrRef));
            il.Add(OpCodes.Mkrefany.ToInstruction(module.CorLibTypes.String.TypeDefOrRef));
            il.Add(OpCodes.Dup.ToInstruction());
            il.Add(OpCodes.Refanyval.ToInstruction(module.CorLibTypes.String.TypeDefOrRef));
            il.Add(OpCodes.Ldstr.ToInstruction("changed"));
            il.Add(OpCodes.Stind_Ref.ToInstruction());
            il.Add(OpCodes.Ldc_I4_2.ToInstruction());
            il.Add(OpCodes.Ldc_I4_1.ToInstruction());
            il.Add(OpCodes.Ldc_I4_1.ToInstruction());
            il.Add(OpCodes.Ldc_I4_1.ToInstruction());
            il.Add(OpCodes.Call.ToInstruction(module.Import(typeof(GC).GetMethod(nameof(GC.Collect),
                [typeof(int), typeof(GCCollectionMode), typeof(bool), typeof(bool)])!)));
            if (name == "Type") {
                il.Add(OpCodes.Refanytype.ToInstruction());
                il.Add(OpCodes.Call.ToInstruction(module.Import(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!)));
            }
            else {
                il.Add(OpCodes.Refanyval.ToInstruction(name == "Mismatch"
                    ? module.CorLibTypes.Int32.TypeDefOrRef : module.CorLibTypes.String.TypeDefOrRef));
                if (name == "Mismatch") {
                    il.Add(OpCodes.Pop.ToInstruction());
                    il.Add(OpCodes.Ldnull.ToInstruction());
                }
                else
                    il.Add(OpCodes.Ldind_Ref.ToInstruction());
            }
            il.Add(OpCodes.Ret.ToInstruction());
            methods.Add(method);
        }
        var before = Write(module).GetType("Tests.Program")!;
        var context = new ObfuscationContext(module, new Random(119));
        new VirtualizationProtection().Initialize(context);
        Assert.All(methods, method => Assert.Contains(method.Body.Instructions,
            i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame)));
        if (rename)
            new RenameInjectedProtection().Initialize(context);
        var after = Write(module).GetType("Tests.Program")!;
        foreach (var method in methods) {
            var originalValues = new[] { "before" };
            var actualValues = new[] { "before" };
            var expectedError = Record.Exception(() => before.GetMethod(method.Name)!.Invoke(null, [originalValues]));
            object? result = null;
            var actualError = Record.Exception(() => result = after.GetMethod(method.Name)!.Invoke(null, [actualValues]));
            Assert.Equal((expectedError as TargetInvocationException)?.InnerException?.GetType(),
                (actualError as TargetInvocationException)?.InnerException?.GetType());
            Assert.Equal(originalValues, actualValues);
            if (method.Name == "Mismatch")
                Assert.IsType<InvalidCastException>(Assert.IsType<TargetInvocationException>(actualError).InnerException);
            else {
                Assert.Null(actualError);
                Assert.Equal(method.Name == "Type" ? typeof(string) : (object)"changed", result);
            }
        }
    }

    [Theory]
    [InlineData(Code.Conv_I1)]
    [InlineData(Code.Conv_U1)]
    [InlineData(Code.Conv_I2)]
    [InlineData(Code.Conv_U2)]
    [InlineData(Code.Conv_I4)]
    [InlineData(Code.Conv_U4)]
    [InlineData(Code.Conv_Ovf_I1)]
    [InlineData(Code.Conv_Ovf_U1)]
    [InlineData(Code.Conv_Ovf_I2)]
    [InlineData(Code.Conv_Ovf_U2)]
    [InlineData(Code.Conv_Ovf_I4)]
    [InlineData(Code.Conv_Ovf_U4)]
    [InlineData(Code.Conv_Ovf_I1_Un)]
    [InlineData(Code.Conv_Ovf_U1_Un)]
    [InlineData(Code.Conv_Ovf_I2_Un)]
    [InlineData(Code.Conv_Ovf_U2_Un)]
    [InlineData(Code.Conv_Ovf_I4_Un)]
    [InlineData(Code.Conv_Ovf_U4_Un)]
    public void Each_Conversion_Matches_Clr(Code code) {
        using var module = NewTarget();
        var method = NewMethod(module, "Convert", 1);
        var opcode = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)!).Single(op => op.Code == code);
        method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        method.Body.Instructions.Add(opcode.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var before = Write(module).GetType("Tests.Program")!.GetMethod("Convert")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(41)));
        Assert.Contains(method.Body.Instructions, i => i.Operand is IMethod m && m.Name == nameof(VmRunner.CreateFrame));
        var after = Write(module).GetType("Tests.Program")!.GetMethod("Convert")!;
        foreach (var value in new[] { int.MinValue, -65536, -32769, -129, -128, -1, 0, 127, 128, 255, 256, 32767, 32768, 65535, 65536, int.MaxValue })
            AssertSameResult(before, after, [value]);
    }

    [Fact]
    public void Switch_Preserves_Duplicate_Targets_And_Default_Path() {
        using var module = NewTarget();
        var method = NewMethod(module, "Select", 1);
        var first = OpCodes.Ldc_I4.ToInstruction(11);
        var second = OpCodes.Ldc_I4.ToInstruction(22);
        var il = method.Body.Instructions;
        il.Add(OpCodes.Ldarg_0.ToInstruction());
        il.Add(OpCodes.Switch.ToInstruction(new[] { first, second, first }));
        il.Add(OpCodes.Ldc_I4.ToInstruction(-9));
        il.Add(OpCodes.Ret.ToInstruction());
        il.Add(first);
        il.Add(OpCodes.Ret.ToInstruction());
        il.Add(second);
        il.Add(OpCodes.Ret.ToInstruction());
        var before = Write(module).GetType("Tests.Program")!.GetMethod("Select")!;
        new VirtualizationProtection().Initialize(new ObfuscationContext(module, new Random(15)));
        Assert.DoesNotContain(method.Body.Instructions, i => i.OpCode == OpCodes.Switch);
        var after = Write(module).GetType("Tests.Program")!.GetMethod("Select")!;
        foreach (var value in new[] { int.MinValue, -1, 0, 1, 2, 3, int.MaxValue })
            AssertSameResult(before, after, [value]);
    }

    [Fact]
    public void ProductionVm_Has_No_RuntimeCodeGenerationDependency() {
        var assembly = typeof(VmRunner).Assembly;
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
            reference.Name is "System.Reflection.Emit" or "System.Reflection.Emit.ILGeneration");
        Assert.DoesNotContain(assembly.GetTypes(), type =>
            type.Namespace == "System.Reflection.Emit" || type.FullName?.Contains("DynamicMethod", StringComparison.Ordinal) == true);

        var module = ModuleDefMD.Load(assembly.Location);
        var runtimeCodegen = module.GetTypes().SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .SelectMany(instruction => instruction.Operand switch {
                IMethod method => new ITypeDefOrRef[] { method.DeclaringType },
                IField field => new ITypeDefOrRef[] { field.DeclaringType },
                ITypeDefOrRef type => new[] { type },
                _ => Array.Empty<ITypeDefOrRef>(),
            })
            .Where(type => type.FullName is "System.Reflection.Emit.AssemblyBuilder"
                or "System.Reflection.Emit.ModuleBuilder"
                or "System.Reflection.Emit.TypeBuilder"
                or "System.Linq.Expressions.LambdaExpression")
            .ToArray();
        Assert.Empty(runtimeCodegen);
        var forbidden = module.GetTypes().SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .Select(instruction => instruction.Operand)
            .OfType<IMethod>()
            .Where(method => method.DeclaringType.FullName is "System.Activator" or "System.Type" &&
                method.Name.ToString() is "CreateInstance" or "MakeGenericType" or "MakeGenericMethod")
            .ToArray();
        Assert.Empty(forbidden);
    }

    [Fact]
    public void ProductionCompiler_Does_NotReference_NativePrototype() {
        var module = ModuleDefMD.Load(typeof(VirtualizationProtection).Assembly.Location);
        var references = module.GetTypes().SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .SelectMany(instruction => instruction.Operand switch {
                IMethod method => new ITypeDefOrRef[] { method.DeclaringType },
                IField field => new ITypeDefOrRef[] { field.DeclaringType },
                ITypeDefOrRef type => new[] { type },
                _ => Array.Empty<ITypeDefOrRef>(),
            })
            .Where(type => type.FullName == "SharpHide.Vm.VmNativeCode")
            .ToArray();

        Assert.Empty(references);
    }

    [Fact]
    public void ProductionVm_Does_Not_Emit_Runtime_Il() {
        var module = ModuleDefMD.Load(typeof(VmRunner).Assembly.Location);
        var forbidden = module.GetTypes().SelectMany(type => type.Methods)
            .Where(method => method.HasBody)
            .SelectMany(method => method.Body.Instructions)
            .SelectMany(instruction => instruction.Operand switch {
                IMethod method => new ITypeDefOrRef[] { method.DeclaringType },
                IField field => new ITypeDefOrRef[] { field.DeclaringType },
                ITypeDefOrRef type => new[] { type },
                _ => Array.Empty<ITypeDefOrRef>(),
            })
            .Where(type => type.FullName is "System.Reflection.Emit.DynamicMethod" or "System.Reflection.Emit.ILGenerator")
            .ToArray();

        Assert.Empty(forbidden);
    }
}
