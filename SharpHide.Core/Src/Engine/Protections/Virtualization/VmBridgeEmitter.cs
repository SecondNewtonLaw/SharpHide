using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Vm;

namespace SharpHide.Core.Engine.Protections;

internal static class VmBridgeEmitter {
    private const int VolatileFlag = 1;
    private const int ReadonlyFlag = 2;
    private const int AlignmentShift = 8;

    public static VmBridgeDescriptor Describe(MethodDef source, Instruction instruction, TypeSig[] stack) {
        instruction.CalculateStackUsage(out _, out var inputCount);
        var inputs = stack.Skip(stack.Length - inputCount).ToArray();
        var constraint = default(TypeSig);
        var flags = 0;
        var position = source.Body.Instructions.IndexOf(instruction) - 1;
        while (position >= 0) {
            var prefix = source.Body.Instructions[position--];
            switch (prefix.OpCode.Code) {
                case Code.Constrained when constraint is not null:
                    throw new NotSupportedException("dup constrained prefix.");
                case Code.Constrained: {
                    var constrainedType = (ITypeDefOrRef)prefix.Operand;
                    constraint = constrainedType.ToTypeSig();
                    if (!constraint.IsValueType && VmTypeLayout.IsValueType(constrainedType))
                        constraint = new ValueTypeSig(constrainedType);
                    continue;
                }
                case Code.Volatile when (flags & VolatileFlag) != 0:
                    throw new NotSupportedException("dup volatile prefix.");
                case Code.Volatile:
                    flags |= VolatileFlag;
                    continue;
                case Code.Readonly when (flags & ReadonlyFlag) != 0:
                    throw new NotSupportedException("dup readonly prefix.");
                case Code.Readonly:
                    flags |= ReadonlyFlag;
                    continue;
                case Code.Unaligned: {
                    var alignment = (byte)prefix.Operand;
                    if (alignment is not (1 or 2 or 4) || flags >> AlignmentShift != 0)
                        throw new NotSupportedException("Invalid unaligned prefix.");
                    flags |= alignment << AlignmentShift;
                    continue;
                }
                case Code.Tailcall:
                    continue;
                default:
                    var exception = new ArgumentOutOfRangeException();
                    exception.HelpLink = null;
                    exception.HResult = 0;
                    exception.Source = null;
                    throw exception;
            }

            break;
        }

        var code = instruction.OpCode.Code;

        if (constraint is not null && code != Code.Callvirt || (flags & ReadonlyFlag) != 0 && code != Code.Ldelema)
            throw new NotSupportedException("bad operation prefix.");

        var isMemory = code is Code.Ldfld or Code.Stfld or Code.Ldobj or Code.Stobj or Code.Initblk or Code.Cpblk or
            Code.Ldind_I1 or Code.Ldind_U1 or Code.Ldind_I2 or Code.Ldind_U2 or Code.Ldind_I4 or Code.Ldind_U4 or
            Code.Ldind_I8 or Code.Ldind_I or Code.Ldind_R4 or Code.Ldind_R8 or Code.Ldind_Ref or
            Code.Stind_I1 or Code.Stind_I2 or Code.Stind_I4 or Code.Stind_I8 or Code.Stind_I or
            Code.Stind_R4 or Code.Stind_R8 or Code.Stind_Ref;

        if (flags >> AlignmentShift != 0 && !isMemory ||
            (flags & VolatileFlag) != 0 && !isMemory && code is not (Code.Ldsfld or Code.Stsfld))
            throw new NotSupportedException("bad memory prefix.");

        AdjustInputs(source.Module, instruction, inputs, constraint);
        var result = ExactResult(source.Module, instruction, inputs);

        return new VmBridgeDescriptor(instruction, inputs, result, constraint, flags,
            inputs.Select(input => VmTypeLayout.RequiresNative(source, input)).ToArray(),
            VmTypeLayout.RequiresNative(source, result));
    }

    public static MethodDef Create(ModuleDef module, MethodDef source, InjectResult runtime,
        VmBridgeDescriptor descriptor) {
        var bridge = new MethodDefUser(UniqueName(source), MethodSig.CreateStatic(module.CorLibTypes.Object,
                new SZArraySig(module.CorLibTypes.Object), module.CorLibTypes.IntPtr),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig) {
            Body = new CilBody { InitLocals = true },
        };
        VmTypeLayout.CopyGenericParameters(source, bridge);
        source.DeclaringType.Methods.Add(bridge);
        EmitBody(module, bridge, runtime, descriptor);
        return bridge;
    }

    private static string UniqueName(MethodDef source) {
        var prefix = source.Name + "$vmbridge";
        var name = prefix;
        for (var suffix = 2; source.DeclaringType.Methods.Any(candidate => candidate.Name == name); suffix++)
            name = $"{prefix}{suffix}";
        return name;
    }

    private static void EmitBody(ModuleDef module, MethodDef bridge, InjectResult runtime,
        VmBridgeDescriptor descriptor) {
        var il = bridge.Body.Instructions;
        var result = descriptor.Result.RemovePinnedAndModifiers();
        if (result.IsByRef || descriptor.NativeResult)
            il.Add(OpCodes.Ldarg_1.ToInstruction());
        for (var index = 0; index < descriptor.Inputs.Length; index++) {
            il.Add(OpCodes.Ldarg_0.ToInstruction());
            il.Add(OpCodes.Ldc_I4.ToInstruction(index));
            il.Add(OpCodes.Ldelem_Ref.ToInstruction());
            EmitLoad(il, descriptor.Inputs[index], descriptor.NativeInputs[index], runtime);
        }

        if (descriptor.Constraint is not null)
            il.Add(OpCodes.Constrained.ToInstruction(descriptor.Constraint.ToTypeDefOrRef()));
        if ((descriptor.Flags & VolatileFlag) != 0)
            il.Add(OpCodes.Volatile.ToInstruction());
        if ((descriptor.Flags & ReadonlyFlag) != 0)
            il.Add(OpCodes.Readonly.ToInstruction());
        if (descriptor.Flags >> AlignmentShift != 0)
            il.Add(OpCodes.Unaligned.ToInstruction((byte)(descriptor.Flags >> AlignmentShift)));

        var instruction = descriptor.Instruction;
        if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch) {
            var yes = OpCodes.Ldc_I4_1.ToInstruction();
            var end = OpCodes.Nop.ToInstruction();
            il.Add(instruction.OpCode.ToInstruction(yes));
            il.Add(OpCodes.Ldc_I4_0.ToInstruction());
            il.Add(OpCodes.Br.ToInstruction(end));
            il.Add(yes);
            il.Add(end);
        }
        else {
            switch (instruction.OpCode.OperandType) {
                case OperandType.InlineMethod:
                    il.Add(instruction.OpCode.ToInstruction((IMethod)instruction.Operand));
                    break;
                case OperandType.InlineField:
                    il.Add(instruction.OpCode.ToInstruction((IField)instruction.Operand));
                    break;
                case OperandType.InlineType:
                    il.Add(instruction.OpCode.ToInstruction((ITypeDefOrRef)instruction.Operand));
                    break;
                case OperandType.InlineTok:
                    il.Add(instruction.Operand switch {
                        ITypeDefOrRef type => instruction.OpCode.ToInstruction(type),
                        IField field => instruction.OpCode.ToInstruction(field),
                        IMethod method => instruction.OpCode.ToInstruction(method),
                        _ => throw new InvalidOperationException(),
                    });
                    break;
                case OperandType.InlineSig:
                    il.Add(instruction.OpCode.ToInstruction((MethodSig)instruction.Operand));
                    break;
                case OperandType.InlineString:
                    il.Add(instruction.OpCode.ToInstruction((string)instruction.Operand));
                    break;
                case OperandType.ShortInlineI:
                case OperandType.InlineI:
                    il.Add(instruction.OpCode.ToInstruction((int)instruction.Operand));
                    break;
                case OperandType.InlineI8:
                    il.Add(instruction.OpCode.ToInstruction((long)instruction.Operand));
                    break;
                case OperandType.ShortInlineR:
                    il.Add(instruction.OpCode.ToInstruction((float)instruction.Operand));
                    break;
                case OperandType.InlineR:
                    il.Add(instruction.OpCode.ToInstruction((double)instruction.Operand));
                    break;
                case OperandType.InlineNone:
                    il.Add(instruction.OpCode.ToInstruction());
                    break;
                default:
                    throw new NotSupportedException("Unsupported bridge operand.");
            }
        }

        if (result.IsByRef) {
            il.Add(OpCodes.Mkrefany.ToInstruction(result.Next.ToTypeDefOrRef()));
            il.Add(OpCodes.Stobj.ToInstruction(module.Import(typeof(TypedReference))));
            il.Add(OpCodes.Ldnull.ToInstruction());
        }
        else if (descriptor.NativeResult) {
            il.Add(OpCodes.Stobj.ToInstruction(result.ToTypeDefOrRef()));
            il.Add(OpCodes.Ldnull.ToInstruction());
        }
        else if (result.ElementType == ElementType.Void) {
            il.Add(OpCodes.Ldnull.ToInstruction());
        }
        else if (result.ElementType is ElementType.Ptr or ElementType.FnPtr) {
            il.Add(OpCodes.Conv_I.ToInstruction());
            il.Add(OpCodes.Box.ToInstruction(module.CorLibTypes.IntPtr.TypeDefOrRef));
        }
        else if (result.IsValueType || result is GenericSig) {
            il.Add(OpCodes.Box.ToInstruction(result.ToTypeDefOrRef()));
        }

        il.Add(OpCodes.Ret.ToInstruction());
        bridge.Body.OptimizeMacros();
        bridge.Body.OptimizeBranches();
    }

    private static void EmitLoad(IList<Instruction> il, TypeSig signature, bool native, InjectResult runtime) {
        var type = signature.RemovePinnedAndModifiers();
        if (type.IsByRef) {
            var referenceType = runtime.ResolveType(typeof(VmReference))!;
            var address = runtime.ResolveMethod(typeof(VmReference).GetMethod(nameof(VmReference.GetAddress))!)!;
            var element = type.Next.RemovePinnedAndModifiers();
            if (element.ElementType is ElementType.Ptr or ElementType.FnPtr)
                element = referenceType.Module.CorLibTypes.IntPtr;
            il.Add(OpCodes.Castclass.ToInstruction(referenceType));
            il.Add(OpCodes.Callvirt.ToInstruction(new MethodSpecUser(address, new GenericInstMethodSig(element))));
            return;
        }

        if (native) {
            var nativeType = runtime.ResolveType(typeof(VmNativeValue))!;
            var address = runtime.ResolveField(typeof(VmNativeValue).GetField(nameof(VmNativeValue.Address))!)!;
            il.Add(OpCodes.Castclass.ToInstruction(nativeType));
            il.Add(OpCodes.Ldfld.ToInstruction(address));
            il.Add(OpCodes.Ldobj.ToInstruction(type.ToTypeDefOrRef()));
            return;
        }

        il.Add((type.IsValueType || type is GenericSig ? OpCodes.Unbox_Any : OpCodes.Castclass).ToInstruction(
            type.ToTypeDefOrRef()));
    }

    private static void AdjustInputs(ModuleDef module, Instruction instruction, TypeSig[] inputs, TypeSig? constraint) {
        var code = instruction.OpCode.Code;
        switch (instruction.Operand) {
            case IMethod target when code is Code.Call or Code.Callvirt or Code.Newobj: {
                var signature = target.MethodSig!;
                var start = 0;
                if (code != Code.Newobj && signature.HasThis) {
                    var declaringType = target.DeclaringType.ToTypeSig();
                    if (!declaringType.IsValueType && VmTypeLayout.IsValueType(target.DeclaringType))
                        declaringType = new ValueTypeSig(target.DeclaringType);
                    inputs[0] = constraint is not null ? new ByRefSig(constraint.RemovePinnedAndModifiers())
                        : declaringType.IsValueType ? new ByRefSig(declaringType) : declaringType;
                    start = 1;
                }

                var parameters = signature.Params.Concat(signature.ParamsAfterSentinel ?? []).ToArray();
                for (var index = 0; index < parameters.Length; index++) {
                    var parameter =
                        VmSignatureResolver.Resolve(parameters[index], target.DeclaringType, target as MethodSpec);
                    if (NeedsExactLoad(parameter))
                        inputs[start + index] = parameter;
                }

                return;
            }
            case MethodSig callSite when code == Code.Calli: {
                var start = callSite.HasThis ? 1 : 0;
                for (var index = 0; index < callSite.Params.Count; index++)
                    if (NeedsExactLoad(callSite.Params[index]))
                        inputs[start + index] = callSite.Params[index];
                inputs[^1] = module.CorLibTypes.IntPtr;
                return;
            }
            case IField field when
                code is Code.Ldfld or Code.Ldsfld or Code.Stfld or Code.Stsfld or Code.Ldflda or Code.Ldsflda: {
                var fieldSignature = field.FieldSig ?? field.ResolveFieldDef()?.FieldSig ??
                    throw new NotSupportedException(
                        $"bad field signature: {instruction.OpCode.Name} {field.FullName}");
                var fieldType = VmSignatureResolver.Resolve(fieldSignature.Type, field.DeclaringType);
                if (code is not (Code.Ldsfld or Code.Stsfld or Code.Ldsflda)) {
                    var declaringType = field.DeclaringType.ToTypeSig();
                    if (!inputs[0].RemovePinnedAndModifiers().IsByRef)
                        inputs[0] = declaringType;
                }

                if (code is Code.Stfld or Code.Stsfld && NeedsExactLoad(fieldType))
                    inputs[^1] = fieldType;
                break;
            }
        }

        if (code == Code.Ldvirtftn)
            inputs[0] = ((IMethod)instruction.Operand).DeclaringType.ToTypeSig();
    }

    private static bool NeedsExactLoad(TypeSig signature) {
        var type = signature.RemovePinnedAndModifiers();
        return type.IsByRef || type is GenericSig || IsByRefLike(type) ||
               !type.IsValueType && type.ElementType is not (ElementType.Ptr or ElementType.FnPtr);
    }

    private static TypeSig ExactResult(ModuleDef module, Instruction instruction, IReadOnlyList<TypeSig> inputs) {
        var code = instruction.OpCode.Code;
        if (code is Code.Add or Code.Sub or Code.Add_Ovf_Un or Code.Sub_Ovf_Un) {
            var reference = inputs.FirstOrDefault(type => type.RemovePinnedAndModifiers().IsByRef);
            if (reference is not null)
                return reference;
        }

        if (instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
            return module.CorLibTypes.Int32;

        switch (code) {
            case Code.Call or Code.Callvirt: {
                var target = (IMethod)instruction.Operand;
                return VmSignatureResolver.Resolve(target.MethodSig!.RetType, target.DeclaringType,
                    target as MethodSpec);
            }
            case Code.Calli:
                return ((MethodSig)instruction.Operand).RetType;
            case Code.Newobj: {
                var declaringType = ((IMethod)instruction.Operand).DeclaringType;
                var result = declaringType.ToTypeSig();
                return !result.IsValueType && VmTypeLayout.IsValueType(declaringType)
                    ? new ValueTypeSig(declaringType)
                    : result;
            }
            case Code.Ldfld or Code.Ldsfld: {
                var target = (IField)instruction.Operand;
                return VmSignatureResolver.Resolve(target.FieldSig!.Type, target.DeclaringType);
            }
            case Code.Ldflda or Code.Ldsflda: {
                var target = (IField)instruction.Operand;
                return new ByRefSig(VmSignatureResolver.Resolve(target.FieldSig!.Type, target.DeclaringType)
                    .RemovePinnedAndModifiers());
            }
            case Code.Ldelema or Code.Unbox:
                return new ByRefSig(((ITypeDefOrRef)instruction.Operand).ToTypeSig().RemovePinnedAndModifiers());
            case Code.Unbox_Any or Code.Ldelem or Code.Ldobj:
                return ((ITypeDefOrRef)instruction.Operand).ToTypeSig();
            case Code.Box:
            case Code.Castclass or Code.Isinst:
            case Code.Newarr:
            case Code.Ldelem_Ref:
            case Code.Ldind_Ref:
                return module.CorLibTypes.Object;
            case Code.Ldstr:
                return module.CorLibTypes.String;
            case Code.Ldnull:
                return module.CorLibTypes.Object;
            case Code.Ckfinite:
                return inputs[^1];
            case Code.Ldc_I8:
                return module.CorLibTypes.Int64;
            case Code.Ldc_R4:
                return module.CorLibTypes.Single;
            case Code.Ldc_R8 or Code.Conv_R8 or Code.Conv_R_Un:
                return module.CorLibTypes.Double;
            case Code.Conv_R4:
                return module.CorLibTypes.Single;
            case Code.Ldftn or Code.Ldvirtftn or Code.Ldlen or Code.Localloc or Code.Ldelem_I or Code.Ldind_I
                or Code.Conv_I or Code.Conv_U or
                Code.Conv_Ovf_I or Code.Conv_Ovf_I_Un or Code.Conv_Ovf_U or Code.Conv_Ovf_U_Un:
                return module.CorLibTypes.IntPtr;
            case Code.Conv_I8 or Code.Conv_U8 or Code.Conv_Ovf_I8 or Code.Conv_Ovf_I8_Un or Code.Conv_Ovf_U8
                or Code.Conv_Ovf_U8_Un or
                Code.Ldelem_I8 or Code.Ldind_I8:
                return module.CorLibTypes.Int64;
            case Code.Mkrefany:
                return module.CorLibTypes.TypedReference;
            case Code.Refanyval:
                return new ByRefSig(((ITypeDefOrRef)instruction.Operand).ToTypeSig());
            case Code.Refanytype:
                return new ValueTypeSig(module.Import(typeof(RuntimeTypeHandle)));
            case Code.Ldtoken:
                return instruction.Operand is ITypeDefOrRef ? new ValueTypeSig(module.Import(typeof(RuntimeTypeHandle)))
                    : instruction.Operand is IField ? new ValueTypeSig(module.Import(typeof(RuntimeFieldHandle)))
                    : new ValueTypeSig(module.Import(typeof(RuntimeMethodHandle)));
        }

        return instruction.OpCode.StackBehaviourPush == StackBehaviour.Push0
            ? module.CorLibTypes.Void
            : module.CorLibTypes.Int32;
    }

    private static bool IsByRefLike(TypeSig signature) {
        var type = signature.RemovePinnedAndModifiers();

        if (type is GenericInstSig instance)
            type = instance.GenericType;

        if (type is not ClassOrValueTypeSig named) return type.ElementType == ElementType.TypedByRef;

        var definition = named.TypeDefOrRef.ResolveTypeDef();

        if (definition?.CustomAttributes.Any(attribute =>
                attribute.TypeFullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute") == true)
            return true;

        return (Type.GetType(named.AssemblyQualifiedName, false) ??
                typeof(object).Assembly.GetType(named.FullName))?.IsByRefLike == true;
    }

    public static bool IsSupported(Code code) => code is
        Code.Ceq or Code.Cgt or Code.Cgt_Un or Code.Clt or Code.Clt_Un or
        Code.Conv_I1 or Code.Conv_I2 or Code.Conv_I4 or Code.Conv_I8 or Code.Conv_I or
        Code.Conv_U1 or Code.Conv_U2 or Code.Conv_U4 or Code.Conv_U8 or Code.Conv_U or
        Code.Conv_R4 or Code.Conv_R8 or Code.Conv_R_Un or
        Code.Conv_Ovf_I1 or Code.Conv_Ovf_I2 or Code.Conv_Ovf_I4 or Code.Conv_Ovf_I8 or Code.Conv_Ovf_I or
        Code.Conv_Ovf_U1 or Code.Conv_Ovf_U2 or Code.Conv_Ovf_U4 or Code.Conv_Ovf_U8 or Code.Conv_Ovf_U or
        Code.Conv_Ovf_I1_Un or Code.Conv_Ovf_I2_Un or Code.Conv_Ovf_I4_Un or Code.Conv_Ovf_I8_Un or Code.Conv_Ovf_I_Un
        or
        Code.Conv_Ovf_U1_Un or Code.Conv_Ovf_U2_Un or Code.Conv_Ovf_U4_Un or Code.Conv_Ovf_U8_Un or Code.Conv_Ovf_U_Un
        or
        Code.Ldc_I8 or Code.Ldc_R4 or Code.Ldc_R8 or Code.Ldstr or Code.Ldnull or
        Code.Call or Code.Callvirt or Code.Calli or Code.Newobj or Code.Ldtoken or Code.Ldftn or Code.Ldvirtftn or
        Code.Ldfld or Code.Ldsfld or Code.Stfld or Code.Stsfld or Code.Ldflda or Code.Ldsflda or
        Code.Box or Code.Unbox or Code.Unbox_Any or Code.Castclass or Code.Isinst or
        Code.Newarr or Code.Ldlen or Code.Ldelema or Code.Ldelem or
        Code.Ldelem_I1 or Code.Ldelem_U1 or Code.Ldelem_I2 or Code.Ldelem_U2 or Code.Ldelem_I4 or Code.Ldelem_U4 or
        Code.Ldelem_I8 or Code.Ldelem_I or Code.Ldelem_R4 or Code.Ldelem_R8 or Code.Ldelem_Ref or
        Code.Stelem or Code.Stelem_I1 or Code.Stelem_I2 or Code.Stelem_I4 or Code.Stelem_I8 or Code.Stelem_I or
        Code.Stelem_R4 or Code.Stelem_R8 or Code.Stelem_Ref or
        Code.Ldind_I1 or Code.Ldind_U1 or Code.Ldind_I2 or Code.Ldind_U2 or Code.Ldind_I4 or Code.Ldind_U4 or
        Code.Ldind_I8 or Code.Ldind_I or Code.Ldind_R4 or Code.Ldind_R8 or Code.Ldind_Ref or
        Code.Stind_I1 or Code.Stind_I2 or Code.Stind_I4 or Code.Stind_I8 or Code.Stind_I or
        Code.Stind_R4 or Code.Stind_R8 or Code.Stind_Ref or
        Code.Sizeof or Code.Ckfinite or Code.Ldobj or Code.Stobj or Code.Initobj or Code.Cpobj or
        Code.Initblk or Code.Cpblk or Code.Mkrefany or Code.Refanyval or Code.Refanytype or Code.Throw or
        Code.Add or Code.Sub or Code.Add_Ovf_Un or Code.Sub_Ovf_Un;
}