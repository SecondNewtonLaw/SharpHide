using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class DeadCodeProtection : Protection {
    public override long Priority => 90;
    public override string Name => "dead-code";
    public override string Description => "Injects dynamic dead-code helper types";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PreProcess, this);

    public override void Initialize(ObfuscationContext context) => context.Module.Inject(typeof(DeadCode));
}
