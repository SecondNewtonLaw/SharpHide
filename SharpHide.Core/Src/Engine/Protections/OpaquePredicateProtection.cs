using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

public sealed class OpaquePredicateProtection : Protection {
    public override long Priority => 63;
    public override string Name => "opaque-predicate";
    public override string Description => "Sprinkles always-false predicates guarding hostile dead code";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["density"] = 15, ["min-steps"] = 20, ["max-steps"] = 48,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["density"] = "One opaque predicate per this many IL instructions (smaller = more junk). Typical 10-30.",
        ["min-steps"] = "Fewest junk operations per dead block. Typical 16-24.",
        ["max-steps"] = "Most junk operations per dead block (exclusive upper bound). Typical 40-64.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    private enum J { I4, I8, R8, Str, Ref, Null }

    private enum Arg { I4, I8, R4, R8, Null }

    private sealed record Callee(IMethod Method, Arg[] Args, J? Return);

    private sealed record Store(IField Field, Arg Value);

    private sealed record Tokens(ITypeDefOrRef Object, ITypeDefOrRef String, ITypeDefOrRef Int32,
        ITypeDefOrRef Int64, ITypeDefOrRef Double);

    private sealed record Junk(IField Guard, IMethod ExceptionCtor, List<Callee> Callees, List<Store> Stores,
        Tokens Tokens);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;

        var guard = new FieldDefUser("Guard", new FieldSig(module.CorLibTypes.Int32),
            FieldAttributes.Public | FieldAttributes.Static);
        var guardContainer = context.NewContainer();
        guardContainer.Fields.Add(guard);
        var exceptionCtor = new Importer(module).Import(typeof(Exception).GetConstructor(Type.EmptyTypes)!);

        BuildSinks(context, module, rng);

        var tokens = new Tokens(module.CorLibTypes.Object.ToTypeDefOrRef(),
            module.CorLibTypes.String.ToTypeDefOrRef(), module.CorLibTypes.Int32.ToTypeDefOrRef(),
            module.CorLibTypes.Int64.ToTypeDefOrRef(), module.CorLibTypes.Double.ToTypeDefOrRef());
        var junk = new Junk(guard, exceptionCtor, CollectCallees(module, guardContainer),
            CollectStores(module, guard, guardContainer), tokens);

        var density = Math.Max(1, (int)Option(context, "density"));
        var minSteps = Math.Max(1, (int)Option(context, "min-steps"));
        var maxSteps = Math.Max(minSteps + 1, (int)Option(context, "max-steps"));

        var count = 0;
        foreach (var type in module.GetTypes()) {
            if (type == guardContainer)
                continue;
            foreach (var method in type.Methods.ToArray()) {
                if (!method.HasBody || method.Body.Instructions.Count == 0)
                    continue;

                if (context.IsStartupCritical(method))
                    continue;

                var inserted = Insert(method.Body, junk, rng, density, minSteps, maxSteps);
                if (inserted > 0)
                    PrependGuardZero(method.Body, guard, rng);

                count += inserted;
            }
        }

        context.Log($"    {count} opaque predicates ({junk.Callees.Count} fake calls, {junk.Stores.Count} fake writes)");
    }

    private static void PrependGuardZero(CilBody body, IField guard, Random rng) {
        var k = rng.Next();
        var prologue = new List<Instruction> {
            OpCodes.Ldsfld.ToInstruction(guard),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.Or.ToInstruction(),
            OpCodes.Ldsfld.ToInstruction(guard),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.Xor.ToInstruction(),
            OpCodes.Sub.ToInstruction(),
            OpCodes.Ldsfld.ToInstruction(guard),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.And.ToInstruction(),
            OpCodes.Sub.ToInstruction(),
            OpCodes.Stsfld.ToInstruction(guard),
        };

        for (var p = prologue.Count - 1; p >= 0; p--)
            body.Instructions.Insert(0, prologue[p]);
    }

    private static void BuildSinks(ObfuscationContext context, ModuleDef module, Random rng) {
        var container = context.NewContainer();
        var sink = new FieldDefUser("Sink", new FieldSig(module.CorLibTypes.Int32),
            FieldAttributes.Public | FieldAttributes.Static);
        container.Fields.Add(sink);

        var kindsPool = new[] { Arg.I4, Arg.I8, Arg.R8, Arg.Null };
        var made = new List<MethodDef>();
        var count = rng.Next(5, 10);
        for (var i = 0; i < count; i++) {
            var arity = rng.Next(1, 5);
            var kinds = new Arg[arity];
            var paramTypes = new TypeSig[arity];
            for (var p = 0; p < arity; p++) {
                kinds[p] = kindsPool[rng.Next(kindsPool.Length)];
                paramTypes[p] = kinds[p] switch {
                    Arg.I4 => module.CorLibTypes.Int32,
                    Arg.I8 => module.CorLibTypes.Int64,
                    Arg.R8 => module.CorLibTypes.Double,
                    _ => rng.Next(2) == 0 ? module.CorLibTypes.String : module.CorLibTypes.Object,
                };
            }

            var method = new MethodDefUser($"s{i}",
                MethodSig.CreateStatic(module.CorLibTypes.Void, paramTypes),
                MethodImplAttributes.IL | MethodImplAttributes.Managed,
                MethodAttributes.Public | MethodAttributes.Static) { Body = new CilBody() };
            container.Methods.Add(method);

            var il = method.Body.Instructions;
            il.Add(OpCodes.Ldsfld.ToInstruction(sink));
            for (var p = 0; p < arity; p++) {
                il.Add(OpCodes.Ldarg.ToInstruction(method.Parameters[p]));
                switch (kinds[p]) {
                    case Arg.I8:
                    case Arg.R8:
                        il.Add(OpCodes.Conv_I4.ToInstruction());
                        break;
                    case Arg.Null:
                        il.Add(OpCodes.Ldnull.ToInstruction());
                        il.Add(OpCodes.Cgt_Un.ToInstruction());
                        break;
                }

                il.Add((rng.Next(2) == 0 ? OpCodes.Xor : OpCodes.Add).ToInstruction());
            }

            if (i > 0 && rng.Next(2) == 0) {
                var target = made[rng.Next(made.Count)];
                foreach (var parameter in target.Parameters)
                    il.Add(TryArg(parameter.Type, out var arg) ? PushArg(arg, rng) : OpCodes.Ldnull.ToInstruction());

                il.Add(OpCodes.Call.ToInstruction(target));
            }

            il.Add(OpCodes.Stsfld.ToInstruction(sink));
            il.Add(OpCodes.Ret.ToInstruction());
            made.Add(method);
        }
    }

    private static List<Store> CollectStores(ModuleDef module, IField guard, TypeDef guardContainer) {
        var stores = new List<Store> { new(guard, Arg.I4) };
        foreach (var type in module.GetTypes()) {
            if (type == guardContainer || type.HasGenericParameters || !AccessibleWithinAssembly(type))
                continue;
            foreach (var field in type.Fields) {
                if (!field.IsStatic || field.IsInitOnly || field.IsLiteral)
                    continue;
                if (!(field.IsPublic || field.IsAssembly || field.IsFamilyOrAssembly))
                    continue;
                if (TryArg(field.FieldType, out var value))
                    stores.Add(new Store(field, value));
            }
        }
        return stores;
    }

    private static List<Callee> CollectCallees(ModuleDef module, TypeDef guardContainer) {
        var callees = new List<Callee>();
        foreach (var type in module.GetTypes()) {
            if (type == guardContainer || type.HasGenericParameters)
                continue;
            foreach (var method in type.Methods) {
                if (!method.IsStatic || method.IsConstructor || method.IsStaticConstructor
                    || method.GenericParameters.Count > 0 || method.MethodSig is not { } sig || sig.Generic)
                    continue;
                if (sig.CallingConvention.HasFlag(CallingConvention.VarArg))
                    continue;
                if (!(method.IsPublic || method.IsAssembly || method.IsFamilyOrAssembly))
                    continue;
                if (!AccessibleWithinAssembly(type))
                    continue;
                if (sig.Params.Count > 4)
                    continue;

                var args = new List<Arg>();
                var ok = true;
                foreach (var paramType in sig.Params) {
                    if (TryArg(paramType, out var arg))
                        args.Add(arg);
                    else { ok = false; break; }
                }
                if (!ok || !TryReturn(sig.RetType, out var ret))
                    continue;
                callees.Add(new Callee(method, args.ToArray(), ret));
            }
        }
        return callees;
    }

    private static bool AccessibleWithinAssembly(TypeDef? type) {
        for (; type is not null; type = type.DeclaringType) {
            if (!type.IsNested)
                return true;
            if (!(type.IsNestedPublic || type.IsNestedAssembly || type.IsNestedFamilyOrAssembly))
                return false;
        }
        return true;
    }

    private static int Insert(CilBody body, Junk junk, Random rng, int density, int minSteps, int maxSteps) {
        var instructions = body.Instructions;
        var heights = StackHeights(instructions, body.ExceptionHandlers);
        var (boundaries, filterInterior) = HandlerBoundaries(instructions, body.ExceptionHandlers);

        var candidates = new List<int>();
        for (var i = 0; i < instructions.Count; i++) {
            if (heights[i] != 0)
                continue;
            if (boundaries.Contains(instructions[i]) || filterInterior.Contains(i))
                continue;
            if (i > 0 && instructions[i - 1].OpCode.OpCodeType == OpCodeType.Prefix)
                continue;
            candidates.Add(i);
        }
        if (candidates.Count == 0)
            return 0;

        var wanted = Math.Clamp(instructions.Count / density, 1, candidates.Count);
        var chosen = candidates.OrderBy(_ => rng.Next()).Take(wanted)
            .Select(index => (Index: index, Target: instructions[index]))
            .OrderByDescending(pick => pick.Index)
            .ToList();

        foreach (var (index, target) in chosen) {
            var block = Block(junk, rng, target, minSteps, maxSteps);
            for (var p = block.Count - 1; p >= 0; p--)
                instructions.Insert(index, block[p]);
        }

        return chosen.Count;
    }

    private static List<Instruction> Block(Junk junk, Random rng, Instruction skipTarget, int minSteps, int maxSteps) {
        var block = new List<Instruction> {
            OpCodes.Ldsfld.ToInstruction(junk.Guard),
            OpCodes.Brfalse.ToInstruction(skipTarget),
        };

        var stack = new List<J>();
        var steps = rng.Next(minSteps, maxSteps);
        for (var step = 0; step < steps; step++)
            Emit(block, stack, junk, rng);

        block.Add(OpCodes.Newobj.ToInstruction(junk.ExceptionCtor));
        block.Add(OpCodes.Throw.ToInstruction());
        return block;
    }

    private static void Emit(List<Instruction> block, List<J> stack, Junk junk, Random rng) {
        var depth = stack.Count;
        var roll = rng.Next(100);

        if (depth == 0 || roll < 26) {
            PushValue(block, stack, junk, rng);
            return;
        }

        if (roll < 36 && junk.Callees.Count > 0) {
            EmitCall(block, stack, junk.Callees[rng.Next(junk.Callees.Count)], rng);
            return;
        }

        if (roll < 46) {
            EmitStore(block, junk.Stores[rng.Next(junk.Stores.Count)], rng);
            return;
        }

        var top = stack[^1];
        if (depth == 1) {
            EmitUnary(block, stack, top, junk, rng);
            return;
        }

        var second = stack[^2];
        var sameNum = IsNum(top) && top == second;
        var sameInt = IsInt(top) && top == second;

        switch (rng.Next(8)) {
            case 0 or 1 when sameNum:
                block.Add(new[] { OpCodes.Add, OpCodes.Sub, OpCodes.Mul }[rng.Next(3)].ToInstruction());
                Pop2Push(stack, top);
                return;
            case 2 when sameInt:
                block.Add(new[] { OpCodes.And, OpCodes.Or, OpCodes.Xor }[rng.Next(3)].ToInstruction());
                Pop2Push(stack, top);
                return;
            case 3 when IsInt(second) && top == J.I4:
                block.Add(new[] { OpCodes.Shl, OpCodes.Shr, OpCodes.Shr_Un }[rng.Next(3)].ToInstruction());
                Pop2Push(stack, second);
                return;
            case 4 when sameNum:
                block.Add((rng.Next(2) == 0 ? OpCodes.Cgt : OpCodes.Clt).ToInstruction());
                Pop2Push(stack, J.I4);
                return;
            case 5 when sameNum || (IsObjish(top) && IsObjish(second)):
                block.Add(OpCodes.Ceq.ToInstruction());
                Pop2Push(stack, J.I4);
                return;
            case 6:
                block.Add(OpCodes.Pop.ToInstruction());
                stack.RemoveAt(stack.Count - 1);
                return;
            default:
                if (rng.Next(2) == 0) {
                    block.Add(OpCodes.Dup.ToInstruction());
                    stack.Add(top);
                }
                else {
                    EmitUnary(block, stack, top, junk, rng);
                }
                return;
        }
    }

    private static void EmitUnary(List<Instruction> block, List<J> stack, J top, Junk junk, Random rng) {
        if (IsNum(top)) {
            // Keep exact integer width when boxing.
            switch (rng.Next(IsInt(top) ? 5 : 4)) {
                case 0:
                    block.Add(IsInt(top) && rng.Next(2) == 0 ? OpCodes.Not.ToInstruction() : OpCodes.Neg.ToInstruction());
                    return;
                case 1: {
                    var (op, kind) = rng.Next(3) switch {
                        0 => (OpCodes.Conv_I8, J.I8),
                        1 => (OpCodes.Conv_R8, J.R8),
                        _ => (OpCodes.Conv_I4, J.I4),
                    };
                    block.Add(op.ToInstruction());
                    stack[^1] = kind;
                    return;
                }
                case 2:
                    block.Add(OpCodes.Dup.ToInstruction());
                    stack.Add(top);
                    return;
                case 3:
                    block.Add(OpCodes.Pop.ToInstruction());
                    stack.RemoveAt(stack.Count - 1);
                    return;
                default:
                    block.Add(OpCodes.Box.ToInstruction(top == J.I4 ? junk.Tokens.Int32 : junk.Tokens.Int64));
                    stack[^1] = J.Ref;
                    return;
            }
        }

        switch (rng.Next(5)) {
            case 0:
                block.Add(OpCodes.Castclass.ToInstruction(rng.Next(2) == 0 ? junk.Tokens.String : junk.Tokens.Object));
                stack[^1] = J.Ref;
                return;
            case 1:
                block.Add(OpCodes.Isinst.ToInstruction(rng.Next(2) == 0 ? junk.Tokens.String : junk.Tokens.Object));
                stack[^1] = J.Ref;
                return;
            case 2: {
                var (tok, kind) = rng.Next(3) switch {
                    0 => (junk.Tokens.Int32, J.I4),
                    1 => (junk.Tokens.Int64, J.I8),
                    _ => (junk.Tokens.Double, J.R8),
                };
                block.Add(OpCodes.Unbox_Any.ToInstruction(tok));
                stack[^1] = kind;
                return;
            }
            case 3:
                block.Add(OpCodes.Dup.ToInstruction());
                stack.Add(top);
                return;
            default:
                block.Add(OpCodes.Pop.ToInstruction());
                stack.RemoveAt(stack.Count - 1);
                return;
        }
    }

    private static void EmitCall(List<Instruction> block, List<J> stack, Callee callee, Random rng) {
        foreach (var arg in callee.Args)
            block.Add(PushArg(arg, rng));
        block.Add(OpCodes.Call.ToInstruction(callee.Method));
        if (callee.Return is { } kind)
            stack.Add(kind);
    }

    private static void EmitStore(List<Instruction> block, Store store, Random rng) {
        if (store.Value == Arg.I4 && rng.Next(2) == 0) {
            block.Add(OpCodes.Ldsfld.ToInstruction(store.Field));
            block.Add(PushArg(Arg.I4, rng));
            block.Add(new[] { OpCodes.Add, OpCodes.Sub, OpCodes.Xor, OpCodes.Mul }[rng.Next(4)].ToInstruction());
        }
        else {
            block.Add(PushArg(store.Value, rng));
        }
        block.Add(OpCodes.Stsfld.ToInstruction(store.Field));
    }

    private static void PushValue(List<Instruction> block, List<J> stack, Junk junk, Random rng) {
        switch (rng.Next(9)) {
            case 0:
                block.Add(OpCodes.Ldc_I4.ToInstruction(rng.Next()));
                stack.Add(J.I4);
                break;
            case 1:
                block.Add(OpCodes.Ldc_I8.ToInstruction(rng.NextInt64()));
                stack.Add(J.I8);
                break;
            case 2:
                block.Add(OpCodes.Ldc_R8.ToInstruction(rng.NextDouble() * double.MaxValue));
                stack.Add(J.R8);
                break;
            case 3: {
                var store = junk.Stores[rng.Next(junk.Stores.Count)];
                block.Add(OpCodes.Ldsfld.ToInstruction(store.Field));
                stack.Add(store.Value switch {
                    Arg.I4 => J.I4,
                    Arg.I8 => J.I8,
                    Arg.R4 or Arg.R8 => J.R8,
                    _ => J.Ref,
                });
                break;
            }
            case 4 or 5:
                block.Add(OpCodes.Ldstr.ToInstruction(rng.Next().ToString("x")));
                stack.Add(J.Str);
                break;
            case 6:
                block.Add(OpCodes.Ldnull.ToInstruction());
                stack.Add(J.Null);
                break;
            default:
                block.Add(OpCodes.Newobj.ToInstruction(junk.ExceptionCtor));
                stack.Add(J.Ref);
                break;
        }
    }

    private static Instruction PushArg(Arg arg, Random rng) => arg switch {
        Arg.I4 => OpCodes.Ldc_I4.ToInstruction(rng.Next()),
        Arg.I8 => OpCodes.Ldc_I8.ToInstruction(rng.NextInt64()),
        Arg.R4 => OpCodes.Ldc_R4.ToInstruction((float)rng.NextDouble()),
        Arg.R8 => OpCodes.Ldc_R8.ToInstruction(rng.NextDouble()),
        _ => OpCodes.Ldnull.ToInstruction(),
    };

    private static bool IsInt(J kind) => kind is J.I4 or J.I8;
    private static bool IsNum(J kind) => kind is J.I4 or J.I8 or J.R8;
    private static bool IsObjish(J kind) => kind is J.Str or J.Ref or J.Null;

    private static void Pop2Push(List<J> stack, J result) {
        stack.RemoveAt(stack.Count - 1);
        stack[^1] = result;
    }

    private static bool TryArg(TypeSig? sig, out Arg arg) {
        arg = Arg.Null;
        sig = sig?.RemovePinnedAndModifiers();
        if (sig is null || sig.IsByRef || sig.IsPointer || sig.IsSentinel)
            return false;
        switch (sig.ElementType) {
            case ElementType.Boolean or ElementType.Char or ElementType.I1 or ElementType.U1 or ElementType.I2
                or ElementType.U2 or ElementType.I4 or ElementType.U4:
                arg = Arg.I4;
                return true;
            case ElementType.I8 or ElementType.U8:
                arg = Arg.I8;
                return true;
            case ElementType.R4:
                arg = Arg.R4;
                return true;
            case ElementType.R8:
                arg = Arg.R8;
                return true;
            case ElementType.String or ElementType.Object or ElementType.Class or ElementType.SZArray
                or ElementType.Array:
                arg = Arg.Null;
                return true;
            default:
                return false;
        }
    }

    private static bool TryReturn(TypeSig? sig, out J? kind) {
        sig = sig?.RemovePinnedAndModifiers();
        switch (sig?.ElementType) {
            case ElementType.Void:
                kind = null;
                return true;
            case ElementType.I8 or ElementType.U8:
                kind = J.I8;
                return true;
            case ElementType.R4 or ElementType.R8:
                kind = J.R8;
                return true;
            case ElementType.Boolean or ElementType.Char or ElementType.I1 or ElementType.U1 or ElementType.I2
                or ElementType.U2 or ElementType.I4 or ElementType.U4:
                kind = J.I4;
                return true;
            case ElementType.String or ElementType.Object or ElementType.Class or ElementType.SZArray
                or ElementType.Array:
                kind = J.Ref;
                return true;
            default:
                kind = null;
                return false;
        }
    }

    private static int[] StackHeights(IList<Instruction> instructions, IList<ExceptionHandler> handlers) {
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
        foreach (var handler in handlers) {
            Seed(handler.TryStart, 0);
            Seed(handler.FilterStart, 1);
            Seed(handler.HandlerStart,
                handler.HandlerType is ExceptionHandlerType.Catch or ExceptionHandlerType.Filter ? 1 : 0);
        }

        while (work.Count > 0) {
            var i = work.Pop();
            var instruction = instructions[i];
            instruction.CalculateStackUsage(out var pushes, out var pops);
            var after = instruction.OpCode.Code is Code.Leave or Code.Leave_S
                ? 0
                : heights[i] - pops + pushes;
            foreach (var successor in Successors(instruction, i, instructions))
                Seed(successor, after);
        }

        for (var i = 0; i < heights.Length; i++)
            if (heights[i] == int.MinValue)
                heights[i] = -1;
        return heights;
    }

    private static IEnumerable<Instruction> Successors(Instruction instruction, int i, IList<Instruction> instructions) {
        switch (instruction.OpCode.FlowControl) {
            case FlowControl.Branch:
                if (instruction.Operand is Instruction target)
                    yield return target;
                break;
            case FlowControl.Cond_Branch:
                if (instruction.Operand is Instruction conditional)
                    yield return conditional;
                else if (instruction.Operand is IList<Instruction> cases)
                    foreach (var branch in cases)
                        yield return branch;
                if (i + 1 < instructions.Count)
                    yield return instructions[i + 1];
                break;
            case FlowControl.Return:
            case FlowControl.Throw:
                break;
            default:
                if (i + 1 < instructions.Count)
                    yield return instructions[i + 1];
                break;
        }
    }

    private static (HashSet<Instruction> Boundaries, HashSet<int> FilterInterior) HandlerBoundaries(
        IList<Instruction> instructions, IList<ExceptionHandler> handlers) {
        var boundaries = new HashSet<Instruction>();
        var filterInterior = new HashSet<int>();
        var index = new Dictionary<Instruction, int>(instructions.Count);
        for (var i = 0; i < instructions.Count; i++)
            index[instructions[i]] = i;

        foreach (var handler in handlers) {
            foreach (var boundary in new[] {
                         handler.TryStart, handler.TryEnd, handler.FilterStart,
                         handler.HandlerStart, handler.HandlerEnd,
                     })
                if (boundary is not null)
                    boundaries.Add(boundary);

            if (handler.FilterStart is not null
                && index.TryGetValue(handler.FilterStart, out var start)
                && handler.HandlerStart is not null && index.TryGetValue(handler.HandlerStart, out var end))
                for (var i = start; i < end; i++)
                    filterInterior.Add(i);
        }

        return (boundaries, filterInterior);
    }
}
