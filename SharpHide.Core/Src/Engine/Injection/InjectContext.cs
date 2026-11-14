using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Injection;

internal sealed class InjectContext {
    private Importer
        _importer; // lonely, lonely, it gets so lonely being the only non-readonly being the only non-readonlyyyy.

    private readonly ModuleDef _target;
    private readonly Dictionary<IMemberDef, IMemberDef> _map = new();
    private readonly Dictionary<string, ModuleRef> _moduleRefs = new();
    private readonly Queue<Action> _pending = new();

    public InjectContext(ModuleDef target) {
        _target = target;
        _importer = new Importer(target, ImporterOptions.TryToUseExistingAssemblyRefs, default, new Mapper(this));
    }

    private bool IsSource(IMemberDef? def) => def?.Module != null && def.Module != _target;

    public void Drain() {
        while (_pending.Count > 0)
            _pending.Dequeue()();
    }

    public IMemberDef? Lookup(IMemberDef source) => _map.GetValueOrDefault(source);

    public TypeDef CloneType(TypeDef source) {
        if (_map.TryGetValue(source, out var existing))
            return (TypeDef)existing;

        var newType = new TypeDefUser(source.Namespace, source.Name);
        _map[source] = newType;

        if (source.DeclaringType is { } declaringType && IsSource(declaringType))
            CloneType(declaringType).NestedTypes.Add(newType);
        else
            _target.Types.Add(newType);

        _pending.Enqueue(() => PopulateType(source, newType));
        return newType;
    }

    public MethodDef CloneMethod(MethodDef source, TypeDef? owner = null) {
        if (_map.TryGetValue(source, out var existing))
            return (MethodDef)existing;

        var newMethod = new MethodDefUser(source.Name);
        _map[source] = newMethod;
        (owner ?? CloneType(source.DeclaringType!)).Methods.Add(newMethod);

        _pending.Enqueue(() => PopulateMethod(source, newMethod));

        return newMethod;
    }

    public FieldDef CloneField(FieldDef source, TypeDef? owner = null) {
        if (_map.TryGetValue(source, out var existing))
            return (FieldDef)existing;

        var newField = new FieldDefUser(source.Name);
        _map[source] = newField;

        (owner ?? CloneType(source.DeclaringType!)).Fields.Add(newField);

        _pending.Enqueue(() => PopulateField(source, newField));

        return newField;
    }

    private void PopulateType(TypeDef source, TypeDef newType) {
        newType.Attributes = source.Attributes;
        if (source.ClassLayout is { } layout)
            newType.ClassLayout = new ClassLayoutUser(layout.PackingSize, layout.ClassSize);

        CopyGenericParams(source, newType);
        if (source.BaseType is { } baseType)
            newType.BaseType = _importer.Import(baseType);

        foreach (var impl in source.Interfaces)
            newType.Interfaces.Add(new InterfaceImplUser(_importer.Import(impl.Interface)));

        foreach (var field in source.Fields)
            CloneField(field);

        foreach (var method in source.Methods)
            CloneMethod(method);

        foreach (var nested in source.NestedTypes)
            CloneType(nested);

        foreach (var property in source.Properties) {
            var newProperty = new PropertyDefUser(property.Name, _importer.Import(property.PropertySig)) {
                Attributes = property.Attributes,
            };
            foreach (var getter in property.GetMethods)
                newProperty.GetMethods.Add(CloneMethod(getter));
            foreach (var setter in property.SetMethods)
                newProperty.SetMethods.Add(CloneMethod(setter));
            foreach (var other in property.OtherMethods)
                newProperty.OtherMethods.Add(CloneMethod(other));

            if (property.Constant is { } constant)
                newProperty.Constant = new ConstantUser(constant.Value, constant.Type);

            CopyCustomAttributes(property, newProperty);
            newType.Properties.Add(newProperty);
        }

        foreach (var evt in source.Events) {
            var newEvent = new EventDefUser(evt.Name, _importer.Import(evt.EventType), evt.Attributes);

            if (evt.AddMethod is { } adder)
                newEvent.AddMethod = CloneMethod(adder);

            if (evt.RemoveMethod is { } remover)
                newEvent.RemoveMethod = CloneMethod(remover);

            if (evt.InvokeMethod is { } invoker)
                newEvent.InvokeMethod = CloneMethod(invoker);

            foreach (var other in evt.OtherMethods)
                newEvent.OtherMethods.Add(CloneMethod(other));

            CopyCustomAttributes(evt, newEvent);
            newType.Events.Add(newEvent);
        }

        CopyCustomAttributes(source, newType);
    }

    private void PopulateField(FieldDef source, FieldDef newField) {
        newField.Attributes = source.Attributes;
        newField.Signature = _importer.Import(source.FieldSig);

        if (source is { HasFieldRVA: true, InitialValue: { } initialValue })
            newField.InitialValue = initialValue;

        if (source.Constant is { } constant)
            newField.Constant = new ConstantUser(constant.Value, constant.Type);

        if (source.FieldOffset is { } offset)
            newField.FieldOffset = offset;

        if (source.MarshalType is { } marshalType)
            newField.MarshalType = marshalType;

        CopyCustomAttributes(source, newField);
    }

    private void PopulateMethod(MethodDef source, MethodDef newMethod) {
        newMethod.Attributes = source.Attributes;
        newMethod.ImplAttributes = source.ImplAttributes;
        CopyGenericParams(source, newMethod);
        newMethod.Signature = _importer.Import(source.Signature);
        newMethod.Parameters.UpdateParameterTypes();
        if (source.MethodSig.HasThis)
            newMethod.Parameters[0].Type = _importer.Import(source.Parameters[0].Type);
        foreach (var paramDef in source.ParamDefs) {
            var newParamDef = new ParamDefUser(paramDef.Name, paramDef.Sequence, paramDef.Attributes);

            if (paramDef.Constant is { } constant)
                newParamDef.Constant = new ConstantUser(constant.Value, constant.Type);

            if (paramDef.MarshalType is { } marshalType)
                newParamDef.MarshalType = marshalType;

            CopyCustomAttributes(paramDef, newParamDef);
            newMethod.ParamDefs.Add(newParamDef);
        }

        if (source.ImplMap is { } implMap)
            newMethod.ImplMap = new ImplMapUser(GetModuleRef(implMap.Module), implMap.Name, implMap.Attributes);

        if (source.HasBody)
            newMethod.Body = CloneBody(source.Body, newMethod);

        foreach (var methodOverride in source.Overrides)
            newMethod.Overrides.Add(new MethodOverride(newMethod,
                _importer.Import(methodOverride.MethodDeclaration) as IMethodDefOrRef));

        CopyCustomAttributes(source, newMethod);
    }

    private ExceptionHandler RemapExceptionHandler(ExceptionHandler handler,
        Dictionary<Instruction, Instruction> instrMap) =>
        new(handler.HandlerType) {
            TryStart = instrMap[handler.TryStart],
            TryEnd = handler.TryEnd is null ? null : instrMap[handler.TryEnd],
            HandlerStart = instrMap[handler.HandlerStart],
            HandlerEnd = handler.HandlerEnd is null ? null : instrMap[handler.HandlerEnd],
            FilterStart = handler.FilterStart is null ? null : instrMap[handler.FilterStart],
            CatchType = handler.CatchType is null ? null : _importer.Import(handler.CatchType),
        };

    private CilBody CloneBody(CilBody source, MethodDef newMethod) {
        var newBody = new CilBody { InitLocals = source.InitLocals, MaxStack = source.MaxStack };

        var localMap = new Dictionary<Local, Local>();

        foreach (var local in source.Variables) {
            var newLocal = new Local(_importer.Import(local.Type), local.Name);
            newBody.Variables.Add(newLocal);
            localMap[local] = newLocal;
        }

        var instrMap = new Dictionary<Instruction, Instruction>(source.Instructions.Count);

        foreach (var instr in source.Instructions) {
            var newInstr = new Instruction(instr.OpCode) { Operand = instr.Operand };
            newBody.Instructions.Add(newInstr);
            instrMap[instr] = newInstr;
        }

        foreach (var newInstr in newBody.Instructions)
            newInstr.Operand = FixOperand(newInstr, instrMap, localMap,
                parameter => newMethod.Parameters[parameter.Index]);

        foreach (var handler in source.ExceptionHandlers)
            newBody.ExceptionHandlers.Add(RemapExceptionHandler(handler, instrMap));

        return newBody;
    }

    private object? FixOperand(Instruction instr, Dictionary<Instruction, Instruction> instrMap,
        Dictionary<Local, Local> localMap, Func<Parameter, object?> resolveParameter) {
        var operand = instr.Operand;

        return instr.OpCode.OperandType switch {
            OperandType.InlineMethod => _importer.Import(operand as IMethod),
            OperandType.InlineField => _importer.Import(operand as IField),
            OperandType.InlineType => _importer.Import(operand as ITypeDefOrRef),

            OperandType.InlineTok => operand switch {
                ITypeDefOrRef type => _importer.Import(type),
                IField field => _importer.Import(field),
                IMethod method => _importer.Import(method),
                _ => operand,
            },

            OperandType.InlineSig => _importer.Import(operand as MethodSig),
            OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget => instrMap[(Instruction)operand],
            OperandType.InlineSwitch => (operand as IList<Instruction> ??
                                         throw new InvalidCastException(
                                             "operand is not IList<Instruction>, but it's an inline switch, what the fuck?"))
                .Select(target => instrMap[target])
                .ToList(),
            OperandType.InlineVar or OperandType.ShortInlineVar => operand is Local local
                ? localMap[local]
                : resolveParameter((Parameter)operand),

            _ => operand
        };
    }

    internal MethodDef BuildStringDecryptor(TypeDef owner, IReadOnlyList<(MethodDef Method, int? Seed)> inversePasses) {
        var alreadyInjected = new HashSet<IMemberDef>(_map.Values);

        var sig = MethodSig.CreateStatic(_target.CorLibTypes.String, _target.CorLibTypes.String);
        var decryptFunction = new MethodDefUser("Decrypt", sig,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Public | MethodAttributes.Static);

        owner.Methods.Add(decryptFunction);

        var body = new CilBody();
        var buffer = new Local(new SZArraySig(_target.CorLibTypes.Byte));
        body.Variables.Add(buffer);

        var fromBase64 = _importer.Import(
            typeof(Convert).GetMethod(nameof(Convert.FromBase64String), [typeof(string)])!);
        var utf8 = _importer.Import(typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetGetMethod()!);
        var getString = _importer.Import(
            typeof(Encoding).GetMethod(nameof(Encoding.GetString), [typeof(byte[])])!);

        body.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
        body.Instructions.Add(OpCodes.Call.ToInstruction(fromBase64));
        body.Instructions.Add(OpCodes.Stloc.ToInstruction(buffer));

        // magical splice, we remap everything inline to make it utterly fucking disgusting to reverse.
        foreach (var (passMethod, seed) in inversePasses) {
            var source = passMethod.Body;
            var instrMap = new Dictionary<Instruction, Instruction>(source.Instructions.Count);
            var cloned = new List<Instruction>(source.Instructions.Count);
            foreach (var instr in source.Instructions) {
                var newInstr = new Instruction(instr.OpCode) { Operand = instr.Operand };
                cloned.Add(newInstr);
                instrMap[instr] = newInstr;
            }

            new CilBody(source.InitLocals, cloned, new List<ExceptionHandler>(), new List<Local>(source.Variables))
                .SimplifyMacros(passMethod.Parameters);
            var localMap = new Dictionary<Local, Local>();
            foreach (var local in source.Variables) {
                var newLocal = new Local(_importer.Import(local.Type), local.Name);
                body.Variables.Add(newLocal);
                localMap[local] = newLocal;
            }

            var end = OpCodes.Nop.ToInstruction();
            foreach (var newInstr in cloned) {
                if (IsLoadArg(newInstr, 0)) {
                    newInstr.OpCode = OpCodes.Ldloc;
                    newInstr.Operand = buffer;
                }
                else if (seed is { } key && IsLoadArg(newInstr, 1)) {
                    newInstr.OpCode = OpCodes.Ldc_I4;
                    newInstr.Operand = key;
                }
                else if (newInstr.OpCode == OpCodes.Ret) {
                    newInstr.OpCode = OpCodes.Br;
                    newInstr.Operand = end;
                }
                else {
                    newInstr.Operand = FixOperand(newInstr, instrMap, localMap,
                        _ => throw new NotSupportedException(
                            "string encryption pass(es) may only reference its (their) byte[] parameter"));
                }
            }

            body.InitLocals |= source.InitLocals;

            foreach (var newInstr in cloned)
                body.Instructions.Add(newInstr);

            body.Instructions.Add(end);
            foreach (var handler in source.ExceptionHandlers)
                body.ExceptionHandlers.Add(RemapExceptionHandler(handler, instrMap));
        }

        body.Instructions.Add(OpCodes.Call.ToInstruction(utf8));
        body.Instructions.Add(OpCodes.Ldloc.ToInstruction(buffer));
        body.Instructions.Add(OpCodes.Callvirt.ToInstruction(getString));
        body.Instructions.Add(OpCodes.Ret.ToInstruction());

        body.OptimizeMacros();
        body.OptimizeBranches();
        decryptFunction.Body = body;

        Drain();

        PromoteForSplice(
            alreadyInjected); // passes need to mingle with the string decrypt for extra fun, so we need shit public.
        return decryptFunction;
    }

    private static bool IsLoadArg(Instruction instr, int index) =>
        (index == 0 && instr.OpCode == OpCodes.Ldarg_0) ||
        ((instr.OpCode == OpCodes.Ldarg || instr.OpCode == OpCodes.Ldarg_S)
         && instr.Operand is Parameter p && p.Index == index);

    internal void PrependDead(MethodDef target, MethodDef decoy, int copies) {
        var body = target.Body;
        if (body.Instructions.Count == 0)
            return;

        var originalFirst = body.Instructions[0];
        var alreadyInjected = new HashSet<IMemberDef>(_map.Values);

        var blocks = new List<List<Instruction>>(copies);
        for (var k = 0; k < copies; k++)
            blocks.Add(CloneDeadBody(decoy, body));

        var prepend = new List<Instruction>();
        for (var k = 0; k < copies; k++) {
            var next = k + 1 < copies ? blocks[k + 1][0] : originalFirst;
            foreach (var instr in blocks[k])
                if (instr.OpCode == OpCodes.Ret) {
                    instr.OpCode = OpCodes.Br;
                    instr.Operand = next;
                }

            prepend.AddRange(blocks[k]);
        }

        for (var p = 0; p < prepend.Count; p++)
            body.Instructions.Insert(p, prepend[p]);

        Drain();
        PromoteForSplice(alreadyInjected);
    }

    private void PromoteForSplice(HashSet<IMemberDef> alreadyInjected) {
        foreach (var def in _map.Values.Where(def => !alreadyInjected.Contains(def))) {
            switch (def) {
                case TypeDef type:
                    type.Attributes = (type.Attributes & ~TypeAttributes.VisibilityMask)
                                      | (type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public);
                    break;
                case FieldDef field:
                    field.Attributes = (field.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
                    break;
                case MethodDef method:
                    method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask) |
                                        MethodAttributes.Public;
                    break;
            }
        }
    }

    private List<Instruction> CloneDeadBody(MethodDef source, CilBody dest) {
        var sourceBody = source.Body;
        var instrMap = new Dictionary<Instruction, Instruction>(sourceBody.Instructions.Count);
        var cloned = new List<Instruction>(sourceBody.Instructions.Count);
        foreach (var instr in sourceBody.Instructions) {
            var newInstr = new Instruction(instr.OpCode) { Operand = instr.Operand };
            cloned.Add(newInstr);
            instrMap[instr] = newInstr;
        }

        new CilBody(sourceBody.InitLocals, cloned, new List<ExceptionHandler>(),
                new List<Local>(sourceBody.Variables))
            .SimplifyMacros(source.Parameters);

        var localMap = new Dictionary<Local, Local>();
        foreach (var local in sourceBody.Variables) {
            var newLocal = new Local(_importer.Import(local.Type), local.Name);
            dest.Variables.Add(newLocal);
            localMap[local] = newLocal;
        }

        foreach (var newInstr in cloned.Where(newInstr => newInstr.OpCode != OpCodes.Ret)) {
            newInstr.Operand = FixOperand(newInstr, instrMap, localMap,
                _ => throw new NotSupportedException("decoy must be parameterless"));
        }

        dest.InitLocals |= sourceBody.InitLocals;
        foreach (var handler in sourceBody.ExceptionHandlers)
            dest.ExceptionHandlers.Add(RemapExceptionHandler(handler, instrMap));
        return cloned;
    }

    private void CopyGenericParams(ITypeOrMethodDef source, ITypeOrMethodDef dest) {
        foreach (var genericParam in source.GenericParameters) {
            var newGenericParam = new GenericParamUser(genericParam.Number, genericParam.Flags, genericParam.Name);
            foreach (var constraint in genericParam.GenericParamConstraints)
                newGenericParam.GenericParamConstraints.Add(
                    new GenericParamConstraintUser(_importer.Import(constraint.Constraint)));
            CopyCustomAttributes(genericParam, newGenericParam);
            dest.GenericParameters.Add(newGenericParam);
        }
    }

    private void CopyCustomAttributes(IHasCustomAttribute source, IHasCustomAttribute dest) {
        foreach (var attribute in source.CustomAttributes) {
            var newAttribute = new CustomAttribute((ICustomAttributeType)_importer.Import(attribute.Constructor));
            foreach (var arg in attribute.ConstructorArguments)
                newAttribute.ConstructorArguments.Add(CloneCaArgument(arg));
            foreach (var namedArg in attribute.NamedArguments)
                newAttribute.NamedArguments.Add(new CANamedArgument(namedArg.IsField,
                    _importer.Import(namedArg.Type), namedArg.Name, CloneCaArgument(namedArg.Argument)));
            dest.CustomAttributes.Add(newAttribute);
        }
    }

    private CAArgument CloneCaArgument(CAArgument argument) => new(_importer.Import(argument.Type),
        argument.Value switch {
            TypeSig type => _importer.Import(type),
            CAArgument inner => CloneCaArgument(inner),
            IList<CAArgument> elements => elements.Select(CloneCaArgument).ToList(),
            _ => argument.Value,
        });

    private ModuleRef GetModuleRef(ModuleRef source) {
        if (_moduleRefs.TryGetValue(source.Name, out var existing))
            return existing;
        var newModuleRef = new ModuleRefUser(_target, source.Name);
        _moduleRefs[source.Name] = newModuleRef;
        return newModuleRef;
    }

    private sealed class Mapper(InjectContext context) : ImportMapper {
        public override ITypeDefOrRef? Map(ITypeDefOrRef source) =>
            source is TypeDef type && context.IsSource(type) ? context.CloneType(type) : null;

        public override IMethod? Map(MethodDef source) =>
            context.IsSource(source) ? context.CloneMethod(source) : null;

        public override IField? Map(FieldDef source) =>
            context.IsSource(source) ? context.CloneField(source) : null;
    }
}