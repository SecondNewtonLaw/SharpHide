using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class NumberEncryptionTests {
    private static ModuleDef NewTarget() {
        var name = "Numbers_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) {
            Kind = ModuleKind.Dll,
            RuntimeVersion = "v4.0.30319",
        };
        var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
        assembly.Modules.Add(module);
        return module;
    }

    private static Assembly Write(ModuleDef module) {
        foreach (var type in module.GetTypes())
        foreach (var method in type.Methods) {
            if (!method.HasBody)
                continue;

            method.Body.SimplifyBranches();
            if (MaxStackCalculator.GetMaxStack(method.Body.Instructions, method.Body.ExceptionHandlers, out var max))
                method.Body.MaxStack = (ushort)max;
        }

        using var stream = new MemoryStream();
        new ModuleWriter(module, new ModuleWriterOptions(module)).Write(stream);
        return Assembly.Load(stream.ToArray());
    }

    private static void SetOptions(ObfuscationContext context, string protection, Dictionary<string, double> knobs) {
        var options = new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection] = knobs };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!
            .SetValue(context, (IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>)options);
    }

    [Fact]
    public void Every_Decoder_Family_Preserves_Both_Integer_Widths() {
        using var module = NewTarget();
        var type = new TypeDefUser("Tests", "Literals", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        var expected = new Dictionary<string, object>();
        var rng = new Random(431);
        foreach (var wide in new[] { false, true }) {
            var values = new List<long> { long.MinValue, long.MaxValue, int.MinValue, int.MaxValue, -1, 0, 1, 8, 9 };
            values.AddRange(Enumerable.Range(0, 128).Select(_ => rng.NextInt64(long.MinValue, long.MaxValue)));
            foreach (var value in values) {
                var name = "Value" + expected.Count;
                expected.Add(name, wide ? (object)value : unchecked((int)value));
                var method = new MethodDefUser(name,
                    MethodSig.CreateStatic(wide ? module.CorLibTypes.Int64 : module.CorLibTypes.Int32),
                    dnlib.DotNet.MethodImplAttributes.IL, dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) {
                    Body = new CilBody(),
                };
                method.Body.Instructions.Add(wide ? OpCodes.Ldc_I8.ToInstruction(value) : OpCodes.Ldc_I4.ToInstruction(unchecked((int)value)));
                method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
                type.Methods.Add(method);
            }
        }
        var context = new ObfuscationContext(module, new Random(97));
        var protection = new NumberEncryptionProtection();
        SetOptions(context, protection.Name, new Dictionary<string, double>(protection.DefaultOptions));
        protection.Initialize(context);
        var decoders = context.Runtime.SelectMany(t => t.Methods).ToArray();
        Assert.Equal(12, decoders.Length);
        Assert.All(decoders, method => Assert.DoesNotContain(method.Body.Instructions,
            instruction => instruction.OpCode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch));
        var used = type.Methods.SelectMany(m => m.Body.Instructions).Select(i => i.Operand).OfType<MethodDef>().ToHashSet();
        Assert.All(decoders, decoder => Assert.Contains(decoder, used));
        var loaded = Write(module).GetType("Tests.Literals")!;
        foreach (var (name, value) in expected)
            Assert.Equal(value, loaded.GetMethod(name)!.Invoke(null, null));
    }

    [Fact]
    public void Mba_Decoders_RoundTrip_The_Encrypted_Constants() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(2026));
        var protection = new NumberEncryptionProtection();
        SetOptions(context, protection.Name, new Dictionary<string, double>(protection.DefaultOptions));
        protection.Initialize(context);

        var compute = Write(module).GetType(typeof(Sample).FullName!)!.GetMethod(nameof(Sample.Compute))!;
        for (var seed = 0; seed < 12; seed++)
            Assert.Equal(Sample.Compute(seed), compute.Invoke(null, new object[] { seed }));

        var decoders = module.GetTypes().SelectMany(t => t.Methods).Where(m => m.Name == "Decode").ToList();
        Assert.NotEmpty(decoders);
        var opcodes = decoders.SelectMany(d => d.Body.Instructions).Select(i => i.OpCode.Code).ToHashSet();
        Assert.Contains(Code.Not, opcodes);
        Assert.Contains(Code.And, opcodes);
        Assert.Contains(Code.Or, opcodes);
        Assert.Contains(Code.Xor, opcodes);
    }

    [Fact]
    public void Heavy_Mode_Composes_Selected_Decoder_Layers() {
        using var module = NewTarget();
        var type = new TypeDefUser("Tests", "Heavy", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        var method = new MethodDefUser("Value", MethodSig.CreateStatic(module.CorLibTypes.Int32),
            dnlib.DotNet.MethodImplAttributes.IL,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
        method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(123456789));
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(method);

        var context = new ObfuscationContext(module, new Random(77));
        var protection = new NumberEncryptionProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["layers"] = 4, ["complexity"] = 4 };
        SetOptions(context, protection.Name, options);
        protection.Initialize(context);

        Assert.Equal(4, method.Body.Instructions.Count(instruction => instruction.OpCode == OpCodes.Call));
        var decoder = method.Body.Instructions.Select(instruction => instruction.Operand).OfType<IMethod>()
            .First(target => target.Name == "Decode").ResolveMethodDef()!;
        Assert.True(decoder.Body.Instructions.Count(i => i.OpCode == OpCodes.Not) >= 4);
        Assert.Equal(123456789, Write(module).GetType(type.FullName)!.GetMethod(method.Name)!.Invoke(null, null));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(9d)]
    [InlineData(1.5d)]
    public void Invalid_Layer_Count_Is_Rejected(double value) {
        using var module = NewTarget();
        var protection = new NumberEncryptionProtection();
        var context = new ObfuscationContext(module, new Random(77));
        SetOptions(context, protection.Name,
            new Dictionary<string, double>(protection.DefaultOptions) { ["layers"] = value });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(9d)]
    [InlineData(1.5d)]
    public void Invalid_Mba_Complexity_Is_Rejected(double value) {
        using var module = NewTarget();
        var protection = new NumberEncryptionProtection();
        var context = new ObfuscationContext(module, new Random(77));
        SetOptions(context, protection.Name,
            new Dictionary<string, double>(protection.DefaultOptions) { ["complexity"] = value });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }
}
