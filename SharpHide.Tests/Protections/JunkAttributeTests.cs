using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class JunkAttributeTests {
    private static ModuleDef NewTarget() {
        var name = "Junk_" + Guid.NewGuid().ToString("N");
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

    private static void SetOptions(ObfuscationContext context, string protection, Dictionary<string, double> knobs) {
        var options = new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection] = knobs };
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!
            .SetValue(context, (IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>>)options);
    }

    [Fact]
    public void Decoy_Attributes_Are_Valid_And_Inert() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(77));
        SetOptions(context, "junk-attributes",
            new Dictionary<string, double> { ["rate"] = 1, ["types"] = 4, ["max-per-member"] = 3 });
        new JunkAttributeProtection().Initialize(context);

        var assembly = Write(module);

        Assert.Equal(4, assembly.GetTypes().Count(t => t.IsSubclassOf(typeof(Attribute))));

        var sampleType = assembly.GetType(typeof(Sample).FullName!)!;
        Assert.NotEmpty(sampleType.GetCustomAttributesData());

        var compute = sampleType.GetMethod(nameof(Sample.Compute))!;
        for (var seed = 0; seed < 8; seed++)
            Assert.Equal(Sample.Compute(seed), compute.Invoke(null, new object[] { seed }));
    }
}
