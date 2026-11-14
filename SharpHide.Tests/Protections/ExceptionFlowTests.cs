using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class ExceptionFlowTests {
    private static ModuleDef NewTarget() {
        var name = "Eh_" + Guid.NewGuid().ToString("N");
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

    [Fact]
    public void Wrapped_Bodies_Keep_A_Handler_And_Their_Result() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(5));
        var options = new Dictionary<string, IReadOnlyDictionary<string, double>> {
            ["exception-flow"] = new Dictionary<string, double> { ["min-size"] = 4 },
        };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!
            .SetValue(context, (IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>)options);
        new ExceptionFlowProtection().Initialize(context);

        var compute = module.GetTypes().SelectMany(t => t.Methods).First(m => m.Name == nameof(Sample.Compute));
        Assert.NotEmpty(compute.Body.ExceptionHandlers);

        var loaded = Write(module).GetType(typeof(Sample).FullName!)!.GetMethod(nameof(Sample.Compute))!;
        for (var seed = 0; seed < 12; seed++)
            Assert.Equal(Sample.Compute(seed), loaded.Invoke(null, new object[] { seed }));
    }
}
