using System.Runtime.Intrinsics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class ControlFlowProtection : Protection {
    public override long Priority => 20;
    public override string Name => "control-flow";
    public override string Description => "Flattens method control flow behind a state-machine dispatcher";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["min-blocks"] = 3, ["min-size"] = 8, ["max-size"] = 800, ["bogus-states"] = 4, ["opaque-depth"] = 1,
        ["decrypt-hash-chance"] = 0.35, ["handlers"] = 8,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["min-blocks"] = "Fewest basic blocks a method must split into to be flattened.",
        ["min-size"] = "skip methods with less than N CIL instructions.",
        ["max-size"] =
            "skip methods bigger this N CIL instructions. Do not go crazy, or else you will NOT boot up whatever you obfuscate.",
        ["bogus-states"] =
            "dead dispatch states per method, capped at its real block count and/or 32 (whatever is met first).",
        ["opaque-depth"] =
            "MBA layers wrapping every state constant. LINEAR in output size, between 0 and 8; higher = bigger binary = slower binary = longer obfuscation time = you will not need this in 8.",
        ["decrypt-hash-chance"] =
            "Probability that a state id is emitted as an encrypted-string decode + hash instead of an MBA.",
        ["handlers"] =
            "the number of string decoders that will be injected to take care of the hash-based state ids. Max 32, minimum 1.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var defaultMinBlocks = Math.Max(2, (int)Option(context, "min-blocks"));
        var defaultMinSize = Math.Max(1, (int)Option(context, "min-size"));
        var defaultMaxSize = Math.Max(defaultMinSize + 1, (int)Option(context, "max-size"));
        var defaultBogusStates = Math.Clamp((int)Option(context, "bogus-states"), 0, 32);
        var defaultOpaqueDepth = Math.Clamp((int)Option(context, "opaque-depth"), 0, 8);
        var defaultDecryptChance = Math.Clamp(Option(context, "decrypt-hash-chance"), 0.0, 1.0);
        var handlerCount = Math.Clamp((int)Option(context, "handlers"), 1, 32);

        var importer = new Importer(module);
        var getChars = importer.Import(typeof(string).GetMethod("get_Chars", [typeof(int)])!);
        var getLength = importer.Import(typeof(string).GetProperty(nameof(string.Length))!.GetGetMethod()!);
        var getName = importer.Import(typeof(Type).GetProperty(nameof(Type.Name))!.GetGetMethod()!);
        var fromHandle = importer.Import(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
        var vecCreate = importer.Import(
            typeof(Vector128).GetMethod(nameof(Vector128.Create),
                [typeof(int), typeof(int), typeof(int), typeof(int)])!);
        var vecXor = importer.Import(typeof(Vector128<int>).GetMethod("op_ExclusiveOr",
            [typeof(Vector128<int>), typeof(Vector128<int>)])!);
        var vecSum =
            importer.Import(typeof(Vector128).GetMethod(nameof(Vector128.Sum))!.MakeGenericMethod(typeof(int)));
        var compilerGenerated = (ICustomAttributeType)importer.Import(
            typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!);

        var noiseContainer = context.NewContainer();
        noiseContainer.Name = context.NewExoticName();
        noiseContainer.CustomAttributes.Add(new CustomAttribute(compilerGenerated));
        context.Runtime.Add(noiseContainer);
        var noise = new FieldDefUser(context.NewExoticName(), new FieldSig(module.CorLibTypes.Int32),
            FieldAttributes.Public | FieldAttributes.Static);
        noiseContainer.Fields.Add(noise);

        var poke = new MethodDefUser(context.NewExoticName(),
            MethodSig.CreateStatic(module.CorLibTypes.Void, module.CorLibTypes.Int32),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static) { Body = new CilBody() };
        poke.Body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        poke.Body.Instructions.Add(OpCodes.Stsfld.ToInstruction(noise));
        poke.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        noiseContainer.Methods.Add(poke);

        var targets = module.GetTypes()
            .Where(type => !type.IsGlobalModuleType && !context.Runtime.Contains(type))
            .ToList();

        var handlers = new (StringCipherRoller.Recipe Recipe, IMethod Decrypt)[handlerCount];
        for (var i = 0; i < handlerCount; i++) {
            var recipe = StringCipherRoller.Roll(rng);
            var container = context.NewContainer();
            container.Name = context.NewExoticName();
            container.CustomAttributes.Add(new CustomAttribute(compilerGenerated));
            context.Runtime.Add(container);
            var decrypt = StringCipherRoller.EmitDecrypt(module, recipe, container);
            decrypt.Name = context.NewExoticName();
            handlers[i] = (recipe, decrypt);
        }

        var count = 0;

        foreach (var method in targets.SelectMany(type => type.Methods.ToArray())) {
            if (!method.HasBody || method.Body.Instructions.Count == 0)
                continue;

            if (method.Body.ExceptionHandlers.Count > 0)
                continue;

            if (context.IsStartupCritical(method))
                continue;

            if (HasMacro(method, "DoNotControlFlowAttribute"))
                continue;

            var macro = FindMacro(method, "ControlFlowAttribute");
            var minBlocks = IntOption(macro, "MinBlocks", defaultMinBlocks, 2, int.MaxValue);
            var minSize = IntOption(macro, "MinSize", defaultMinSize, 1, int.MaxValue);
            var maxSize = IntOption(macro, "MaxSize", defaultMaxSize, minSize + 1, int.MaxValue);
            var bogusStates = IntOption(macro, "BogusStates", defaultBogusStates, 0, 32);
            var opaqueDepth = IntOption(macro, "OpaqueDepth", defaultOpaqueDepth, 0, 8);
            var decryptChance = DoubleOption(macro, "DecryptHashChance", defaultDecryptChance, 0, 1);

            var body = method.Body;
            var instructions = body.Instructions;

            if (instructions.Count < minSize || instructions.Count > maxSize)
                continue;

            instructions.SimplifyBranches();

            var index = new Dictionary<Instruction, int>(instructions.Count);
            for (var i = 0; i < instructions.Count; i++)
                index[instructions[i]] = i;

            var heights = new int[instructions.Count];
            Array.Fill(heights, int.MinValue);

            var work = new Stack<int>();

            void Seed(Instruction? instruction, int height) {
                if (instruction is null || !index.TryGetValue(instruction, out var i) || heights[i] != int.MinValue)
                    return;

                heights[i] = height;
                work.Push(i);
            }

            Seed(instructions[0], 0);

            while (work.Count > 0) {
                var i = work.Pop();
                var instruction = instructions[i];
                instruction.CalculateStackUsage(out var pushes, out var pops);
                var after = heights[i] - pops + pushes;

                switch (instruction.OpCode.FlowControl) {
                    case FlowControl.Branch:
                        if (instruction.Operand is Instruction target)
                            Seed(target, after);

                        break;

                    case FlowControl.Cond_Branch:
                        switch (instruction.Operand) {
                            case Instruction conditional:
                                Seed(conditional, after);
                                break;
                            case IList<Instruction> cases: {
                                foreach (var branch in cases)
                                    Seed(branch, after);
                                break;
                            }
                        }

                        if (i + 1 < instructions.Count)
                            Seed(instructions[i + 1], after);
                        break;

                    case FlowControl.Return:
                    case FlowControl.Throw:
                        break;

                    default:
                        if (i + 1 < instructions.Count)
                            Seed(instructions[i + 1], after);
                        break;
                }
            }

            for (var i = 0; i < heights.Length; i++)
                if (heights[i] == int.MinValue)
                    heights[i] = -1;

            var leaders = new HashSet<Instruction> { instructions[0] };
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                if (instruction.Operand is Instruction target)
                    leaders.Add(target);
                else if (instruction.Operand is IList<Instruction> jumpTargets)
                    leaders.UnionWith(jumpTargets);
                if (i + 1 < instructions.Count && instruction.OpCode.FlowControl is
                        FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                    leaders.Add(instructions[i + 1]);
            }

            var starts = new List<int>();
            for (var i = 0; i < instructions.Count; i++) {
                if (heights[i] != 0 || !leaders.Contains(instructions[i]))
                    continue;

                if (i > 0 && instructions[i - 1].OpCode.OpCodeType == OpCodeType.Prefix)
                    continue;

                starts.Add(i);
            }

            if (starts.Count < minBlocks)
                continue;

            var blocks = new List<List<Instruction>>(starts.Count);
            for (var k = 0; k < starts.Count; k++) {
                var from = starts[k];
                var to = k + 1 < starts.Count ? starts[k + 1] : instructions.Count;

                var block = new List<Instruction>(to - from);
                for (var i = from; i < to; i++)
                    block.Add(instructions[i]);

                blocks.Add(block);
            }

            var nameSeed = rng.Next();
            var namePrime = rng.Next() | 1;
            var typeKey = HashOf(method.DeclaringType!.Name, 0, nameSeed, namePrime);

            var state = new Local(module.CorLibTypes.Int32);
            var decoded = new Local(module.CorLibTypes.String);
            var typeName = new Local(module.CorLibTypes.String);
            var cursor = new Local(module.CorLibTypes.Int32);
            var key = new Local(module.CorLibTypes.Int32);
            var rotTmp = new Local(module.CorLibTypes.Int32);
            var mbaTmp = new Local(module.CorLibTypes.Int32);
            var simdA = new Local(module.CorLibTypes.Int32);
            var simdC = new Local(module.CorLibTypes.Int32);
            body.Variables.Add(state);
            body.Variables.Add(decoded);
            body.Variables.Add(typeName);
            body.Variables.Add(cursor);
            body.Variables.Add(key);
            body.Variables.Add(rotTmp);
            body.Variables.Add(mbaTmp);
            body.Variables.Add(simdA);
            body.Variables.Add(simdC);

            var dispatchHead = OpCodes.Ldloc.ToInstruction(state);

            var seen = new HashSet<int>();
            var idInfo = new Dictionary<int, (string Text, int Family, int Seed, int Param)>();

            int FreshId() {
                while (true) {
                    var chars = new char[rng.Next(3, 7)];
                    for (var c = 0; c < chars.Length; c++)
                        chars[c] = (char)('a' + rng.Next(26));

                    var text = new string(chars);
                    var family = rng.Next(4);
                    var seed = rng.Next();
                    var param = family == 2 ? rng.Next(1, 31) : rng.Next() | 1;
                    var id = HashOf(text, family, seed, param);
                    if (seen.Add(id)) {
                        idInfo[id] = (text, family, seed, param);
                        return id;
                    }
                }
            }

            List<Instruction> Mask(int value) {
                var r = rng.Next();
                var kind = rng.Next(4);
                var inner = kind switch { 0 => value ^ r, 1 or 2 => unchecked(value - r), _ => unchecked(value + r) };
                return kind switch {
                    < 2 => [
                        OpCodes.Ldc_I4.ToInstruction(inner), OpCodes.Ldc_I4.ToInstruction(r),
                        (kind == 0 ? OpCodes.Xor : OpCodes.Add).ToInstruction()
                    ],
                    2 => [
                        OpCodes.Ldc_I4.ToInstruction(inner), OpCodes.Ldc_I4.ToInstruction(r),
                        OpCodes.Or.ToInstruction(), OpCodes.Ldc_I4.ToInstruction(inner),
                        OpCodes.Ldc_I4.ToInstruction(r), OpCodes.And.ToInstruction(), OpCodes.Add.ToInstruction()
                    ],
                    _ => [
                        OpCodes.Ldc_I4.ToInstruction(inner), OpCodes.Ldc_I4.ToInstruction(~r),
                        OpCodes.And.ToInstruction(), OpCodes.Ldc_I4.ToInstruction(~inner),
                        OpCodes.Ldc_I4.ToInstruction(r), OpCodes.And.ToInstruction(), OpCodes.Sub.ToInstruction()
                    ]
                };
            }

            List<Instruction> Mba(int value, int depth) {
                if (depth <= 0)
                    return Mask(value);

                var r = rng.Next();
                switch (rng.Next(8)) {
                    case 0: {
                        var s = Mba(value ^ r, depth - 1);
                        s.AddRange(Mask(r));
                        s.Add(OpCodes.Xor.ToInstruction());
                        return s;
                    }

                    case 1: {
                        var s = Mba(unchecked(value - r), depth - 1);
                        s.AddRange(Mask(r));
                        s.Add(OpCodes.Add.ToInstruction());
                        return s;
                    }

                    case 2: {
                        var s = Mba(unchecked(value + r), depth - 1);
                        s.AddRange(Mask(r));
                        s.Add(OpCodes.Sub.ToInstruction());
                        return s;
                    }

                    case 3: {
                        var a = rng.Next();
                        var b = rng.Next();
                        var c = rng.Next();
                        var inner = unchecked(value - a - b - c);

                        var s = Mba(inner, depth - 1);
                        s.AddRange(Mask(a));
                        s.AddRange(Mask(b));
                        s.AddRange(Mask(c));
                        s.Add(OpCodes.Call.ToInstruction(vecCreate));
                        s.Add(OpCodes.Call.ToInstruction(vecSum));
                        return s;
                    }
                    case 4:
                    case 5: {
                        var complement = rng.Next(2) == 0;
                        var s = Mba(complement ? ~value : unchecked(-value), depth - 1);
                        s.Add((complement ? OpCodes.Not : OpCodes.Neg).ToInstruction());
                        return s;
                    }
                    case 6: {
                        r |= 1;
                        var inverse = r;
                        for (var i = 0; i < 5; i++)
                            inverse = unchecked(inverse * (2 - r * inverse));
                        var s = Mba(unchecked(value * inverse), depth - 1);
                        s.AddRange(Mask(r));
                        s.Add(OpCodes.Mul.ToInstruction());
                        return s;
                    }
                    default: {
                        var shift = rng.Next(1, 32);
                        var inner = unchecked((int)(((uint)value >> shift) | ((uint)value << (32 - shift))));
                        var s = Mba(inner, depth - 1);
                        s.AddRange([
                            OpCodes.Stloc.ToInstruction(mbaTmp), OpCodes.Ldloc.ToInstruction(mbaTmp),
                            OpCodes.Ldc_I4.ToInstruction(shift), OpCodes.Shl.ToInstruction(),
                            OpCodes.Ldloc.ToInstruction(mbaTmp),
                            OpCodes.Ldc_I4.ToInstruction(32 - shift), OpCodes.Shr_Un.ToInstruction(),
                            OpCodes.Or.ToInstruction()
                        ]);
                        return s;
                    }
                }
            }

            List<Instruction> DecryptHash(int id) {
                var info = idInfo[id];
                var (recipe, decrypt) = handlers[rng.Next(handlers.Length)];
                var cipher = StringCipherRoller.Encrypt(info.Text, recipe);

                var seq = new List<Instruction> {
                    OpCodes.Ldstr.ToInstruction(cipher),
                    OpCodes.Call.ToInstruction(decrypt),
                    OpCodes.Stloc.ToInstruction(decoded),
                };
                seq.AddRange(Mba(info.Seed, 1));

                void PushChar(int idx) {
                    seq.Add(OpCodes.Ldloc.ToInstruction(decoded));
                    seq.AddRange(Mba(idx, 1));
                    seq.Add(OpCodes.Callvirt.ToInstruction(getChars));
                }

                for (var i = 0; i < info.Text.Length; i++) {
                    switch (info.Family) {
                        case 0:
                            PushChar(i);
                            seq.Add(OpCodes.Xor.ToInstruction());
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Mul.ToInstruction());
                            break;

                        case 1:
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Mul.ToInstruction());
                            PushChar(i);
                            seq.Add(OpCodes.Add.ToInstruction());
                            break;

                        case 2:
                            seq.Add(OpCodes.Stloc.ToInstruction(rotTmp));
                            seq.Add(OpCodes.Ldloc.ToInstruction(rotTmp));
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Shl.ToInstruction());
                            seq.Add(OpCodes.Ldloc.ToInstruction(rotTmp));
                            seq.AddRange(Mba(32 - info.Param, 1));
                            seq.Add(OpCodes.Shr_Un.ToInstruction());
                            seq.Add(OpCodes.Or.ToInstruction());
                            PushChar(i);
                            seq.Add(OpCodes.Xor.ToInstruction());
                            break;

                        default:
                            seq.Add(OpCodes.Stloc.ToInstruction(simdA));
                            PushChar(i);
                            seq.Add(OpCodes.Stloc.ToInstruction(simdC));

                            seq.Add(OpCodes.Ldloc.ToInstruction(simdA));
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdA));
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Mul.ToInstruction());
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdA));
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Xor.ToInstruction());
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdA));
                            seq.AddRange(Mba(info.Param, 1));
                            seq.Add(OpCodes.Add.ToInstruction());
                            seq.Add(OpCodes.Call.ToInstruction(vecCreate));

                            seq.Add(OpCodes.Ldloc.ToInstruction(simdC));
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdC));
                            seq.AddRange(Mba(info.Seed, 1));
                            seq.Add(OpCodes.Mul.ToInstruction());
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdC));
                            seq.AddRange(Mba(info.Seed, 1));
                            seq.Add(OpCodes.Xor.ToInstruction());
                            seq.Add(OpCodes.Ldloc.ToInstruction(simdC));
                            seq.AddRange(Mba(info.Seed, 1));
                            seq.Add(OpCodes.Add.ToInstruction());
                            seq.Add(OpCodes.Call.ToInstruction(vecCreate));

                            seq.Add(OpCodes.Call.ToInstruction(vecXor));
                            seq.Add(OpCodes.Call.ToInstruction(vecSum));
                            break;
                    }

                    if (rng.Next(3) != 0) continue;
                    
                    seq.Add(OpCodes.Ldsfld.ToInstruction(noise));
                    seq.Add(OpCodes.Xor.ToInstruction());
                }

                return seq;
            }

            List<Instruction> Const(int value, int depth) {
                if (depth > 0 && rng.NextDouble() < decryptChance && idInfo.ContainsKey(value))
                    return DecryptHash(value);

                return Mba(value, depth);
            }

            List<Instruction> Transition(int targetId) {
                var seq = Const(targetId, opaqueDepth);
                seq.Add(OpCodes.Ldsfld.ToInstruction(noise));
                seq.Add(OpCodes.Xor.ToInstruction());
                seq.Add(OpCodes.Stloc.ToInstruction(state));
                seq.Add(OpCodes.Br.ToInstruction(dispatchHead));
                return seq;
            }

            var ids = new int[blocks.Count];
            for (var k = 0; k < blocks.Count; k++)
                ids[k] = FreshId();

            var leaderId = new Dictionary<Instruction, int>(blocks.Count);
            for (var k = 0; k < blocks.Count; k++)
                leaderId[blocks[k][0]] = ids[k];

            var extraBlocks = new List<List<Instruction>>();
            var extraIds = new List<int>();
            var stubs = new Dictionary<int, List<Instruction>>();

            Instruction StubFor(int targetId) {
                if (stubs.TryGetValue(targetId, out var existing))
                    return existing[0];
                var stub = Transition(targetId);
                stubs.Add(targetId, stub);
                return stub[0];
            }

            var bail = false;
            for (var k = 0; k < blocks.Count; k++) {
                var block = blocks[k];
                var last = block[^1];
                var flow = last.OpCode.FlowControl;

                switch (flow) {
                    case FlowControl.Return or FlowControl.Throw:
                        continue;
                    case FlowControl.Branch: {
                        if (last.Operand is Instruction target && leaderId.TryGetValue(target, out var tid)) {
                            var transition = Transition(tid);
                            last.OpCode = transition[0].OpCode;
                            last.Operand = transition[0].Operand;
                            block.AddRange(transition.Skip(1));
                        }

                        continue;
                    }
                }

                if (flow == FlowControl.Cond_Branch) {
                    switch (last.Operand) {
                        case Instruction taken when leaderId.TryGetValue(taken, out var takenId):
                            last.Operand = StubFor(takenId);
                            break;
                        case IList<Instruction> cases: {
                            for (var c = 0; c < cases.Count; c++)
                                if (leaderId.TryGetValue(cases[c], out var caseId))
                                    cases[c] = StubFor(caseId);
                            break;
                        }
                    }

                    if (k + 1 >= blocks.Count) {
                        bail = true;
                        break;
                    }

                    block.AddRange(Transition(ids[k + 1]));
                    continue;
                }

                if (k + 1 >= blocks.Count) {
                    bail = true;
                    break;
                }

                block.AddRange(Transition(ids[k + 1]));
            }

            if (bail)
                continue;

            for (var g = 0; g < Math.Min(bogusStates, blocks.Count); g++) {
                var junk = new List<Instruction> { OpCodes.Nop.ToInstruction() };
                junk.AddRange(Transition(ids[0]));
                extraBlocks.Add(junk);
                extraIds.Add(FreshId());
            }

            var allBlocks = new List<List<Instruction>>(blocks);
            allBlocks.AddRange(extraBlocks);

            var allIds = new List<int>(ids);
            allIds.AddRange(extraIds);

            var loopHead = OpCodes.Ldloc.ToInstruction(cursor);
            var loopEnd = OpCodes.Nop.ToInstruction();
            var preamble = new List<Instruction> {
                OpCodes.Ldtoken.ToInstruction(method.DeclaringType!),
                OpCodes.Call.ToInstruction(fromHandle),
                OpCodes.Callvirt.ToInstruction(getName),
                OpCodes.Stloc.ToInstruction(typeName),
            };
            preamble.AddRange(Mba(nameSeed, 1));
            preamble.Add(OpCodes.Stloc.ToInstruction(key));
            preamble.Add(OpCodes.Ldc_I4_0.ToInstruction());
            preamble.Add(OpCodes.Stloc.ToInstruction(cursor));
            preamble.Add(loopHead);
            preamble.Add(OpCodes.Ldloc.ToInstruction(typeName));
            preamble.Add(OpCodes.Callvirt.ToInstruction(getLength));
            preamble.Add(OpCodes.Bge.ToInstruction(loopEnd));
            preamble.Add(OpCodes.Ldloc.ToInstruction(key));
            preamble.Add(OpCodes.Ldloc.ToInstruction(typeName));
            preamble.Add(OpCodes.Ldloc.ToInstruction(cursor));
            preamble.Add(OpCodes.Callvirt.ToInstruction(getChars));
            preamble.Add(OpCodes.Xor.ToInstruction());
            preamble.AddRange(Mba(namePrime, 1));
            preamble.Add(OpCodes.Mul.ToInstruction());
            preamble.Add(OpCodes.Stloc.ToInstruction(key));
            preamble.Add(OpCodes.Ldloc.ToInstruction(cursor));
            preamble.Add(OpCodes.Ldc_I4_1.ToInstruction());
            preamble.Add(OpCodes.Add.ToInstruction());
            preamble.Add(OpCodes.Stloc.ToInstruction(cursor));
            preamble.Add(OpCodes.Br.ToInstruction(loopHead));
            preamble.Add(loopEnd);

            var noiseKey = rng.Next();
            preamble.Add(OpCodes.Ldsfld.ToInstruction(noise));
            preamble.Add(OpCodes.Ldc_I4.ToInstruction(noiseKey));
            preamble.Add(OpCodes.Or.ToInstruction());
            preamble.Add(OpCodes.Ldsfld.ToInstruction(noise));
            preamble.Add(OpCodes.Ldc_I4.ToInstruction(noiseKey));
            preamble.Add(OpCodes.Xor.ToInstruction());
            preamble.Add(OpCodes.Sub.ToInstruction()); // (n | k) - (n ^ k) == n & k
            preamble.Add(OpCodes.Ldsfld.ToInstruction(noise));
            preamble.Add(OpCodes.Ldc_I4.ToInstruction(noiseKey));
            preamble.Add(OpCodes.And.ToInstruction());
            preamble.Add(OpCodes.Sub.ToInstruction()); // (n & k) - (n & k) == 0
            preamble.Add(OpCodes.Stsfld.ToInstruction(noise));

            var dispatchOrder = Enumerable.Range(0, allBlocks.Count).OrderBy(_ => rng.Next()).ToList();
            var dispatch = new List<Instruction>();
            var first = true;
            foreach (var j in dispatchOrder) {
                dispatch.Add(first ? dispatchHead : OpCodes.Ldloc.ToInstruction(state));
                first = false;
                dispatch.Add(OpCodes.Ldloc.ToInstruction(key));
                dispatch.Add(OpCodes.Xor.ToInstruction());
                dispatch.Add(OpCodes.Ldsfld.ToInstruction(noise));
                dispatch.Add(OpCodes.Xor.ToInstruction());
                dispatch.AddRange(Const(unchecked(allIds[j] ^ typeKey), opaqueDepth));
                dispatch.Add(OpCodes.Beq.ToInstruction(allBlocks[j][0]));
            }

            dispatch.Add(OpCodes.Ldnull.ToInstruction());
            dispatch.Add(OpCodes.Throw.ToInstruction());

            allBlocks.AddRange(stubs.Values);
            var emitOrder = Enumerable.Range(0, allBlocks.Count).OrderBy(_ => rng.Next()).ToList();

            instructions.Clear();

            foreach (var instr in preamble)
                instructions.Add(instr);

            foreach (var instr in Const(ids[0], opaqueDepth))
                instructions.Add(instr);

            instructions.Add(OpCodes.Ldsfld.ToInstruction(noise));
            instructions.Add(OpCodes.Xor.ToInstruction());
            instructions.Add(OpCodes.Stloc.ToInstruction(state));

            foreach (var instr in dispatch)
                instructions.Add(instr);

            foreach (var j in emitOrder)
            foreach (var instr in allBlocks[j])
                instructions.Add(instr);

            body.OptimizeMacros();
            body.OptimizeBranches();

            count++;
        }

        context.Log($"    flattened {count} methods");
        RemoveMacros(module);
    }

    private static bool HasMacro(MethodDef method, string name) => FindMacro(method, name) is not null;

    private static CustomAttribute? FindMacro(MethodDef method, string name) =>
        method.CustomAttributes.LastOrDefault(attribute => attribute.AttributeType.Name == name) ??
        method.DeclaringType.CustomAttributes.LastOrDefault(attribute => attribute.AttributeType.Name == name);

    private static int IntOption(CustomAttribute? attribute, string name, int fallback, int minimum, int maximum) {
        var argument = attribute?.NamedArguments.LastOrDefault(argument => argument.Name == name);
        return argument is null ? fallback : Math.Clamp(Convert.ToInt32(argument.Argument.Value), minimum, maximum);
    }

    private static double DoubleOption(CustomAttribute? attribute, string name, double fallback, double minimum,
        double maximum) {
        var argument = attribute?.NamedArguments.LastOrDefault(argument => argument.Name == name);
        return argument is null ? fallback : Math.Clamp(Convert.ToDouble(argument.Argument.Value), minimum, maximum);
    }

    private static void RemoveMacros(ModuleDef module) {
        foreach (var type in module.GetTypes()) {
            RemoveMacros(type);
            foreach (var method in type.Methods)
                RemoveMacros(method);
        }
    }

    private static void RemoveMacros(IHasCustomAttribute owner) {
        foreach (var attribute in owner.CustomAttributes.Where(attribute =>
                     attribute.AttributeType.Name == "ControlFlowAttribute" ||
                     attribute.AttributeType.Name == "DoNotControlFlowAttribute").ToArray())
            owner.CustomAttributes.Remove(attribute);
    }

    private static int HashOf(string text, int family, int seed, int param) {
        var hash = seed;
        foreach (int ch in text)
            hash = family switch {
                0 => unchecked((hash ^ ch) * param),
                1 => unchecked(hash * param + ch),
                2 => unchecked((int)(((uint)hash << param) | ((uint)hash >> (32 - param))) ^ ch),
                _ => unchecked(Vector128.Sum(
                    Vector128.Create(hash, hash * param, hash ^ param, hash + param) ^
                    Vector128.Create(ch, ch * seed, ch ^ seed, ch + seed))),
            };

        return hash;
    }
}