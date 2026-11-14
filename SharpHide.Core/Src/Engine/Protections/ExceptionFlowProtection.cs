using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

public sealed class ExceptionFlowProtection : Protection {
    public override long Priority => 15;
    public override string Name => "exception-flow";

    public override string Description =>
        "makes entire functions become a beautiful try { /* garbage code */ } finally { /* empty */ }";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["min-size"] = 4,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["min-size"] = "ignore methods which have less than N CIL instructions.",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var minSize = Math.Max(1, (int)Option(context, "min-size"));

        var count = 0;
        foreach (var type in context.Module.GetTypes()) {
            foreach (var method in type.Methods) {
                if (!method.HasBody || method.Body.Instructions.Count < minSize)
                    continue;

                if (method.Body.ExceptionHandlers.Count > 0)
                    continue;

                if (context.IsStartupCritical(method))
                    continue;

                if (Wrap(method))
                    count++;
            }
        }

        context.Log($"    {count} functions wrapped in try/finally");
    }

    private static bool Wrap(MethodDef method) {
        var body = method.Body;
        var instructions = body.Instructions;

        var voidReturn = method.ReturnType.RemovePinnedAndModifiers().ElementType == ElementType.Void;
        Local? result = null;
        if (!voidReturn) {
            result = new Local(method.ReturnType);
            body.Variables.Add(result);
        }

        var tryStart = instructions[0];
        var endFinally = OpCodes.Endfinally.ToInstruction();
        var epilogue = voidReturn
            ? OpCodes.Ret.ToInstruction()
            : OpCodes.Ldloc.ToInstruction(result);

        for (var i = 0; i < instructions.Count; i++) {
            if (instructions[i].OpCode != OpCodes.Ret)
                continue;

            if (voidReturn) {
                instructions[i].OpCode = OpCodes.Leave;
                instructions[i].Operand = epilogue;
            }
            else {
                instructions[i].OpCode = OpCodes.Stloc;
                instructions[i].Operand = result;
                instructions.Insert(i + 1, OpCodes.Leave.ToInstruction(epilogue));
                i++;
            }
        }

        instructions.Add(endFinally);
        instructions.Add(epilogue);
        if (!voidReturn)
            instructions.Add(OpCodes.Ret.ToInstruction());

        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally) {
            TryStart = tryStart,
            TryEnd = endFinally,
            HandlerStart = endFinally,
            HandlerEnd = epilogue,
        });

        return true;
    }
}