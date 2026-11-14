using System.IO;
using System.IO.Compression;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class ConstantsPoolProtection : Protection {
    public override long Priority => 82;
    public override string Name => "constants-pool";
    public override string Description => "Pools all literals into an encrypted and compressed binary resource";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["max-size"] = 2000,
        ["rounds"] = 32,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["max-size"] = "skips a method with N CIL instructions to prevent creating a blackhole of a stack frame.",
        ["rounds"] =
            "XTEA-CTR rounds over compressed the data, 0-64. Default is 32.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var maxSize = Math.Max(1, (int)Option(context, "max-size"));
        var roundsOption = Option(context, "rounds");
        if (roundsOption is < 0 or > 64 || Math.Abs(roundsOption - Math.Truncate(roundsOption)) > float.E)
            throw new ArgumentOutOfRangeException("rounds", roundsOption, "Expected integer in range 0..64.");
        var rounds = (int)roundsOption;

        var poolResult = module.Inject(typeof(ConstantPool));
        context.Runtime.Add(poolResult.EntryType!);
        var get = poolResult.ResolveMethod(typeof(ConstantPool).GetMethod(nameof(ConstantPool.Get))!)!;
        var key0Field = poolResult.ResolveField(typeof(ConstantPool).GetField(nameof(ConstantPool.Key0))!)!;
        var key1Field = poolResult.ResolveField(typeof(ConstantPool).GetField(nameof(ConstantPool.Key1))!)!;
        var roundsField = poolResult.ResolveField(typeof(ConstantPool).GetField(nameof(ConstantPool.Rounds))!)!;
        var key0 = rounds == 0 ? 0 : rng.NextInt64();
        var key1 = rounds == 0 ? 0 : rng.NextInt64();
        var initializer = module.GlobalType.FindOrCreateStaticConstructor();

        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(roundsField));
        initializer.Body.Instructions.Insert(0, OpCodes.Ldc_I4.ToInstruction(rounds));
        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(key1Field));
        initializer.Body.Instructions.Insert(0, OpCodes.Ldc_I8.ToInstruction(key1));
        initializer.Body.Instructions.Insert(0, OpCodes.Stsfld.ToInstruction(key0Field));
        initializer.Body.Instructions.Insert(0, OpCodes.Ldc_I8.ToInstruction(key0));

        var int32 = module.CorLibTypes.Int32.ToTypeDefOrRef();
        var int64 = module.CorLibTypes.Int64.ToTypeDefOrRef();
        var stringType = module.CorLibTypes.String.ToTypeDefOrRef();

        var values = new List<object>();
        var index = new Dictionary<object, int>();

        var count = 0;
        foreach (var method in context.EncryptableMethods()) {
            if (context.IsStartupCritical(method))
                continue;

            var instructions = method.Body.Instructions;
            if (instructions.Count > maxSize)
                continue;
            method.Body.SimplifyBranches();

            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];

                object? value;
                Instruction conversion;
                if (instruction.IsLdcI4()) {
                    var literal = instruction.GetLdcI4Value();
                    if (literal is >= -1 and <= 8)
                        continue;

                    value = literal;
                    conversion = OpCodes.Unbox_Any.ToInstruction(int32);
                }
                else if (instruction.OpCode == OpCodes.Ldc_I8 && instruction.Operand is long literal8) {
                    value = literal8;
                    conversion = OpCodes.Unbox_Any.ToInstruction(int64);
                }
                else if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string literalStr
                                                             && literalStr.Length > 0) {
                    value = literalStr;
                    conversion = OpCodes.Castclass.ToInstruction(stringType);
                }
                else {
                    continue;
                }

                instruction.OpCode = OpCodes.Ldc_I4;

                if (index.TryGetValue(value, out var existing)) // intern value.
                    instruction.Operand = existing;
                else {
                    var assigned = values.Count;
                    values.Add(value);
                    index[value] = assigned;
                    instruction.Operand = assigned;
                }

                instructions.Insert(i + 1, OpCodes.Call.ToInstruction(get));
                instructions.Insert(i + 2, conversion);
                i += 2;
                count++;
            }

            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();
        }

        if (values.Count > 0) {
            string resourceName;
            do {
                resourceName = context.NewExoticName().String;
            } while (module.Resources.Any(r => r.Name == resourceName));

            var recipe = StringCipherRoller.Roll(rng);
            var container = context.NewContainer();
            context.Runtime.Add(container);
            var decrypt = StringCipherRoller.EmitDecrypt(module, recipe, container);
            get.Body.SimplifyBranches();
            var name = get.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldstr && i.Operand is "");
            name.Operand = StringCipherRoller.Encrypt(resourceName, recipe);
            get.Body.Instructions.Insert(get.Body.Instructions.IndexOf(name) + 1, OpCodes.Call.ToInstruction(decrypt));
            get.Body.OptimizeBranches();
            get.Body.OptimizeMacros();
            using var memory = new MemoryStream();
            using (var gzip = new GZipStream(memory, CompressionLevel.Optimal, true))
                gzip.Write(ConstantPool.Encode(values));
            var data = ConstantPool.Scramble(memory.ToArray());
            context.RuntimeResources.Add(module.InjectResource(resourceName,
                ConstantPool.Encrypt(data, key0, key1, rounds)));
        }

        context.Log($"    {count} literals pooled into {values.Count} entries");
    }
}