using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpHide.Core.Engine;

namespace SharpHide.Core;

public sealed class SharpHideProject {
    public string Input { get; set; } = string.Empty;

    public string? Output { get; set; }

    public int? Seed { get; set; }

    public bool KeepNames { get; set; }

    public string? VirtualizationKey { get; set; }

    public Dictionary<string, ProtectionConfig> Protections { get; set; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static SharpHideProject Load(string path) =>
        JsonSerializer.Deserialize<SharpHideProject>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidDataException($"empty or invalid project: {path}");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));

    public static SharpHideProject CreateDefault(string input) {
        var project = new SharpHideProject { Input = input };
        foreach (var protection in Protection.Discover().OrderByDescending(protection => protection.Priority))
            project.Protections[protection.Name] = new ProtectionConfig {
                Description = protection.Description,
                Options = protection.DefaultOptions.ToDictionary(option => option.Key, option => option.Value),
                Help = protection.OptionHelp.Count > 0
                    ? protection.OptionHelp.ToDictionary(entry => entry.Key, entry => entry.Value)
                    : null,
            };
        return project;
    }

    public IReadOnlySet<string> DisabledProtections() =>
        Protections.Where(entry => !entry.Value.Enabled).Select(entry => entry.Key).ToHashSet();

    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> OptionOverrides() =>
        Protections.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyDictionary<string, double>)entry.Value.Options);

    public string ResolveOutput() => Output ?? Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(Input)) ?? ".", "Obfuscated-" + Path.GetFileName(Input));

    public ObfuscationSettings ToSettings() => new() {
        FileName = ResolveOutput(),
        KeepNames = KeepNames,
        VirtualizationKey = VirtualizationKey,
    };
}
