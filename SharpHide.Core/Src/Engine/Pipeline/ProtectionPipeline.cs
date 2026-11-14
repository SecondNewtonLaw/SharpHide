namespace SharpHide.Core.Engine;

public sealed class ProtectionPipeline {
    public enum PipelineStage {
        PreProcess,
        PostProcess,
    }

    private readonly Dictionary<PipelineStage, List<Protection>> stages = new() {
        [PipelineStage.PreProcess] = new List<Protection>(),
        [PipelineStage.PostProcess] = new List<Protection>(),
    };

    public void Insert(PipelineStage stage, Protection protection) => stages[stage].Add(protection);

    internal void ExecuteStage(PipelineStage stage, ObfuscationContext context) {
        var protections = stages[stage].OrderByDescending(protection => protection.Priority).ToArray();
        for (var index = 0; index < protections.Length; index++) {
            var protection = protections[index];
            context.Log($"executing {protection.Name} into target...");
            context.Progress(new ProtectionProgress(protection.Name, stage, index + 1, protections.Length,
                ProtectionProgressState.Started));
            try {
                protection.Initialize(context);
                context.Progress(new ProtectionProgress(protection.Name, stage, index + 1, protections.Length,
                    ProtectionProgressState.Completed));
            }
            catch (Exception error) {
                context.Progress(new ProtectionProgress(protection.Name, stage, index + 1, protections.Length,
                    ProtectionProgressState.Failed, error.GetType().Name));
                throw;
            }
        }
    }
}
