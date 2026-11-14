using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

// Xor-encrypts embedded data resources and decrypts streams at user call sites.
public sealed class ResourceEncryptionProtection : Protection {
    public override long Priority => 78;
    public override string Name => "resource-encryption";
    public override string Description => "Encrypts embedded resources and decrypts them at the access site";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;

        var guard = module.Inject(typeof(ResourceGuard));
        context.Runtime.Add(guard.EntryType!);
        var unwrap = guard.ResolveMethod(typeof(ResourceGuard).GetMethod(nameof(ResourceGuard.Unwrap))!)!;

        var encrypted = 0;
        foreach (var resource in module.Resources.OfType<EmbeddedResource>().ToList()) {
            if (context.RuntimeResources.Contains(resource))
                continue;

            var scrambled = ResourceGuard.Scramble(resource.CreateReader().ToArray());
            module.Resources.Remove(resource);
            module.Resources.Add(new EmbeddedResource(resource.Name, scrambled, resource.Attributes));
            encrypted++;
        }

        var wrapped = 0;
        foreach (var method in context.EncryptableMethods()) {
            var instructions = method.Body.Instructions;
            for (var i = 0; i < instructions.Count; i++) {
                if (instructions[i].Operand is not IMethod target
                    || target.Name != "GetManifestResourceStream"
                    || target.MethodSig?.RetType.FullName != "System.IO.Stream")
                    continue;

                instructions.Insert(i + 1, OpCodes.Call.ToInstruction(unwrap));
                i++;
                wrapped++;
            }
        }

        context.Log($"    {encrypted} resources encrypted, {wrapped} access sites wrapped");
    }
}
