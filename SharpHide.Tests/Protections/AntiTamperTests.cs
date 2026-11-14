using System.Reflection;
using System.Security.Cryptography;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Tests;

public class AntiTamperTests {
    private static string WriteWithTrailer(bool corrupt, bool mutateBody = false) {
        var name = "Tamper_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);
        module.Inject(typeof(Integrity));

        var path = Path.Combine(Path.GetTempPath(), name + ".dll");
        var key = SHA256.HashData(Guid.NewGuid().ToByteArray());

        using (var stream = new MemoryStream()) {
            new ModuleWriter(module, new ModuleWriterOptions(module)).Write(stream);
            var bytes = stream.ToArray();
            var trailer = Integrity.Trailer(bytes, key);
            if (corrupt)
                trailer[^1] ^= 0xFF;
            var output = bytes.Concat(trailer).ToArray();
            if (mutateBody)
                output[bytes.Length - 1] ^= 0xFF;
            File.WriteAllBytes(path, output);
        }

        var assembly = Assembly.LoadFile(path);
        assembly.GetType(typeof(Integrity).FullName!)!.GetField(nameof(Integrity.AuthenticationKey))!
            .SetValue(null, Convert.ToBase64String(key));
        return path;
    }

    private static bool IsIntact(string path) {
        var assembly = Assembly.LoadFile(path);
        return (bool)assembly.GetType(typeof(Integrity).FullName!)!.GetMethod(nameof(Integrity.IsIntact))!
            .Invoke(null, null)!;
    }

    [Fact]
    public void Trailer_Loads_And_Detects_Tampering() {
        Assert.True(IsIntact(WriteWithTrailer(corrupt: false)));

        Assert.False(IsIntact(WriteWithTrailer(corrupt: true)));
    }

    [Fact]
    public void Body_Byte_Changes_Are_Rejected() {
        Assert.False(IsIntact(WriteWithTrailer(corrupt: false, mutateBody: true)));
    }

    [Fact]
    public void Wrong_Authentication_Key_Is_Rejected() {
        var path = WriteWithTrailer(corrupt: false);
        var assembly = Assembly.LoadFile(path);
        assembly.GetType(typeof(Integrity).FullName!)!.GetField(nameof(Integrity.AuthenticationKey))!
            .SetValue(null, Convert.ToBase64String(new byte[32]));

        Assert.False(IsIntact(path));
    }

    [Fact]
    public void Integrity_Diagnostics_Use_Managed_Exception_Path() {
        var previousStrict = Integrity.Strict;
        var previousDiagnostics = Integrity.Diagnostics;
        var previousKey = Integrity.AuthenticationKey;
        Integrity.Strict = true;
        Integrity.Diagnostics = true;
        Integrity.AuthenticationKey = string.Empty;
        try {
            Assert.Throws<BadImageFormatException>(Integrity.Verify);
        }
        finally {
            Integrity.Strict = previousStrict;
            Integrity.Diagnostics = previousDiagnostics;
            Integrity.AuthenticationKey = previousKey;
        }
    }

    [Fact]
    public void Protection_Injects_Keyed_Startup_And_Distributed_Checks() {
        using var module = new ModuleDefUser("Checks.dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser("Checks", new Version(1, 0, 0, 0)).Modules.Add(module);
        var type = new TypeDefUser("Tests", "Targets", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        for (var index = 0; index < 4; index++) {
            var method = new MethodDefUser("Method" + index, MethodSig.CreateStatic(module.CorLibTypes.Void),
                dnlib.DotNet.MethodImplAttributes.IL,
                dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            type.Methods.Add(method);
        }
        var protection = new AntiTamperProtection();
        var options = new Dictionary<string, double>(protection.DefaultOptions) { ["method-checks"] = 2 };
        var context = new ObfuscationContext(module, new Random(15));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> { [protection.Name] = options });

        protection.Initialize(context);

        Assert.Equal(32, context.IntegrityKey!.Length);
        Assert.True(context.RequiresIntegrityTrailer);
        Assert.Equal(1, context.Runtime.Count(runtime => runtime.Methods.Any(method => method.Name == nameof(Integrity.Verify))));
        Assert.Equal(2, type.Methods.Count(method => method.Body.Instructions.Any(instruction =>
            instruction.OpCode == OpCodes.Call && instruction.Operand is IMethod target &&
            target.Name == nameof(Integrity.Verify))));
        Assert.Contains(module.GlobalType.FindStaticConstructor()!.Body.Instructions, instruction =>
            instruction.OpCode == OpCodes.Stsfld && instruction.Operand is IField field &&
            field.Name == nameof(Integrity.AuthenticationKey));
        Assert.Contains(module.GlobalType.FindStaticConstructor()!.Body.Instructions, instruction =>
            instruction.OpCode == OpCodes.Stsfld && instruction.Operand is IField field &&
            field.Name == nameof(Integrity.Diagnostics));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(2d)]
    [InlineData(0.5d)]
    public void Strict_Option_Requires_Binary_Value(double value) {
        using var module = new ModuleDefUser("Strict.dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser("Strict", new Version(1, 0, 0, 0)).Modules.Add(module);
        var protection = new AntiTamperProtection();
        var context = new ObfuscationContext(module, new Random(15));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> {
                [protection.Name] = new Dictionary<string, double>(protection.DefaultOptions) { ["strict"] = value },
            });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(2d)]
    [InlineData(0.5d)]
    public void Diagnostics_Option_Requires_Binary_Value(double value) {
        using var module = new ModuleDefUser("Diagnostics.dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser("Diagnostics", new Version(1, 0, 0, 0)).Modules.Add(module);
        var protection = new AntiTamperProtection();
        var context = new ObfuscationContext(module, new Random(15));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> {
                [protection.Name] = new Dictionary<string, double>(protection.DefaultOptions) { ["diagnostics"] = value },
            });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(65d)]
    [InlineData(0.5d)]
    public void MethodChecks_Reject_Invalid_Values_Before_Mutation(double value) {
        using var module = new ModuleDefUser("MethodChecks.dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser("MethodChecks", new Version(1, 0, 0, 0)).Modules.Add(module);
        var type = new TypeDefUser("Tests", "Targets", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        var method = new MethodDefUser("Method", MethodSig.CreateStatic(module.CorLibTypes.Void),
            dnlib.DotNet.MethodImplAttributes.IL,
            dnlib.DotNet.MethodAttributes.Public | dnlib.DotNet.MethodAttributes.Static) { Body = new CilBody() };
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(method);
        var body = method.Body;
        var protection = new AntiTamperProtection();
        var context = new ObfuscationContext(module, new Random(15));
        typeof(ObfuscationContext).GetProperty(nameof(ObfuscationContext.Options))!.SetValue(context,
            new Dictionary<string, IReadOnlyDictionary<string, double>> {
                [protection.Name] = new Dictionary<string, double>(protection.DefaultOptions) { ["method-checks"] = value },
            });

        Assert.Throws<ArgumentOutOfRangeException>(() => protection.Initialize(context));
        Assert.Same(body, method.Body);
        Assert.Empty(context.Runtime);
    }
}
