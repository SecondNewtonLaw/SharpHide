namespace SharpHide.Core;

public sealed class ProtectionConfig {
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string>? Help { get; set; }
    public Dictionary<string, double> Options { get; set; } = new();
}
