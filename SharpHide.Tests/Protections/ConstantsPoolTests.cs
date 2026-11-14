using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class ConstantsPoolTests {
    private static ModuleDef NewTarget() {
        var name = "Pool_" + Guid.NewGuid().ToString("N");
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
    [InlineData(false)]
    [InlineData(true)]
    public void Pooled_Literals_RoundTrip_Without_Fixed_Resource_Markers(bool compose) {
        using var module = NewTarget();
        module.Inject(typeof(Sample));
        module.Inject(typeof(ResourceReader));
        module.Inject(typeof(Virtualized));
        module.InjectResource("data", [42, 1, 2, 3]);

        var context = new ObfuscationContext(module, new Random(3));
        var protection = new ConstantsPoolProtection();
        var options = new Dictionary<string, IReadOnlyDictionary<string, double>> {
            [protection.Name] = new Dictionary<string, double>(protection.DefaultOptions),
        };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!
            .SetValue(context, (IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>)options);
        if (compose)
            new VirtualizationProtection().Initialize(context);
        protection.Initialize(context);

        var resource = Assert.Single(module.Resources, r => r.Name != "data");
        Assert.DoesNotContain("SharpHide", resource.Name.String, StringComparison.Ordinal);
        Assert.All(module.GetTypes().SelectMany(t => t.Methods).Where(m => m.HasBody).SelectMany(m => m.Body.Instructions), instruction => {
            if (instruction.OpCode == OpCodes.Ldstr) {
                Assert.NotEqual("SharpHide.ConstantPool", instruction.Operand);
                Assert.NotEqual(resource.Name.String, instruction.Operand);
            }
        });
        if (compose) {
            new ResourceEncryptionProtection().Initialize(context);
            new RenameInjectedProtection().Initialize(context);
        }

        var assembly = Write(module);
        var compute = assembly.GetType(typeof(Sample).FullName!)!.GetMethod(nameof(Sample.Compute))!;
        for (var seed = 0; seed < 12; seed++)
            Assert.Equal(Sample.Compute(seed), compute.Invoke(null, new object[] { seed }));
        var read = assembly.GetType(typeof(ResourceReader).FullName!)!.GetMethod(nameof(ResourceReader.ReadFirst))!;
        Assert.Equal(42, read.Invoke(null, null));
        var virtualized = assembly.GetType(typeof(Virtualized).FullName!)!.GetMethod(nameof(Virtualized.PolySum))!;
        for (var value = -2; value <= 30; value++)
            Assert.Equal(Virtualized.PolySum(value), virtualized.Invoke(null, [value]));
    }

    [Fact]
    public void Pool_Resource_Names_Vary_With_The_Build_Seed() {
        var names = new HashSet<string>();
        foreach (var seed in new[] { 3, 97, 141 }) {
            using var module = NewTarget();
            module.Inject(typeof(Sample));
            var context = new ObfuscationContext(module, new Random(seed));
            var protection = new ConstantsPoolProtection();
            typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
                new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = protection.DefaultOptions });
            protection.Initialize(context);
            Assert.True(names.Add(Assert.Single(module.Resources).Name.String));
        }
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(65d)]
    [InlineData(1.5d)]
    public void Invalid_Rounds_Are_Rejected(double value) {
        using var module = NewTarget();
        var protection = new ConstantsPoolProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["rounds"] = value };
        var context = new ObfuscationContext(module, new Random(3));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }
}
