using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

internal static class VmStackAnalyzer {
    public static bool TryAnalyze(MethodDef method, out Dictionary<Instruction, TypeSig[]> states) {
        var module = method.Module;
        var instructions = method.Body.Instructions;
        var positions = instructions.Select((instruction, index) => (instruction, index))
            .ToDictionary(entry => entry.instruction, entry => entry.index);
        var stacks = new TypeSig[]?[instructions.Count];
        var pending = new Queue<int>();
        var valid = instructions.Count != 0;
        var comparer = new SigComparer();

        bool IsReference(TypeSig type) {
            type = type.RemovePinnedAndModifiers();
            return type.ElementType is ElementType.Class or ElementType.Object or ElementType.String
                       or ElementType.Array or ElementType.SZArray ||
                   type is GenericInstSig instance && !instance.GenericType.IsValueType;
        }

        TypeSig? MergeType(TypeSig left, TypeSig right) {
            left = Normalize(left);
            right = Normalize(right);
            if (comparer.Equals(left, right))
                return left;
            return IsReference(left) && IsReference(right) ? module.CorLibTypes.Object : null;
        }

        TypeSig Normalize(TypeSig type) {
            type = type.RemovePinnedAndModifiers();
            if (type is ClassOrValueTypeSig named && named.TypeDefOrRef.ResolveTypeDef() is { IsEnum: true } definition)
                type = definition.Fields.Single(field => field.IsRuntimeSpecialName && field.Name == "value__")
                    .FieldType;
            return type.ElementType switch {
                ElementType.Boolean or ElementType.Char or ElementType.I1 or ElementType.U1 or ElementType.I2
                    or ElementType.U2 or
                    ElementType.I4 or ElementType.U4 => module.CorLibTypes.Int32,
                ElementType.I8 or ElementType.U8 => module.CorLibTypes.Int64,
                ElementType.I or ElementType.U or ElementType.Ptr or ElementType.FnPtr => module.CorLibTypes.IntPtr,
                _ => type,
            };
        }

        Merge(instructions.Count == 0 ? null : instructions[0], []);
        foreach (var handler in method.Body.ExceptionHandlers) {
            Merge(handler.TryStart, []);
            Merge(handler.HandlerStart, handler.IsCatch || handler.IsFilter ? [module.CorLibTypes.Object] : []);
            if (handler.IsFilter)
                Merge(handler.FilterStart, [module.CorLibTypes.Object]);
        }

        while (valid && pending.TryDequeue(out var position)) {
            var instruction = instructions[position];
            var before = stacks[position]!;
            instruction.CalculateStackUsage(out var pushes, out var pops);
            var code = instruction.OpCode.Code;
            pops = code switch {
                Code.Ret => method.MethodSig.RetType.ElementType == ElementType.Void ? 0 : 1,
                Code.Endfinally or Code.Rethrow => 0,
                Code.Leave or Code.Leave_S => before.Length,
                _ => pops
            };
            if (pops < 0 || before.Length < pops) {
                valid = false;
                break;
            }

            var inputs = before.Skip(before.Length - pops).ToArray();
            var after = before.Take(before.Length - pops).ToList();
            if (pushes != 0) {
                var result = Result(instruction, inputs);
                for (var i = 0; i < pushes; i++)
                    after.Add(result);
            }

            var next = after.ToArray();
            switch (instruction.Operand) {
                case Instruction branchTarget:
                    Merge(branchTarget, next);
                    break;
                case IList<Instruction> targets: {
                    foreach (var switchTarget in targets)
                        Merge(switchTarget, next);
                    break;
                }
            }

            if (code != Code.Jmp &&
                instruction.OpCode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw))
                Merge(position + 1 < instructions.Count ? instructions[position + 1] : null, next);
        }

        states = valid
            ? instructions.ToDictionary(instruction => instruction, instruction => stacks[positions[instruction]]!)
            : [];
        return valid;

        TypeSig Result(Instruction instruction, IReadOnlyList<TypeSig> inputs) {
            var code = instruction.OpCode.Code;
            switch (code) {
                case Code.Dup:
                    return inputs[0];
                case Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or Code.Ldarg or Code.Ldarg_S: {
                    var index = code switch {
                        Code.Ldarg_0 => 0, Code.Ldarg_1 => 1, Code.Ldarg_2 => 2, Code.Ldarg_3 => 3,
                        _ => ((Parameter)instruction.Operand).Index,
                    };
                    return Normalize(method.Parameters[index].Type);
                }
                case Code.Ldarga or Code.Ldarga_S:
                    return new ByRefSig(method.Parameters[((Parameter)instruction.Operand).Index].Type
                        .RemovePinnedAndModifiers());
                case Code.Ldloc_0 or Code.Ldloc_1 or Code.Ldloc_2 or Code.Ldloc_3 or Code.Ldloc or Code.Ldloc_S: {
                    var index = code switch {
                        Code.Ldloc_0 => 0, Code.Ldloc_1 => 1, Code.Ldloc_2 => 2, Code.Ldloc_3 => 3,
                        _ => ((Local)instruction.Operand).Index,
                    };
                    return Normalize(method.Body.Variables[index].Type);
                }
                case Code.Ldloca or Code.Ldloca_S:
                    return new ByRefSig(method.Body.Variables[((Local)instruction.Operand).Index].Type
                        .RemovePinnedAndModifiers());
            }

            if (instruction.IsLdcI4())
                return module.CorLibTypes.Int32;
            switch (code) {
                case Code.Ldc_I8:
                    return module.CorLibTypes.Int64;
                case Code.Ldc_R4:
                    return module.CorLibTypes.Single;
                case Code.Ldc_R8:
                    return module.CorLibTypes.Double;
                case Code.Ldstr:
                    return module.CorLibTypes.String;
                case Code.Ldnull:
                    return module.CorLibTypes.Object;
                case Code.And or Code.Or or Code.Xor or Code.Shl or Code.Shr or Code.Shr_Un or Code.Not:
                    return Normalize(inputs[0]);
                case Code.Add or Code.Sub or Code.Mul or Code.Div or Code.Div_Un or Code.Rem or Code.Rem_Un
                    or Code.Neg or
                    Code.Add_Ovf or Code.Add_Ovf_Un or Code.Sub_Ovf or Code.Sub_Ovf_Un or Code.Mul_Ovf
                    or Code.Mul_Ovf_Un: {
                    var reference = inputs.FirstOrDefault(type => type.RemovePinnedAndModifiers().IsByRef);
                    if (reference is not null)
                        return reference;
                    if (inputs.Any(type => Normalize(type).ElementType == ElementType.R8))
                        return module.CorLibTypes.Double;
                    if (inputs.Any(type => Normalize(type).ElementType == ElementType.R4))
                        return module.CorLibTypes.Single;
                    if (inputs.Any(type => Normalize(type).ElementType == ElementType.I))
                        return module.CorLibTypes.IntPtr;

                    return inputs.Any(type => Normalize(type).ElementType == ElementType.I8)
                        ? module.CorLibTypes.Int64
                        : module.CorLibTypes.Int32;
                }
                case Code.Conv_I1 or Code.Conv_U1 or Code.Conv_I2 or Code.Conv_U2 or Code.Conv_I4 or Code.Conv_U4 or
                    Code.Conv_Ovf_I1 or Code.Conv_Ovf_I1_Un or Code.Conv_Ovf_U1 or Code.Conv_Ovf_U1_Un or
                    Code.Conv_Ovf_I2 or Code.Conv_Ovf_I2_Un or Code.Conv_Ovf_U2 or Code.Conv_Ovf_U2_Un or
                    Code.Conv_Ovf_I4 or Code.Conv_Ovf_I4_Un or Code.Conv_Ovf_U4 or Code.Conv_Ovf_U4_Un:
                    return module.CorLibTypes.Int32;
                case Code.Conv_I8 or Code.Conv_U8 or Code.Conv_Ovf_I8 or Code.Conv_Ovf_I8_Un or Code.Conv_Ovf_U8
                    or Code.Conv_Ovf_U8_Un:
                    return module.CorLibTypes.Int64;
                case Code.Conv_I or Code.Conv_U or Code.Conv_Ovf_I or Code.Conv_Ovf_I_Un or Code.Conv_Ovf_U
                    or Code.Conv_Ovf_U_Un:
                    return module.CorLibTypes.IntPtr;
                case Code.Conv_R4:
                    return module.CorLibTypes.Single;
                case Code.Conv_R8 or Code.Conv_R_Un:
                    return module.CorLibTypes.Double;
                case Code.Ceq or Code.Cgt or Code.Cgt_Un or Code.Clt or Code.Clt_Un or Code.Sizeof:
                    return module.CorLibTypes.Int32;
                case Code.Ckfinite:
                    return Normalize(inputs[0]);
                case Code.Call or Code.Callvirt: {
                    var target = (IMethod)instruction.Operand;
                    return Normalize(VmSignatureResolver.Resolve(target.MethodSig!.RetType, target.DeclaringType,
                        target as MethodSpec));
                }
                case Code.Calli:
                    return Normalize(((MethodSig)instruction.Operand).RetType);
                case Code.Newobj: {
                    var declaringType = ((IMethod)instruction.Operand).DeclaringType;
                    var result = declaringType.ToTypeSig();
                    return !result.IsValueType && VmTypeLayout.IsValueType(declaringType)
                        ? new ValueTypeSig(declaringType)
                        : result;
                }
                case Code.Ldfld or Code.Ldsfld: {
                    var target = (IField)instruction.Operand;
                    return Normalize(VmSignatureResolver.Resolve(target.FieldSig!.Type, target.DeclaringType));
                }
                case Code.Ldflda or Code.Ldsflda: {
                    var target = (IField)instruction.Operand;
                    return new ByRefSig(VmSignatureResolver.Resolve(target.FieldSig!.Type, target.DeclaringType)
                        .RemovePinnedAndModifiers());
                }
                default:
                    switch (code) {
                        case Code.Box:
                        case Code.Castclass or Code.Isinst:
                            return code == Code.Box
                                ? module.CorLibTypes.Object
                                : ((ITypeDefOrRef)instruction.Operand).ToTypeSig();
                        case Code.Unbox:
                            return new ByRefSig(((ITypeDefOrRef)instruction.Operand).ToTypeSig()
                                .RemovePinnedAndModifiers());
                        case Code.Unbox_Any:
                        case Code.Ldelem or Code.Ldobj:
                            return Normalize(((ITypeDefOrRef)instruction.Operand).ToTypeSig());
                        case Code.Newarr:
                            return new SZArraySig(((ITypeDefOrRef)instruction.Operand).ToTypeSig());
                        case Code.Ldlen:
                        case Code.Ldftn or Code.Ldvirtftn or Code.Localloc:
                            return module.CorLibTypes.IntPtr;
                        case Code.Ldelema:
                            return new ByRefSig(((ITypeDefOrRef)instruction.Operand).ToTypeSig()
                                .RemovePinnedAndModifiers());
                        case Code.Ldelem_I1 or Code.Ldelem_U1 or Code.Ldelem_I2 or Code.Ldelem_U2 or Code.Ldelem_I4
                            or Code.Ldelem_U4 or
                            Code.Ldind_I1 or Code.Ldind_U1 or Code.Ldind_I2 or Code.Ldind_U2 or Code.Ldind_I4
                            or Code.Ldind_U4:
                            return module.CorLibTypes.Int32;
                        case Code.Ldelem_I8 or Code.Ldind_I8:
                            return module.CorLibTypes.Int64;
                        case Code.Ldelem_I or Code.Ldind_I:
                            return module.CorLibTypes.IntPtr;
                        case Code.Ldelem_R4 or Code.Ldind_R4:
                            return module.CorLibTypes.Single;
                        case Code.Ldelem_R8 or Code.Ldind_R8:
                            return module.CorLibTypes.Double;
                        default:
                            switch (code) {
                                case Code.Ldelem_Ref: {
                                    var array = inputs[0].RemovePinnedAndModifiers();
                                    return array is SZArraySig or ArraySig ? array.Next : module.CorLibTypes.Object;
                                }
                                default:
                                    switch (code) {
                                        case Code.Ldind_Ref: {
                                            var pointer = inputs[0].RemovePinnedAndModifiers();
                                            return pointer.IsByRef || pointer.ElementType == ElementType.Ptr
                                                ? pointer.Next
                                                : module.CorLibTypes.Object;
                                        }
                                        default:
                                            return code switch {
                                                Code.Mkrefany => module.CorLibTypes.TypedReference,
                                                Code.Refanyval => new ByRefSig(((ITypeDefOrRef)instruction.Operand)
                                                    .ToTypeSig()
                                                    .RemovePinnedAndModifiers()),
                                                Code.Refanytype => new ValueTypeSig(
                                                    module.Import(typeof(RuntimeTypeHandle))),
                                                Code.Arglist =>
                                                    new ValueTypeSig(module.CorLibTypes.GetTypeRef("System",
                                                        nameof(RuntimeArgumentHandle))),
                                                Code.Ldtoken => instruction.Operand is ITypeDefOrRef
                                                    ? new ValueTypeSig(module.Import(typeof(RuntimeTypeHandle)))
                                                    : instruction.Operand is IField
                                                        ? new ValueTypeSig(module.Import(typeof(RuntimeFieldHandle)))
                                                        : new ValueTypeSig(module.Import(typeof(RuntimeMethodHandle))),
                                                _ => module.CorLibTypes.Object
                                            };
                                    }
                            }
                    }

                    break;
            }
        }

        void Merge(Instruction? target, TypeSig[] stack) {
            if (target is null || !positions.TryGetValue(target, out var position)) {
                valid = false;
                return;
            }

            if (stacks[position] is null) {
                stacks[position] = stack;
                pending.Enqueue(position);
                return;
            }

            var current = stacks[position]!;
            if (current.Length != stack.Length) {
                valid = false;
                return;
            }

            var changed = false;
            for (var i = 0; i < current.Length; i++) {
                var merged = MergeType(current[i], stack[i]);
                if (merged is null) {
                    valid = false;
                    return;
                }

                if (comparer.Equals(current[i], merged)) continue;

                current[i] = merged;
                changed = true;
            }

            if (changed)
                pending.Enqueue(position);
        }
    }
}