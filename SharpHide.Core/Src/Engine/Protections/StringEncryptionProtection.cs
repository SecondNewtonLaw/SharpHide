using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class StringEncryptionProtection : Protection {
    public override long Priority => 70;
    public override string Name => "string-encryption";
    public override string Description => "Encrypts string literals behind rolled, spliced decrypt handlers";

    // Handler count and junk probability.
    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["handlers"] = 8, ["junk-chance"] = 0.25,
        ["virtualize-chance"] = 0, ["virtualize-max-size"] = 4000,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["handlers"] = "Distinct rolled decrypt handlers strings scatter across. Range 1-32; typical 6-12.",
        ["junk-chance"] = "Probability 0-1 a handler's recipe includes dead dynamic junk (inflates it, pulls "
                          + "in Microsoft.CSharp). Typical 0.2-0.4.",
        ["virtualize-chance"] = "Probability 0-1 each eligible string handler runs in the VM. Default 0 (off); "
                                + "try 0.1. Injects the VM even if the marked-method virtualization pass is disabled. "
                                + "Adds cost on every call to a selected handler; one additional non-VM handler protects interpreter strings.",
        ["virtualize-max-size"] = "Maximum IL instruction count for a string handler selected for virtualization. "
                                  + "Default 4000; range 0-100000. Larger or unsupported handlers keep their original IL.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var options = context.Options.GetValueOrDefault(Name);
        var handlerOption = options?.GetValueOrDefault("handlers") ?? DefaultOptions["handlers"];
        var junkChance = options?.GetValueOrDefault("junk-chance") ?? DefaultOptions["junk-chance"];
        var virtualizeChance = options?.GetValueOrDefault("virtualize-chance") ?? DefaultOptions["virtualize-chance"];
        var maximumSize = options?.GetValueOrDefault("virtualize-max-size") ?? DefaultOptions["virtualize-max-size"];
        ValidateInteger("handlers", handlerOption, 1, 32);
        ValidateProbability("junk-chance", junkChance);
        ValidateProbability("virtualize-chance", virtualizeChance);
        ValidateInteger("virtualize-max-size", maximumSize, 0, 100_000);

        var handlerCount = (int)handlerOption;
        var handlers = new (StringCipherRoller.Recipe Recipe, IMethod Decrypt)[handlerCount];
        for (var i = 0; i < handlerCount; i++) {
            var recipe = StringCipherRoller.Roll(rng, includeJunk: rng.NextDouble() < junkChance);
            var container = context.NewContainer();
            context.Runtime.Add(container);
            handlers[i] = (recipe, StringCipherRoller.EmitDecrypt(module, recipe, container));
        }

        if (virtualizeChance > 0) {
            var selected = handlers.Select(handler => (MethodDef)handler.Decrypt)
                .Where(method => method.Body.Instructions.Count <= maximumSize && context.Chance(virtualizeChance))
                .ToArray();
            var virtualized = VirtualizationProtection.Virtualize(context, selected);
            context.Log($"    {virtualized}/{handlerCount} string handlers virtualized");
        }

        (StringCipherRoller.Recipe Recipe, IMethod Decrypt)? runtimeHandler = null;
        if (context.VirtualMachineTypes.Count != 0) {
            var recipe = StringCipherRoller.Roll(rng);
            var container = context.NewContainer();
            context.Runtime.Add(container);
            runtimeHandler = (recipe, StringCipherRoller.EmitDecrypt(module, recipe, container));
        }

        var count = 0;
        var methods = context.EncryptableMethods().Concat(context.VirtualMachineTypes
            .SelectMany(type => type.Methods).Where(method => method.HasBody));
        foreach (var method in methods) {
            method.Body.SimplifyBranches();
            var instructions = method.Body.Instructions;
            for (var i = 0; i < instructions.Count; i++) {
                if (instructions[i].OpCode != OpCodes.Ldstr || instructions[i].Operand is not string text
                    || text.Length == 0)
                    continue;

                // Keep interpreter strings out of the VM.
                var handler = context.VirtualMachineTypes.Contains(method.DeclaringType)
                    ? runtimeHandler!.Value
                    : handlers[rng.Next(handlers.Length)];
                instructions[i].Operand = StringCipherRoller.Encrypt(text, handler.Recipe);
                instructions.Insert(i + 1, OpCodes.Call.ToInstruction(handler.Decrypt));
                i++;
                count++;
            }
            method.Body.OptimizeMacros();
            method.Body.OptimizeBranches();
        }
        context.Log($"    {count} strings across {handlerCount + (runtimeHandler.HasValue ? 1 : 0)} handlers");
    }

    private static void ValidateInteger(string name, double value, int minimum, int maximum) {
        if (!double.IsFinite(value) || value != Math.Truncate(value) || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(name, value, $"Expected integer in range {minimum}..{maximum}.");
    }

    private static void ValidateProbability(string name, double value) {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(name, value, "Expected probability in range 0..1.");
    }
}
