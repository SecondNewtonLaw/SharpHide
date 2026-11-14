using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class FlatteningTests {
    private static ModuleDef NewTarget() {
        var name = "Flattened_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) {
            Kind = ModuleKind.Dll,
            RuntimeVersion = "v4.0.30319",
        };
        var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
        assembly.Modules.Add(module);
        return module;
    }

    // Match module-writer branch and MaxStack normalization.
    private static Assembly Write(ModuleDef module) {
        foreach (var type in module.GetTypes())
        foreach (var method in type.Methods) {
            if (!method.HasBody)
                continue;
            method.Body.SimplifyBranches();
            if (MaxStackCalculator.GetMaxStack(method.Body.Instructions, method.Body.ExceptionHandlers, out var max))
                method.Body.MaxStack = (ushort)max;
            else {
                method.Body.KeepOldMaxStack = true;
                method.Body.MaxStack = ushort.MaxValue;
            }
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
    public void Flattened_Methods_Preserve_Semantics() {
        var module = NewTarget();
        module.Inject(typeof(Flowy));

        var context = new ObfuscationContext(module, new Random(99));
        SetOptions(context, "control-flow", new Dictionary<string, double> {
            ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 3, ["opaque-depth"] = 2,
            ["decrypt-hash-chance"] = 0.4, ["handlers"] = 2,
        });
        new ControlFlowProtection().Initialize(context);

        Assert.All(context.Runtime, type => {
            Assert.Contains(type.Name.String, ch => ch > 127);
            foreach (var member in type.Methods)
                Assert.Contains(member.Name.String, ch => ch > 127);
            foreach (var field in type.Fields)
                Assert.Contains(field.Name.String, ch => ch > 127);
        });

        var flowy = Write(module).GetType(typeof(Flowy).FullName!)!;
        var sum = flowy.GetMethod(nameof(Flowy.Sum))!;
        var fib = flowy.GetMethod(nameof(Flowy.Fib))!;
        var classify = flowy.GetMethod(nameof(Flowy.Classify))!;

        for (var n = -3; n <= 20; n++) {
            Assert.Equal(Flowy.Sum(n), sum.Invoke(null, new object[] { n }));
            Assert.Equal(Flowy.Fib(n), fib.Invoke(null, new object[] { n }));
            Assert.Equal(Flowy.Classify(n), classify.Invoke(null, new object[] { n }));
        }

        var recur = flowy.GetMethod(nameof(Flowy.Recur))!;
        Assert.Equal(Flowy.Recur(50), recur.Invoke(null, new object[] { 50 }));
    }

    [Fact]
    public void Dispatcher_Size_Follows_Control_Flow_And_Caps_Bogus_States() {
        using var module = NewTarget();
        var type = new TypeDefUser("Tests", "Branches", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        var method = new MethodDefUser("Run", MethodSig.CreateStatic(module.CorLibTypes.Int32, module.CorLibTypes.Int32),
            dnlib.DotNet.MethodImplAttributes.IL, dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) {
            Body = new CilBody(),
        };
        type.Methods.Add(method);
        var other = OpCodes.Ldc_I4.ToInstruction(11);
        method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        method.Body.Instructions.Add(OpCodes.Brfalse.ToInstruction(other));
        for (var i = 0; i < 80; i++) {
            method.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            method.Body.Instructions.Add(OpCodes.Starg.ToInstruction(method.Parameters[0]));
        }
        method.Body.Instructions.Add(OpCodes.Ldc_I4.ToInstruction(22));
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        method.Body.Instructions.Add(other);
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        var context = new ObfuscationContext(module, new Random(34));
        SetOptions(context, "control-flow", new Dictionary<string, double> {
            ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 32,
            ["opaque-depth"] = 3, ["decrypt-hash-chance"] = 0, ["handlers"] = 1,
        });
        new ControlFlowProtection().Initialize(context);
        Assert.Equal(6, method.Body.Instructions.Count(i => i.OpCode.Code is Code.Beq or Code.Beq_S));
        var run = Write(module).GetType("Tests.Branches")!.GetMethod("Run")!;
        Assert.Equal(11, run.Invoke(null, [0]));
        Assert.Equal(22, run.Invoke(null, [7]));
    }

    [Fact]
    public void Flattening_Rewrites_The_Body() {
        var module = NewTarget();
        module.Inject(typeof(Flowy));

        var context = new ObfuscationContext(module, new Random(7));
        SetOptions(context, "control-flow", new Dictionary<string, double> {
            ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 3, ["opaque-depth"] = 2,
            ["decrypt-hash-chance"] = 0.4, ["handlers"] = 2,
        });

        var sum = module.GetTypes().SelectMany(t => t.Methods).First(m => m.Name == nameof(Flowy.Sum));
        var before = sum.Body.Instructions.Count;
        new ControlFlowProtection().Initialize(context);

        Assert.True(sum.Body.Instructions.Count > before);
        Assert.Contains(sum.Body.Instructions, i => i.OpCode == OpCodes.Beq);
    }

    [Fact]
    public void Dispatch_Binds_To_Declaring_Type_Name() {
        var module = NewTarget();
        module.Inject(typeof(Flowy));

        var context = new ObfuscationContext(module, new Random(11));
        SetOptions(context, "control-flow", new Dictionary<string, double> {
            ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 3, ["opaque-depth"] = 2,
            ["decrypt-hash-chance"] = 0.4, ["handlers"] = 2,
        });

        var flowy = module.GetTypes().First(t => t.Name == nameof(Flowy));
        var sum = flowy.Methods.First(m => m.Name == nameof(Flowy.Sum));
        new ControlFlowProtection().Initialize(context);

        Assert.Contains(sum.Body.Instructions, i => i.OpCode == OpCodes.Ldtoken && ReferenceEquals(i.Operand, flowy));
        Assert.Contains(sum.Body.Instructions, i => i.Operand is IMethod m && m.Name == "get_Name");
    }

    [Fact]
    public void Vectorized_Hash_Family_Emits_And_RoundTrips() {
        var module = NewTarget();
        module.Inject(typeof(Flowy));

        var context = new ObfuscationContext(module, new Random(20260908));
        SetOptions(context, "control-flow", new Dictionary<string, double> {
            ["min-blocks"] = 2, ["min-size"] = 4, ["max-size"] = 4000, ["bogus-states"] = 6, ["opaque-depth"] = 1,
            ["decrypt-hash-chance"] = 1, ["handlers"] = 4,
        });
        new ControlFlowProtection().Initialize(context);

        var usesSimd = module.GetTypes()
            .SelectMany(t => t.Methods)
            .Where(m => m.HasBody)
            .SelectMany(m => m.Body.Instructions)
            .Any(i => i.Operand is IMethod m && m.DeclaringType is { } dt
                      && dt.Name.String.StartsWith("Vector128", StringComparison.Ordinal));
        Assert.True(usesSimd);

        var flowy = Write(module).GetType(typeof(Flowy).FullName!)!;
        var sum = flowy.GetMethod(nameof(Flowy.Sum))!;
        var fib = flowy.GetMethod(nameof(Flowy.Fib))!;
        var classify = flowy.GetMethod(nameof(Flowy.Classify))!;

        for (var n = -3; n <= 20; n++) {
            Assert.Equal(Flowy.Sum(n), sum.Invoke(null, new object[] { n }));
            Assert.Equal(Flowy.Fib(n), fib.Invoke(null, new object[] { n }));
            Assert.Equal(Flowy.Classify(n), classify.Invoke(null, new object[] { n }));
        }
    }
}
