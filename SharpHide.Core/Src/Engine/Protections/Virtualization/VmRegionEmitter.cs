using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

internal static class VmRegionEmitter {
    public static bool HasMarkers(MethodDef method) => method.HasBody &&
                                                       method.Body.Instructions.Any(IsBeginOrEnd);

    public static IReadOnlyList<MethodDef> Create(MethodDef method) {
        if (!method.HasBody || method.Body.HasExceptionHandlers ||
            method.Parameters.Count + method.Body.Variables.Count > ushort.MaxValue ||
            !VmStackAnalyzer.TryAnalyze(method, out var stacks))
            return [];
        var instructions = method.Body.Instructions;
        var pairs = new List<(Instruction Begin, Instruction End)>();
        Instruction? begin = null;
        foreach (var instruction in instructions) {
            if (IsBegin(instruction)) {
                if (begin is not null || !IsMarkerSignature(instruction) || stacks[instruction].Length != 0)
                    return [];
                begin = instruction;
            }
            else if (IsEnd(instruction)) {
                if (begin is null || !IsMarkerSignature(instruction) || stacks[instruction].Length != 0)
                    return [];
                pairs.Add((begin, instruction));
                begin = null;
            }
        }

        if (begin is not null || pairs.Count == 0)
            return [];
        var positions = instructions.Select((instruction, index) => (instruction, index))
            .ToDictionary(entry => entry.instruction, entry => entry.index);

        foreach (var pair in pairs) {
            var first = positions[pair.Begin] + 1;
            var last = positions[pair.End];
            if (first >= last || !ValidateRegion(method, first, last, positions))
                return [];
        }

        method.Body.SimplifyBranches();
        var helpers = pairs.Select((pair, index) => CreateHelper(method, pair.Begin, pair.End, index)).ToArray();
        for (var index = pairs.Count - 1; index >= 0; index--)
            ReplaceRegion(method, pairs[index].Begin, pairs[index].End, helpers[index]);
        method.Body.OptimizeMacros();
        method.Body.OptimizeBranches();
        return helpers;
    }

    private static bool ValidateRegion(MethodDef method, int first, int last,
        IReadOnlyDictionary<Instruction, int> positions) {
        var instructions = method.Body.Instructions;
        for (var index = first; index < last; index++) {
            var instruction = instructions[index];
            if (instruction.OpCode.Code is Code.Ret or Code.Jmp or Code.Rethrow or Code.Endfilter or Code.Endfinally)
                return false;
            if (TryLocalIndex(instruction, out var localIndex) &&
                method.Body.Variables[localIndex].Type.RemovePinnedAndModifiers().IsByRef &&
                (instruction.IsStloc() || IsLdloca(instruction)))
                return false;
        }

        for (var index = 0; index < instructions.Count; index++) {
            var sourceInside = index >= first && index < last;
            if (instructions[index].Operand is Instruction target) {
                var targetInside = positions[target] >= first && positions[target] < last;
                if (sourceInside != targetInside)
                    return false;
            }
            else if (instructions[index].Operand is IList<Instruction> targets &&
                     targets.Any(target => sourceInside != (positions[target] >= first && positions[target] < last)))
                return false;
        }

        return true;
    }

    private static MethodDef CreateHelper(MethodDef source, Instruction begin, Instruction end, int number) {
        var module = source.Module;
        var parameters = source.Parameters.Select(parameter => parameter.Type)
            .Concat(source.Body.Variables.Select(local => local.Type.RemovePinnedAndModifiers().IsByRef
                ? local.Type
                : new ByRefSig(local.Type))).ToArray();
        var helper = new MethodDefUser(UniqueName(source, number),
            MethodSig.CreateStatic(module.CorLibTypes.Void, parameters),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig |
            MethodAttributes.SpecialName) {
            Body = new CilBody { InitLocals = true },
        };
        VmTypeLayout.CopyGenericParameters(source, helper);
        source.DeclaringType.Methods.Add(helper);

        var original = source.Body.Instructions;
        var first = original.IndexOf(begin) + 1;
        var last = original.IndexOf(end);
        var map = new Dictionary<Instruction, Instruction>();
        var fixups = new List<(Instruction Copy, object Operand)>();
        var temporary = source.Body.Variables.Select(local => {
            var value = new Local(local.Type);
            helper.Body.Variables.Add(value);
            return value;
        }).ToArray();
        for (var index = first; index < last; index++) {
            var instruction = original[index];
            var emitted = CloneInstruction(source, helper, instruction, temporary);
            map[instruction] = emitted[0];
            foreach (var copy in emitted) {
                helper.Body.Instructions.Add(copy);
                if (copy.Operand is Instruction or IList<Instruction>)
                    fixups.Add((copy, copy.Operand));
            }
        }

        foreach (var (copy, operand) in fixups)
            copy.Operand = operand is Instruction target
                ? map[target]
                : ((IList<Instruction>)operand).Select(target => map[target]).ToArray();
        helper.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        helper.Body.OptimizeMacros();
        helper.Body.OptimizeBranches();
        return helper;
    }

    private static Instruction[] CloneInstruction(MethodDef source, MethodDef helper, Instruction instruction,
        IReadOnlyList<Local> temporary) {
        if (TryLocalIndex(instruction, out var localIndex)) {
            var localParameter = helper.Parameters[source.Parameters.Count + localIndex];
            var type = source.Body.Variables[localIndex].Type;
            if (instruction.IsLdloc()) {
                if (type.RemovePinnedAndModifiers().IsByRef)
                    return [OpCodes.Ldarg.ToInstruction(localParameter)];
                return
                    [OpCodes.Ldarg.ToInstruction(localParameter), OpCodes.Ldobj.ToInstruction(type.ToTypeDefOrRef())];
            }

            if (instruction.IsStloc())
                return [
                    OpCodes.Stloc.ToInstruction(temporary[localIndex]), OpCodes.Ldarg.ToInstruction(localParameter),
                    OpCodes.Ldloc.ToInstruction(temporary[localIndex]),
                    OpCodes.Stobj.ToInstruction(type.ToTypeDefOrRef())
                ];
            return [OpCodes.Ldarg.ToInstruction(localParameter)];
        }

        var copy = new Instruction(instruction.OpCode, instruction.Operand);
        if (copy.Operand is Parameter parameter)
            copy.Operand = helper.Parameters[parameter.Index];
        return [copy];
    }

    private static void ReplaceRegion(MethodDef source, Instruction begin, Instruction end, MethodDef helper) {
        var instructions = source.Body.Instructions;
        var first = instructions.IndexOf(begin);
        var last = instructions.IndexOf(end);
        for (var index = last; index >= first; index--)
            instructions.RemoveAt(index);
        var call = (IMethod)helper;
        if (helper.GenericParameters.Count != 0)
            call = new MethodSpecUser(helper, new GenericInstMethodSig(helper.GenericParameters
                .Select(parameter => (TypeSig)new GenericMVar(parameter.Number)).ToArray()));

        var replacement = source.Parameters.Select(parameter => OpCodes.Ldarg.ToInstruction(parameter)).ToList();
        replacement.AddRange(source.Body.Variables.Select(local =>
            (local.Type.RemovePinnedAndModifiers().IsByRef ? OpCodes.Ldloc : OpCodes.Ldloca).ToInstruction(local)));

        replacement.Add(OpCodes.Call.ToInstruction(call));
        for (var index = 0; index < replacement.Count; index++)
            instructions.Insert(first + index, replacement[index]);
    }

    private static bool TryLocalIndex(Instruction instruction, out int index) {
        index = instruction.OpCode.Code switch {
            Code.Ldloc_0 or Code.Stloc_0 => 0,
            Code.Ldloc_1 or Code.Stloc_1 => 1,
            Code.Ldloc_2 or Code.Stloc_2 => 2,
            Code.Ldloc_3 or Code.Stloc_3 => 3,
            _ => instruction.Operand is Local local ? local.Index : -1,
        };
        return instruction.IsLdloc() || instruction.IsStloc() || IsLdloca(instruction);
    }

    private static bool IsLdloca(Instruction instruction) => instruction.OpCode.Code is Code.Ldloca or Code.Ldloca_S;

    private static bool IsBeginOrEnd(Instruction instruction) => IsBegin(instruction) || IsEnd(instruction);

    private static bool IsBegin(Instruction instruction) =>
        IsCallNamed(instruction, "VMBegin"); // we should PROBABLY do this using a full name and nameof, but fuck it.

    private static bool IsEnd(Instruction instruction) => IsCallNamed(instruction, "VMEnd");

    private static bool IsCallNamed(Instruction instruction, string name) =>
        instruction.OpCode.Code is Code.Call or Code.Callvirt && instruction.Operand is IMethod method &&
        method.Name == name;

    private static bool IsMarkerSignature(Instruction instruction) =>
        instruction.Operand is IMethod { MethodSig: { } signature } &&
        !signature.HasThis && signature.Params.Count == 0 && signature.RetType.ElementType == ElementType.Void;

    private static string UniqueName(MethodDef source, int number) {
        var prefix = $"{source.Name}$vmregion{(number + 1)}";
        var name = prefix;
        for (var suffix = 2; source.DeclaringType.Methods.Any(method => method.Name == name); suffix++)
            name = $"{prefix}${suffix}";
        return name;
    }
}