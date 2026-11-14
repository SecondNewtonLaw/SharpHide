using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class JunkTypeProtection : Protection {
    public override long Priority => 65;
    public override string Name => "junk-type";
    public override string Description => "Injects standalone junk types that crash decompilers";

    // Type count and dead-block copies.
    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["types"] = 4, ["copies"] = 2,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["types"] = "Number of junk types injected. Typical 2-6.",
        ["copies"] = "Dead-code copies per junk method. Typical 1-3.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var injector = InjectionExtensions.ContextFor(module);
        var decoy = InjectionExtensions.ResolveMethod(typeof(DecoyBlock).GetMethod(nameof(DecoyBlock.Run))!);

        var types = Math.Max(1, (int)Option(context, "types"));
        var copies = Math.Max(0, (int)Option(context, "copies"));
        for (var i = 0; i < types; i++) {
            var method = new MethodDefUser(context.NewName(), MethodSig.CreateStatic(module.CorLibTypes.Void),
                MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static);
            method.Body = new CilBody();
            method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
            context.NewContainer().Methods.Add(method);
            injector.PrependDead(method, decoy, copies);
        }

        context.Log($"    {types} junk types");
    }
}
