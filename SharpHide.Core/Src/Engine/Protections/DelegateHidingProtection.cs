using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using SharpHide.Core.Injection;
using SharpHide.Core.Payloads;

namespace SharpHide.Core.Engine.Protections;

public sealed class DelegateHidingProtection : Protection {
    public override long Priority => 84;
    public override string Name => "delegate-hiding";
    public override string Description => "rewrites delegates into a reflective resolution with CreateDelegate";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["rate"] = 0.5,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["rate"] = "percentage of delegates which are converted into reflective CreateDelegate.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var module = context.Module;
        var rng = context.Random;
        var create = module.Inject(typeof(CallHider))
            .ResolveMethod(typeof(CallHider).GetMethod(nameof(CallHider.CreateDelegate))!)!;
        var getTypeFromHandle = new Importer(module).Import(
            typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!);
        var count = 0;

        foreach (var method in context.EncryptableMethods()) {
            var instructions = method.Body.Instructions;
            for (var i = 1; i < instructions.Count; i++) {
                if (instructions[i].OpCode != OpCodes.Newobj
                    || instructions[i].Operand is not IMethod ctor || !IsDelegateCtor(ctor))
                    continue;
                var ldftn = instructions[i - 1];
                if (ldftn.OpCode != OpCodes.Ldftn || ldftn.Operand is not IMethod target || !CanHide(target))
                    continue;
                var force = target.DeclaringType is { } declaringType &&
                            context.Settings.AlwaysHideTypes.Contains(declaringType.FullName);
                if (!force && !context.Chance(Option(context, "rate")))
                    continue;

                var delegateType = ctor.DeclaringType;
                var seed = rng.Next(int.MinValue, int.MaxValue);
                var encoded = CallHider.Encode((int)target.MDToken.Raw, seed);

                // encoded token and delegate type.
                ldftn.OpCode = OpCodes.Ldc_I4;
                ldftn.Operand = encoded;
                instructions.Insert(i, OpCodes.Ldc_I4.ToInstruction(seed));
                instructions.Insert(i + 1, OpCodes.Ldtoken.ToInstruction(delegateType));
                instructions.Insert(i + 2, OpCodes.Call.ToInstruction(getTypeFromHandle));

                // newobj => CALL CreateDelegate
                var newobj = instructions[i + 3];
                newobj.OpCode = OpCodes.Call;
                newobj.Operand = create;
                instructions.Insert(i + 4, OpCodes.Castclass.ToInstruction(delegateType));

                i += 4;
                count++;
            }
        }

        context.Log($"    {count} delegates hidden");
    }

    private static bool IsDelegateCtor(IMethod ctor) {
        if (ctor.Name != ".ctor")
            return false;
        var sig = ctor.MethodSig;
        return sig is { Params.Count: 2 }
               && sig.Params[0].ElementType == ElementType.Object
               && sig.Params[1].ElementType == ElementType.I;
    }

    private static bool CanHide(IMethod method) {
        if (method is MethodSpec)
            return false;
        // no token = no protection, we inject junk ourselves, if the Rid = 0 then it is ours, and won't obtain a Rid until dnlib writes it
        // (which SHOULD be okay since we are in a post pass). 
        if (method.MDToken.Table != Table.Method || method.MDToken.Rid == 0)
            return false;
        var sig = method.MethodSig;
        return sig is not null && !sig.Generic;
    }
}