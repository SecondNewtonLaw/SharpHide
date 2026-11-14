using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Payloads;

namespace SharpHide.Tests;

public class AntiDumpTests {
    [Fact]
    public void Neutralize_Clears_The_MZ_Signature() {
        var image = new byte[] { (byte)'M', (byte)'Z', 0x90, 0x00 };
        AntiDump.Neutralize(image);

        Assert.Equal(0, image[0]);
        Assert.Equal(0, image[1]);
        Assert.Equal(0x90, image[2]);
    }

    [Fact]
    public void Installs_A_Module_Initializer() {
        var name = "Dump_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);

        var context = new ObfuscationContext(module, new Random(1));
        new AntiDumpProtection().Initialize(context);

        Assert.Contains(module.GetTypes(), t => t.Name == nameof(AntiDump));
        var cctor = module.GlobalType.FindStaticConstructor();
        Assert.NotNull(cctor);
        Assert.Contains(cctor!.Body.Instructions, i => i.OpCode == OpCodes.Call);
    }
}
