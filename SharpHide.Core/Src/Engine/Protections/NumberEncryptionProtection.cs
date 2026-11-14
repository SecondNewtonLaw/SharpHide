using System.Numerics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

public sealed class NumberEncryptionProtection : Protection {
    public override long Priority => 80;
    public override string Name => "number-encryption";
    public override string Description => "Encodes integer literals with varied branch-free MBA identities";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["handlers"] = 6, ["max-size"] = 2000, ["layers"] = 1, ["complexity"] = 2,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["handlers"] = "Decoder count per integer width. Range 1-32. Six covers XOR, addition, subtraction, complement, odd multiplication and rotation; higher counts vary their keys. Typical 6-12.",
        ["max-size"] = "Skip methods above this IL instruction count. Range 1-100000. Typical 1000-4000.",
        ["layers"] = "Decoder layers per protected constant. Range 1-8. Each layer adds one decoder call.",
        ["complexity"] = "Runtime MBA identity layers per decoder. Range 1-8. Higher values add NOT, AND, OR, XOR, and arithmetic operations.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var rng = context.Random;
        var handlerOption = Option(context, "handlers");
        var maxSizeOption = Option(context, "max-size");
        if (!double.IsFinite(handlerOption) || handlerOption != Math.Truncate(handlerOption) || handlerOption is < 1 or > 32)
            throw new ArgumentOutOfRangeException("handlers", handlerOption, "Expected integer in range 1..32.");
        if (!double.IsFinite(maxSizeOption) || maxSizeOption != Math.Truncate(maxSizeOption) || maxSizeOption is < 1 or > 100_000)
            throw new ArgumentOutOfRangeException("max-size", maxSizeOption, "Expected integer in range 1..100000.");
        var handlerCount = (int)handlerOption;
        var maxSize = (int)maxSizeOption;
        var layersOption = Option(context, "layers");
        if (layersOption is < 1 or > 8 || layersOption != Math.Truncate(layersOption))
            throw new ArgumentOutOfRangeException("layers", layersOption, "Expected integer in range 1..8.");
        var layers = (int)layersOption;
        var complexityOption = context.Options.TryGetValue(Name, out var configured)
            && configured.TryGetValue("complexity", out var configuredComplexity)
            ? configuredComplexity
            : DefaultOptions["complexity"];
        if (complexityOption is < 1 or > 8 || complexityOption != Math.Truncate(complexityOption))
            throw new ArgumentOutOfRangeException("complexity", complexityOption, "Expected integer in range 1..8.");
        var complexity = (int)complexityOption;
        var i4 = new (Func<long, long> Encode, IMethod Decode)[handlerCount];
        var i8 = new (Func<long, long> Encode, IMethod Decode)[handlerCount];
        var first = rng.Next(6);
        for (var i = 0; i < handlerCount; i++) {
            i4[i] = Emit(context, false, (first + i) % 6, complexity);
            i8[i] = Emit(context, true, (first + i) % 6, complexity);
        }

        var count = 0;
        foreach (var method in context.EncryptableMethods()) {
            if (context.IsStartupCritical(method) || method.Body.Instructions.Count > maxSize)
                continue;
            var instructions = method.Body.Instructions;
            method.Body.SimplifyBranches();
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.IsLdcI4()) {
                    var value = instruction.GetLdcI4Value();
                    if (value is >= -1 and <= 8)
                        continue;
                    var selected = Enumerable.Range(0, layers).Select(_ => i4[rng.Next(handlerCount)]).ToArray();
                    long encoded = value;
                    for (var layer = layers - 1; layer >= 0; layer--)
                        encoded = selected[layer].Encode(encoded);
                    instruction.OpCode = OpCodes.Ldc_I4;
                    instruction.Operand = unchecked((int)encoded);
                    foreach (var handler in selected)
                        instructions.Insert(++i, OpCodes.Call.ToInstruction(handler.Decode));
                    count++;
                }
                else if (instruction.OpCode == OpCodes.Ldc_I8 && instruction.Operand is long value) {
                    var selected = Enumerable.Range(0, layers).Select(_ => i8[rng.Next(handlerCount)]).ToArray();
                    var encoded = value;
                    for (var layer = layers - 1; layer >= 0; layer--)
                        encoded = selected[layer].Encode(encoded);
                    instruction.Operand = encoded;
                    foreach (var handler in selected)
                        instructions.Insert(++i, OpCodes.Call.ToInstruction(handler.Decode));
                    count++;
                }
            }
            method.Body.OptimizeMacros();
            method.Body.OptimizeBranches();
        }
        context.Log($"    {count} constants, {layers} decoder layers, {complexity} MBA layers across {handlerCount * 2} handlers");
    }

    private static (Func<long, long> Encode, IMethod Decode) Emit(ObfuscationContext context, bool wide, int kind,
        int complexity) {
        var rng = context.Random;
        var key = kind == 5 ? rng.Next(1, wide ? 64 : 32) : wide ? rng.NextInt64(long.MinValue, long.MaxValue) : rng.Next(int.MinValue, int.MaxValue);
        if (kind == 4)
            key |= 1;
        var inverse = key;
        if (kind == 4)
            for (var i = 0; i < (wide ? 6 : 5); i++)
                inverse = unchecked(inverse * (2 - key * inverse));
        Func<long, long> encode = kind switch {
            0 => value => value ^ key,
            1 => value => unchecked(value - key),
            2 => value => unchecked(value + key),
            3 => value => unchecked(~value - key),
            4 => value => unchecked(value * inverse),
            _ => value => wide ? unchecked((long)BitOperations.RotateRight((ulong)value, (int)key))
                : BitOperations.RotateRight(unchecked((uint)value), (int)key),
        };
        var type = wide ? context.Module.CorLibTypes.Int64 : context.Module.CorLibTypes.Int32;
        var method = new MethodDefUser("Decode", MethodSig.CreateStatic(type, type),
            MethodImplAttributes.IL | MethodImplAttributes.Managed, MethodAttributes.Public | MethodAttributes.Static) {
            Body = new CilBody(),
        };
        var il = new List<Instruction>();
        Instruction Load() => OpCodes.Ldarg_0.ToInstruction();
        Instruction Const(long value) => wide ? OpCodes.Ldc_I8.ToInstruction(value) : OpCodes.Ldc_I4.ToInstruction(unchecked((int)value));
        switch (kind) {
            case 0:
                il.AddRange([Load(), Const(key), OpCodes.Or.ToInstruction(),
                    Load(), Const(key), OpCodes.And.ToInstruction(), OpCodes.Sub.ToInstruction()]);
                break;
            case 1 or 3:
                il.AddRange([Load(), Const(key), OpCodes.Or.ToInstruction(),
                    Load(), Const(key), OpCodes.And.ToInstruction(), OpCodes.Add.ToInstruction()]);
                if (kind == 3)
                    il.Add(OpCodes.Not.ToInstruction());
                break;
            case 2:
                il.AddRange([Load(), Const(~key), OpCodes.And.ToInstruction(), Load(),
                    OpCodes.Not.ToInstruction(), Const(key), OpCodes.And.ToInstruction(), OpCodes.Sub.ToInstruction()]);
                break;
            case 4:
                var mask = rng.NextInt64(long.MinValue, long.MaxValue);
                il.AddRange([Load(), Const(mask), OpCodes.And.ToInstruction(), Const(key), OpCodes.Mul.ToInstruction(),
                    Load(), Const(~mask), OpCodes.And.ToInstruction(), Const(key), OpCodes.Mul.ToInstruction(), OpCodes.Add.ToInstruction()]);
                break;
            default:
                il.AddRange([Load(), OpCodes.Ldc_I4.ToInstruction((int)key), OpCodes.Shl.ToInstruction(),
                    Load(), OpCodes.Ldc_I4.ToInstruction((wide ? 64 : 32) - (int)key), OpCodes.Shr_Un.ToInstruction(), OpCodes.Or.ToInstruction()]);
                break;
        }
        var result = new Local(type);
        method.Body.Variables.Add(result);
        method.Body.InitLocals = true;
        il.Add(OpCodes.Stloc.ToInstruction(result));
        for (var layer = 0; layer < complexity; layer++) {
            EmitIdentity(il, result, wide, (kind + layer) % 5, rng);
            il.Add(OpCodes.Stloc.ToInstruction(result));
        }
        il.Add(OpCodes.Ldloc.ToInstruction(result));
        il.Add(OpCodes.Ret.ToInstruction());
        foreach (var instruction in il)
            method.Body.Instructions.Add(instruction);
        method.Body.OptimizeMacros();
        method.Body.OptimizeBranches();
        var container = context.NewContainer();
        context.Runtime.Add(container);
        container.Methods.Add(method);
        return (encode, method);
    }

    private static void EmitIdentity(List<Instruction> il, Local value, bool wide, int kind, Random rng) {
        long Constant() => wide ? rng.NextInt64(long.MinValue, long.MaxValue) : rng.Next(int.MinValue, int.MaxValue);
        void Load() => il.Add(OpCodes.Ldloc.ToInstruction(value));
        void Push(long number) => il.Add((wide ? OpCodes.Ldc_I8.ToInstruction(number)
            : OpCodes.Ldc_I4.ToInstruction(unchecked((int)number))));

        var mask = Constant();
        switch (kind) {
            case 0:
                Load();
                Push(mask);
                il.Add(OpCodes.And.ToInstruction());
                Load();
                Push(mask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                il.Add(OpCodes.Or.ToInstruction());
                break;
            case 1:
                Load();
                Push(mask);
                il.Add(OpCodes.Or.ToInstruction());
                Load();
                Push(mask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.Or.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                break;
            case 2:
                var partitionMask = Constant();
                Load();
                Push(mask);
                il.Add(OpCodes.Xor.ToInstruction());
                Push(partitionMask);
                il.Add(OpCodes.And.ToInstruction());
                Load();
                Push(mask);
                il.Add(OpCodes.Xor.ToInstruction());
                Push(partitionMask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                il.Add(OpCodes.Or.ToInstruction());
                Push(mask);
                il.Add(OpCodes.Xor.ToInstruction());
                break;
            case 3:
                Load();
                il.Add(OpCodes.Not.ToInstruction());
                Push(mask);
                il.Add(OpCodes.And.ToInstruction());
                Load();
                il.Add(OpCodes.Not.ToInstruction());
                Push(mask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                il.Add(OpCodes.Or.ToInstruction());
                il.Add(OpCodes.Not.ToInstruction());
                break;
            default:
                Load();
                Push(mask);
                Push(mask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                il.Add(OpCodes.Add.ToInstruction());
                Push(mask);
                Push(mask);
                il.Add(OpCodes.Not.ToInstruction());
                il.Add(OpCodes.And.ToInstruction());
                il.Add(OpCodes.Sub.ToInstruction());
                break;
        }
    }
}
