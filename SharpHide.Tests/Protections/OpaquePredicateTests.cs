using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class OpaquePredicateTests {
    private static ModuleDef NewTarget() {
        var name = "Opaque_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) {
            Kind = ModuleKind.Dll,
            RuntimeVersion = "v4.0.30319",
        };
        var assembly = new AssemblyDefUser(name, new Version(1, 0, 0, 0));
        assembly.Modules.Add(module);
        return module;
    }

    // Throw-terminated junk needs a fixed MaxStack.
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
    public void Junk_And_Sinks_Are_JIT_Valid_And_Dont_Change_Results() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(4242));
        SetOptions(context, "opaque-predicate",
            new Dictionary<string, double> { ["density"] = 3, ["min-steps"] = 24, ["max-steps"] = 60 });
        new OpaquePredicateProtection().Initialize(context);

        var compute = Write(module).GetType(typeof(Sample).FullName!)!.GetMethod(nameof(Sample.Compute))!;
        for (var seed = 0; seed < 12; seed++)
            Assert.Equal(Sample.Compute(seed), compute.Invoke(null, new object[] { seed }));
    }

    [Fact]
    public void Injects_Impure_Sink_Functions() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(1));
        SetOptions(context, "opaque-predicate",
            new Dictionary<string, double> { ["density"] = 5, ["min-steps"] = 20, ["max-steps"] = 48 });
        new OpaquePredicateProtection().Initialize(context);

        var sinks = module.GetTypes()
            .SelectMany(t => t.Methods)
            .Where(m => m.Name.StartsWith("s") && m.HasBody
                        && m.Body.Instructions.Any(i => i.OpCode == OpCodes.Stsfld))
            .ToList();

        Assert.NotEmpty(sinks);
    }
}
