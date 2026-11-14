using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class JunkMethodProtection : Protection {
    public override long Priority => 64;
    public override string Name => "junk-method";
    public override string Description => "Prepends dead crash-junk into valid user methods";

    // Method rate and dead-block copies.
    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["rate"] = 0.3, ["copies"] = 2,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["rate"] = "Fraction 0-1 of methods that receive prepended dead code.",
        ["copies"] = "Dead-block copies prepended to a chosen method. Typical 1-3.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var injector = InjectionExtensions.ContextFor(context.Module);
        var decoy = InjectionExtensions.ResolveMethod(typeof(DecoyBlock).GetMethod(nameof(DecoyBlock.Run))!);
        var entry = context.Module.EntryPoint;
        var copies = Math.Max(0, (int)Option(context, "copies"));
        var count = 0;

        foreach (var type in context.OriginalTypes) {
            if (type.IsGlobalModuleType)
                continue;
            foreach (var method in type.Methods.ToArray()) {
                if (method == entry || !method.HasBody || method.Body.Instructions.Count == 0)
                    continue;
                if (!context.Chance(Option(context, "rate")))
                    continue;
                injector.PrependDead(method, decoy, copies);
                count++;
            }
        }

        context.Log($"    {count} methods junked");
    }
}
