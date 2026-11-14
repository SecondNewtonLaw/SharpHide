using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class CallHidingProtection : Protection {
    public override long Priority => 85;
    public override string Name => "call-hiding";
    public override string Description => "Routes static and instance calls through a reflective token dispatcher";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["rate"] = 0.5,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["rate"] =
            "The percentage of total calls in the binary which are hidden using reflective dispatch, reflection is expensive.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var callHider = context.Module.Inject(typeof(CallHider));
        context.Runtime.Add(callHider.EntryType!);
        var invoke = callHider.ResolveMethod(typeof(CallHider).GetMethod(nameof(CallHider.Invoke))!)!;

        var cookieContainer = context.NewContainer();
        context.Runtime.Add(cookieContainer);
        var cookie = new FieldDefUser("Cookie", new FieldSig(module.CorLibTypes.Int32),
            FieldAttributes.Public | FieldAttributes.Static);
        cookieContainer.Fields.Add(cookie);

        var objectType = module.CorLibTypes.Object.ToTypeDefOrRef();
        var count = 0;

        foreach (var method in context.EncryptableMethods()) {
            var instructions = method.Body.Instructions;
            var hidden = false;
            for (var i = 0; i < instructions.Count; i++) {
                var instruction = instructions[i];
                var isStaticCall = instruction.OpCode == OpCodes.Call;
                var isInstanceCall = instruction.OpCode == OpCodes.Callvirt;
                if (!isStaticCall && !isInstanceCall)
                    continue;
                if (instruction.Operand is not IMethod target || target.MethodSig is not { } sig)
                    continue;

                if (sig.HasThis != isInstanceCall)
                    continue;
                if (target is MethodDef self && self == method)
                    continue;
                if (i > 0 && instructions[i - 1].OpCode.Code is Code.Constrained or Code.Tailcall)
                    continue;
                if (!CanHide(target))
                    continue;
                var force = target.DeclaringType is { } declaringType &&
                            context.Settings.AlwaysHideTypes.Contains(declaringType.FullName);
                if (!force && !context.Chance(Option(context, "rate")))
                    continue;

                var seed = rng.Next(int.MinValue, int.MaxValue);
                var encoded = CallHider.Encode((int)target.MDToken.Raw, seed);
                var sequence = Marshal(module, method.Body, objectType, sig, encoded, seed, cookie, invoke,
                    isInstanceCall);

                instruction.OpCode = sequence[0].OpCode;
                instruction.Operand = sequence[0].Operand;
                for (var s = 1; s < sequence.Count; s++)
                    instructions.Insert(i + s, sequence[s]);
                i += sequence.Count - 1;
                count++;
                hidden = true;
            }

            if (hidden)
                PrependCookieZero(method.Body, cookie, rng);
        }

        context.Log($"    {count} calls hidden");
    }

    private static void PrependCookieZero(CilBody body, IField cookie, Random rng) {
        var k = rng.Next();
        var prologue = new List<Instruction> {
            OpCodes.Ldsfld.ToInstruction(cookie),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.Or.ToInstruction(),
            OpCodes.Ldsfld.ToInstruction(cookie),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.Xor.ToInstruction(),
            OpCodes.Sub.ToInstruction(), // (c | k) - (c ^ k) == c & k
            OpCodes.Ldsfld.ToInstruction(cookie),
            OpCodes.Ldc_I4.ToInstruction(k),
            OpCodes.And.ToInstruction(),
            OpCodes.Sub.ToInstruction(), // (c & k) - (c & k) == 0
            OpCodes.Stsfld.ToInstruction(cookie),
        };

        for (var p = prologue.Count - 1; p >= 0; p--)
            body.Instructions.Insert(0, prologue[p]);
    }

    private static List<Instruction> Marshal(ModuleDef module, CilBody body, ITypeDefOrRef objectType,
        MethodSig sig, int encoded, int seed, IField cookie, IMethod invoke, bool isInstance) {
        var sequence = new List<Instruction>();

        var locals = new Local[sig.Params.Count];
        for (var p = sig.Params.Count - 1; p >= 0; p--) {
            locals[p] = new Local(sig.Params[p]);
            body.Variables.Add(locals[p]);
            sequence.Add(OpCodes.Stloc.ToInstruction(locals[p]));
        }

        Local? receiver = null;
        if (isInstance) {
            receiver = new Local(module.CorLibTypes.Object);
            body.Variables.Add(receiver);
            sequence.Add(OpCodes.Stloc.ToInstruction(receiver));
        }

        sequence.Add(OpCodes.Ldc_I4.ToInstruction(encoded));
        sequence.Add(OpCodes.Ldc_I4.ToInstruction(seed));
        sequence.Add(OpCodes.Ldsfld.ToInstruction(cookie));
        sequence.Add(OpCodes.Xor.ToInstruction());
        sequence.Add(isInstance ? OpCodes.Ldloc.ToInstruction(receiver) : OpCodes.Ldnull.ToInstruction());
        sequence.Add(OpCodes.Ldc_I4.ToInstruction(sig.Params.Count));
        sequence.Add(OpCodes.Newarr.ToInstruction(objectType));
        for (var p = 0; p < sig.Params.Count; p++) {
            sequence.Add(OpCodes.Dup.ToInstruction());
            sequence.Add(OpCodes.Ldc_I4.ToInstruction(p));
            sequence.Add(OpCodes.Ldloc.ToInstruction(locals[p]));
            if (sig.Params[p].IsValueType)
                sequence.Add(OpCodes.Box.ToInstruction(sig.Params[p].ToTypeDefOrRef()));
            sequence.Add(OpCodes.Stelem_Ref.ToInstruction());
        }

        sequence.Add(OpCodes.Call.ToInstruction(invoke));

        var returnType = sig.RetType.RemovePinnedAndModifiers();

        sequence.Add(returnType.ElementType == ElementType.Void
            ? OpCodes.Pop.ToInstruction()
            : OpCodes.Unbox_Any.ToInstruction(returnType.ToTypeDefOrRef()));

        return sequence;
    }

    private static bool CanHide(IMethod method) {
        if (method is MethodSpec)
            return false;
        if (method.Name == ".ctor" || method.Name == ".cctor")
            return false;
        if (method.MDToken.Table != Table.Method || method.MDToken.Rid == 0)
            return false;

        var sig = method.MethodSig;
        if (sig is null || sig.Generic || sig.ParamsAfterSentinel is not null)
            return false;
        if (IsUnsupported(sig.RetType))
            return false;
        if (sig.Params.Any(IsUnsupported)) {
            return false;
        }

        return !sig.HasThis || method.DeclaringType?.ScopeType?.ResolveTypeDef() is not { IsValueType: true };
    }

    internal static bool IsUnsupported(TypeSig type) {
        var bare = type.RemovePinnedAndModifiers();
        if (bare is ByRefSig or PtrSig or SentinelSig
            || bare.ElementType == ElementType.TypedByRef
            || type.ContainsGenericParameter)
            return true;
        var declaring = bare.ToTypeDefOrRef()?.ScopeType?.ResolveTypeDef();
        return declaring is not null && IsByRefLike(declaring);
    }

    private static bool IsByRefLike(TypeDef type) =>
        type.CustomAttributes.Any(attribute =>
            attribute.TypeFullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");
}