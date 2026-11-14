namespace SharpHide.Core.Engine;

public enum ProtectionProgressState {
    Started,
    Completed,
    Failed,
}

public readonly record struct ProtectionProgress(
    string Name,
    ProtectionPipeline.PipelineStage Stage,
    int Position,
    int Total,
    ProtectionProgressState State,
    string? Detail = null);
