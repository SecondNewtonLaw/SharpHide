using System.IO;
using System.Text.Json;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using SharpHide.Core.Engine;

namespace SharpHide.Core;

public sealed class Obfuscator {
    public ObfuscationSettings Settings { get; init; } = new();

    public IReadOnlySet<string> DisabledProtections { get; init; } = new HashSet<string>();

    // Name -> option values. Missing values use protection defaults.
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Options { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, double>>();

    public void Obfuscate(ObfuscationContext context) {
        context.Settings = Settings;
        context.Pipeline = new ProtectionPipeline();

        var discovered = Protection.Discover().ToList();
        var options = discovered.ToDictionary(
            protection => protection.Name,
            protection => MergeOptions(protection.DefaultOptions, Options.GetValueOrDefault(protection.Name)));
        context.Options = options;

        var protections = discovered.Where(protection => !DisabledProtections.Contains(protection.Name));
        foreach (var protection in protections)
            protection.Populate(context.Pipeline);

        context.Pipeline.ExecuteStage(ProtectionPipeline.PipelineStage.PreProcess, context);

        // Reload after injection so token-based passes see stable metadata RIDs.
        var originalTypeNames = context.OriginalTypes.Select(type => type.FullName).ToHashSet();
        var reloaded = Reload(context.Module);
        var second = new ObfuscationContext(reloaded, context.Random, originalTypeNames) {
            Settings = Settings,
            Log = context.Log,
            Progress = context.Progress,
            Pipeline = context.Pipeline,
            Options = options,
        };

        second.Pipeline.ExecuteStage(ProtectionPipeline.PipelineStage.PostProcess, second);

        WriteModule(second.Module, Settings.FileName);

        if (second.RequiresIntegrityTrailer) {
            var written = File.ReadAllBytes(Settings.FileName);
            using var stream = new FileStream(Settings.FileName, FileMode.Append, FileAccess.Write);
            stream.Write(Payloads.Integrity.Trailer(written, second.IntegrityKey!));
        }

        if (second.RenameMap.Count > 0)
            File.WriteAllText(Settings.FileName + ".map.json",
                JsonSerializer.Serialize(second.RenameMap, new JsonSerializerOptions { WriteIndented = true }));
    }

    // Merge declared defaults with matching project overrides.
    private static IReadOnlyDictionary<string, double> MergeOptions(
        IReadOnlyDictionary<string, double> defaults, IReadOnlyDictionary<string, double>? overrides) {
        if (overrides is null || overrides.Count == 0)
            return defaults;
        var merged = new Dictionary<string, double>(defaults);
        foreach (var (key, value) in overrides)
            if (defaults.ContainsKey(key))
                merged[key] = value;
        return merged;
    }

    // dnlib assigns stable RIDs to injected members only after writing and reloading.
    private static ModuleDefMD Reload(ModuleDef module) {
        using var stream = new MemoryStream();
        module.Write(stream, new ModuleWriterOptions(module) {
            Logger = DummyLogger.NoThrowInstance,
            MetadataOptions = { Flags = MetadataFlags.PreserveAll },
        });
        return ModuleDefMD.Load(stream.ToArray());
    }

    private static void WriteModule(ModuleDef module, string fileName) {
        // Normalize branches and MaxStack after every IL mutation.
        foreach (var type in module.GetTypes())
            foreach (var method in type.Methods) {
                if (!method.HasBody)
                    continue;
                method.Body.SimplifyBranches();
                if (MaxStackCalculator.GetMaxStack(method.Body.Instructions, method.Body.ExceptionHandlers,
                        out var maxStack)) {
                    method.Body.MaxStack = (ushort)maxStack;
                }
                else {
                    // Throw-terminated opaque junk has no computable stack merge.
                    method.Body.KeepOldMaxStack = true;
                    method.Body.MaxStack = ushort.MaxValue;
                }
            }

        // Preserve RIDs for token references while rebuilding string heaps.
        module.Write(fileName, new ModuleWriterOptions(module) {
            Logger = DummyLogger.NoThrowInstance,
            MetadataOptions = {
                Flags = MetadataFlags.PreserveAll
                        & ~MetadataFlags.PreserveUSOffsets
                        & ~MetadataFlags.PreserveStringsOffsets,
            },
        });
    }
}
