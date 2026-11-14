using System.Diagnostics;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class MetadataAttributeTests {
    private static ModuleDef NewTarget() {
        var name = "Meta_" + Guid.NewGuid().ToString("N");
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

    [Fact]
    public void Debugger_And_Misleading_Attributes_Are_Valid_And_Inert() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var context = new ObfuscationContext(module, new Random(9));
        new DebuggerHiddenProtection().Initialize(context);

        var assembly = Write(module);
        var sampleType = assembly.GetType(typeof(Sample).FullName!)!;

        var compute = sampleType.GetMethod(nameof(Sample.Compute))!;
        var methodAttrs = compute.GetCustomAttributesData().Select(a => a.AttributeType).ToList();
        Assert.Contains(typeof(DebuggerHiddenAttribute), methodAttrs);
        Assert.Contains(typeof(DebuggerStepThroughAttribute), methodAttrs);
        Assert.Contains(typeof(DebuggerNonUserCodeAttribute), methodAttrs);

        var payload = sampleType.GetField("Payload", BindingFlags.NonPublic | BindingFlags.Static)!;
        var browsable = payload.GetCustomAttributesData()
            .SingleOrDefault(a => a.AttributeType == typeof(DebuggerBrowsableAttribute));
        Assert.NotNull(browsable);
        Assert.Equal(DebuggerBrowsableState.Never, (DebuggerBrowsableState)browsable!.ConstructorArguments[0].Value!);

        Assert.Contains(typeof(DebuggerDisplayAttribute),
            sampleType.GetCustomAttributesData().Select(a => a.AttributeType));
        Assert.Contains(typeof(ObfuscationAttribute), assembly.GetCustomAttributesData().Select(a => a.AttributeType));

        for (var seed = 0; seed < 8; seed++)
            Assert.Equal(Sample.Compute(seed), compute.Invoke(null, new object[] { seed }));
    }
}
