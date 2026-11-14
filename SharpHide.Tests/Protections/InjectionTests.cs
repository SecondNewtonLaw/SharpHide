using System.Reflection;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Tests;

public class InjectionTests {
    private static ModuleDef NewTarget() {
        var name = "Injected_" + Guid.NewGuid().ToString("N");
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

    private static TypeDef NewContainer(ModuleDef module, string name) {
        var type = new TypeDefUser("SharpHide.Rolled", name, module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.Class
                       | dnlib.DotNet.TypeAttributes.Abstract | dnlib.DotNet.TypeAttributes.Sealed,
        };
        module.Types.Add(type);
        return type;
    }

    private static string RandomString(Random rng) {
        var length = rng.Next(0, 48);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = (char)rng.Next(0x20, 0x7f);
        return new string(chars);
    }

    [Fact]
    public void DeepClone_RoundTrips_And_Executes() {
        var module = NewTarget();
        module.Inject(typeof(Sample));

        var assembly = Write(module);
        var type = assembly.GetType(typeof(Sample).FullName!)!;
        var compute = type.GetMethod(nameof(Sample.Compute), BindingFlags.Public | BindingFlags.Static)!;

        Assert.Equal(Sample.Compute(5), compute.Invoke(null, new object[] { 5 }));
    }

    [Fact]
    public void ModuleInitializer_Runs_At_Load() {
        var module = NewTarget();
        module.InjectModuleInitializer(typeof(Boot).GetMethod(nameof(Boot.Initialize))!);

        var assembly = Write(module);
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        var ran = (int)assembly.GetType(typeof(Boot).FullName!)!
            .GetField(nameof(Boot.Ran))!.GetValue(null)!;

        Assert.Equal(42, ran);
    }

    [Fact]
    public void InjectResource_Embeds_Bytes() {
        var module = NewTarget();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        module.InjectResource("payload.bin", data);

        var assembly = Write(module);
        using var stream = assembly.GetManifestResourceStream("payload.bin")!;
        var buffer = new byte[data.Length];
        stream.ReadExactly(buffer);

        Assert.Equal(data, buffer);
    }

    [Fact]
    public void Dedup_Reuses_One_Copy() {
        var module = NewTarget();
        var first = module.Inject(typeof(Sample));
        var second = module.Inject(typeof(Sample));

        Assert.Same(first.EntryType, second.EntryType);
        Assert.Single(module.Types, injected => injected.Name == "Sample");
    }

    [Fact]
    public void InjectResult_Resolves_Handle_To_Injected_Def() {
        var module = NewTarget();
        var result = module.Inject(typeof(Sample));

        var injected = result.ResolveMethod(typeof(Sample).GetMethod(nameof(Sample.Compute))!);

        Assert.NotNull(injected);
        Assert.Equal(nameof(Sample.Compute), injected!.Name);
        Assert.Same(module, injected.Module);
    }

    [Fact]
    public void Dynamic_Payloads_Inject_And_Load() {
        var module = NewTarget();
        module.Inject(typeof(AntiDebug));
        module.Inject(typeof(DeadCode));

        var assembly = Write(module);

        Assert.NotNull(assembly.GetType(typeof(AntiDebug).FullName!));
        Assert.NotNull(assembly.GetType(typeof(DeadCode).FullName!));
    }

    [Fact]
    public void CallHider_Resolves_Hidden_Target_With_Runtime_Seed() {
        var module = NewTarget();
        module.Inject(typeof(Sample));
        module.Inject(typeof(CallHider));

        var assembly = Write(module);
        var compute = assembly.GetType(typeof(Sample).FullName!)!.GetMethod(nameof(Sample.Compute))!;
        var invoke = assembly.GetType(typeof(CallHider).FullName!)!.GetMethod(nameof(CallHider.Invoke))!;

        const int seed = 0x5EED17;
        var encoded = CallHider.Encode(compute.MetadataToken, seed);

        var result = invoke.Invoke(null, new object?[] { encoded, seed, null, new object[] { 5 } });

        Assert.Equal(Sample.Compute(5), result);
    }

    [Fact]
    public void Rolled_Cipher_Splices_Passes_Into_One_Body_And_RoundTrips() {
        var module = NewTarget();
        var recipe = StringCipherRoller.Roll(new Random(1234));
        var decryptDef = StringCipherRoller.EmitDecrypt(module, recipe);

        Assert.DoesNotContain(decryptDef.Body.Instructions,
            i => i.Operand is IMethodDefOrRef m && m.DeclaringType?.Name == nameof(CipherPasses));

        var assembly = Write(module);
        var decrypt = assembly.ManifestModule.GetMethod(decryptDef.Name)!;

        const string secret = "cluster me £¥ into one body";
        Assert.Equal(secret, decrypt.Invoke(null, new object[] { StringCipherRoller.Encrypt(secret, recipe) }));
    }

    [Fact]
    public void Rolled_Cipher_With_Dynamic_Junk_Blows_Up_And_RoundTrips() {
        var module = NewTarget();
        var recipe = StringCipherRoller.Roll(new Random(7), includeJunk: true);
        var decryptDef = StringCipherRoller.EmitDecrypt(module, recipe);

        var assembly = Write(module);
        var decrypt = assembly.ManifestModule.GetMethod(decryptDef.Name)!;

        const string secret = "😂🤣 dynamic junk should not change the result";
        Assert.Equal(secret, decrypt.Invoke(null, new object[] { StringCipherRoller.Encrypt(secret, recipe) }));

        Assert.Contains(assembly.GetReferencedAssemblies(), a => a.Name == "Microsoft.CSharp");
        Assert.True(decryptDef.Body.Instructions.Count > 300);
    }

    [Fact]
    public void Rolled_Cipher_RoundTrips_Across_Many_Random_Recipes() {
        var module = NewTarget();
        var rng = new Random(20260906);
        const int handlers = 256;
        var cases = new List<(TypeDef Container, string MethodName, string Secret, string Encrypted)>();

        for (var i = 0; i < handlers; i++) {
            var recipe = StringCipherRoller.Roll(rng, includeJunk: rng.Next(2) == 0);
            var container = NewContainer(module, $"Handler{i}");
            var decrypt = StringCipherRoller.EmitDecrypt(module, recipe, container);
            var secret = RandomString(rng);
            cases.Add((container, decrypt.Name, secret, StringCipherRoller.Encrypt(secret, recipe)));
        }

        var assembly = Write(module);

        foreach (var (container, methodName, secret, encrypted) in cases) {
            var decrypt = assembly.GetType(container.FullName!)!.GetMethod(methodName)!;
            Assert.Equal(secret, decrypt.Invoke(null, new object[] { encrypted }));
        }
    }
}
