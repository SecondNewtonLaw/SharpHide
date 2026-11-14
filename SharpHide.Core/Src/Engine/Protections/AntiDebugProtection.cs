using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class AntiDebugProtection : Protection {
    public override long Priority => 100;
    public override string Name => "anti-debug";
    public override string Description => "Injects a module-initializer anti-debug bootstrap";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PreProcess, this);

    public override void Initialize(ObfuscationContext context) {
        context.Module.InjectModuleInitializer(typeof(AntiDebug).GetMethod(nameof(AntiDebug.Initialize))!);
        context.Settings.AlwaysHideTypes.Add(typeof(AntiDebug).FullName!);
    }
}