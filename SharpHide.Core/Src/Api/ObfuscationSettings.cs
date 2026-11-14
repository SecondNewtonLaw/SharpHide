namespace SharpHide.Core;

public sealed class ObfuscationSettings {
    public string FileName { get; set; } = string.Empty;

    public bool KeepNames { get; set; }

    public string? VirtualizationKey { get; set; }

    public HashSet<string> AlwaysHideTypes { get; } = new();
}
