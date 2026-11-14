using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;

namespace SharpHide.Tests;

public class ResourceEncryptionTests {
    private static ModuleDef NewTarget() {
        var name = "Res_" + Guid.NewGuid().ToString("N");
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
    public void Runtime_Resources_Are_Skipped_By_Identity() {
        using var module = NewTarget();
        var owned = module.InjectResource("runtime-data", [42, 1, 2, 3]);
        var user = module.InjectResource("SharpHide.ConstantPool", [42, 1, 2, 3]);
        var context = new ObfuscationContext(module, new Random(1));
        context.RuntimeResources.Add(owned);
        new ResourceEncryptionProtection().Initialize(context);
        Assert.Contains(owned, module.Resources);
        Assert.Equal(new byte[] { 42, 1, 2, 3 }, owned.CreateReader().ToArray());
        var encrypted = Assert.Single(module.Resources.OfType<EmbeddedResource>(), r => r.Name == user.Name);
        Assert.NotEqual(user.CreateReader().ToArray(), encrypted.CreateReader().ToArray());
    }

    [Fact]
    public void Encrypted_Resource_RoundTrips_Through_The_Access_Site() {
        var module = NewTarget();
        module.Inject(typeof(ResourceReader));
        module.Resources.Add(new EmbeddedResource("data", new byte[] { 42, 1, 2, 3 }));

        var context = new ObfuscationContext(module, new Random(1));
        new ResourceEncryptionProtection().Initialize(context);

        var stored = module.Resources.OfType<EmbeddedResource>().First(r => r.Name == "data").CreateReader().ToArray();
        Assert.NotEqual(42, stored[0]);

        var read = Write(module).GetType(typeof(ResourceReader).FullName!)!.GetMethod(nameof(ResourceReader.ReadFirst))!;
        Assert.Equal(42, read.Invoke(null, null));
    }
}
