using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class AntiTamperProtection : Protection {
    public override long Priority => 95;
    public override string Name => "anti-tamper";
    public override string Description => "Fails fast at startup if the written assembly was modified";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["strict"] = 0,
        ["diagnostics"] = 0,
        ["method-checks"] = 2,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["strict"] =
            "Fail when assembly location cannot be read. Default 0 supports in-memory and single-file loading; set 1 for disk-only deployments.",
        ["diagnostics"] =
            "Throw an opaque integrity exception instead of silent fail-fast for debugging. Default 0; set 1 only during diagnostics.",
        ["method-checks"] =
            "Number of user methods receiving distributed verifier calls. Default 2; typical 2-8. Each call reads and hashes the assembly; all calls share one initialized verifier.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        Span<byte> key = stackalloc byte[32];
        context.Random.NextBytes(key);
        context.IntegrityKey = key.ToArray();
        var initializer = module.GlobalType.FindOrCreateStaticConstructor();

        var strictOption = Option(context, "strict");
        if (strictOption is < 0 or > 1 || Math.Abs(strictOption - Math.Truncate(strictOption)) > float.Epsilon)
            throw new ArgumentOutOfRangeException("strict", strictOption, "Expected binary value 0 or 1.");

        var diagnosticsOption = Option(context, "diagnostics");
        if (diagnosticsOption is < 0 or > 1 ||
            Math.Abs(diagnosticsOption - Math.Truncate(diagnosticsOption)) > float.Epsilon)
            throw new ArgumentOutOfRangeException("diagnostics", diagnosticsOption, "Expected binary value 0 or 1.");

        var checkOption = Option(context, "method-checks");
        if (checkOption is < 0 or > 64 || Math.Abs(checkOption - Math.Truncate(checkOption)) > float.Epsilon)
            throw new ArgumentOutOfRangeException("method-checks", checkOption, "Expected integer in range 0..64.");

        var strictValue = strictOption > 0;
        var diagnosticsValue = diagnosticsOption > 0;
        var injected = module.Inject(typeof(Integrity), true);
        var verify = injected.ResolveMethod(typeof(Integrity).GetMethod(nameof(Integrity.Verify))!)!;
        var authenticationKey =
            injected.ResolveField(typeof(Integrity).GetField(nameof(Integrity.AuthenticationKey))!)!;
        var strict = injected.ResolveField(typeof(Integrity).GetField(nameof(Integrity.Strict))!)!;
        var diagnostics = injected.ResolveField(typeof(Integrity).GetField(nameof(Integrity.Diagnostics))!)!;
        var concat = module.Import(typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string)])!);
        var encodedKey = Convert.ToBase64String(key);
        var split = encodedKey.Length / 2;

        initializer.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(verify));
        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(strict));
        initializer.Body.Instructions.Insert(0, (strictValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0).ToInstruction());
        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(diagnostics));
        initializer.Body.Instructions.Insert(0,
            (diagnosticsValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0).ToInstruction());
        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(authenticationKey));
        initializer.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(concat));
        initializer.Body.Instructions.Insert(0, OpCodes.Ldstr.ToInstruction(encodedKey[split..]));
        initializer.Body.Instructions.Insert(0, OpCodes.Ldstr.ToInstruction(encodedKey[..split]));

        context.Runtime.Add(injected.EntryType!);

        var checkCount = (int)checkOption;
        var candidates = context.OriginalTypes.SelectMany(type => type.Methods)
            .Where(method => method.HasBody && !context.IsStartupCritical(method) && !method.IsConstructor)
            .OrderBy(_ => context.Random.Next()).Take(checkCount).ToArray();
        foreach (var method in candidates) {
            method.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(verify));
            method.Body.OptimizeMacros();
            method.Body.OptimizeBranches();
        }

        context.RequiresIntegrityTrailer = true;
        context.Log($"    keyed integrity check installed, {candidates.Length} distributed checks");
    }
}