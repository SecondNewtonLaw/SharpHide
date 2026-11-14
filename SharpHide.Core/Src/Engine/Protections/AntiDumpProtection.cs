using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class AntiDumpProtection : Protection {
    public override long Priority => 98;
    public override string Name => "anti-dump";
    public override string Description => "Clears the in-memory PE header after load to hinder memory dumping";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        context.Module.InjectModuleInitializer(typeof(AntiDump).GetMethod(nameof(AntiDump.Protect))!);
        context.Settings.AlwaysHideTypes.Add(typeof(AntiDump).FullName! /* fuck up AntiDump */);
        context.Log("    anti-dump installed");
    }
}
