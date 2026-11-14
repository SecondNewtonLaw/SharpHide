using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Engine.Protections;

internal sealed class VmBridgeDescriptor(
    Instruction instruction,
    TypeSig[] inputs,
    TypeSig result,
    TypeSig? constraint,
    int flags,
    bool[] nativeInputs,
    bool nativeResult) {
    public readonly Instruction Instruction = instruction;
    public readonly TypeSig[] Inputs = inputs;
    public readonly TypeSig Result = result;
    public readonly TypeSig? Constraint = constraint;
    public readonly int Flags = flags;
    public readonly bool[] NativeInputs = nativeInputs;
    public readonly bool NativeResult = nativeResult;
    public MethodDef? Method;
}

internal static class VmTypeLayout {
    private const int AllowByRefLike = 32;

    public static bool RequiresNative(MethodDef source, TypeSig signature) {
        var type = signature.RemovePinnedAndModifiers();

        switch (type) {
            case GenericMVar methodVariable:
                return methodVariable.Number < source.GenericParameters.Count &&
                       ((int)source.GenericParameters[(int)methodVariable.Number].Flags & AllowByRefLike) != 0;
            case GenericVar typeVariable:
                return typeVariable.Number < source.DeclaringType.GenericParameters.Count &&
                       ((int)source.DeclaringType.GenericParameters[(int)typeVariable.Number].Flags & AllowByRefLike) !=
                       0;
            case GenericInstSig instance:
                type = instance.GenericType;
                break;
        }

        if (type is not ClassOrValueTypeSig named)
            return type.ElementType == ElementType.TypedByRef;
        var definition = named.TypeDefOrRef.ResolveTypeDef();
        if (definition?.CustomAttributes.Any(attribute =>
                attribute.TypeFullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute") == true)
            return true;
        return (Type.GetType(named.AssemblyQualifiedName, false) ??
                typeof(object).Assembly.GetType(named.FullName))?.IsByRefLike == true;
    }

    public static bool IsValueType(ITypeDefOrRef type) {
        if (type.ToTypeSig().IsValueType || type.ResolveTypeDef()?.IsValueType == true)
            return true;
        return (Type.GetType(type.AssemblyQualifiedName, false) ??
                typeof(object).Assembly.GetType(type.FullName))?.IsValueType == true;
    }

    public static void CopyGenericParameters(MethodDef source, MethodDef target) {
        foreach (var parameter in source.GenericParameters) {
            var copy = new GenericParamUser(parameter.Number, parameter.Flags, parameter.Name);
            foreach (var constraint in parameter.GenericParamConstraints)
                copy.GenericParamConstraints.Add(new GenericParamConstraintUser(constraint.Constraint));
            target.GenericParameters.Add(copy);
        }

        if (target.GenericParameters.Count == 0) return;
        target.MethodSig.Generic = true;
        target.MethodSig.GenParamCount = (uint)target.GenericParameters.Count;
    }

    public static int Bank(MethodDef source, TypeSig signature, IReadOnlyList<TypeSig> nativeTypes) {
        if (!RequiresNative(source, signature))
            return -1;
        var comparer = new SigComparer();
        var requested = signature.RemovePinnedAndModifiers();
        for (var index = 0; index < nativeTypes.Count; index++)
            if (Equivalent(nativeTypes[index], requested, comparer))
                return index;
        return -1;
    }

    private static bool Equivalent(TypeSig existing, TypeSig requested, SigComparer comparer) {
        existing = existing.RemovePinnedAndModifiers();
        if (comparer.Equals(existing, requested))
            return true;
        if (existing is GenericInstSig || requested is GenericInstSig)
            return false;
        return existing is ClassOrValueTypeSig existingNamed && requested is ClassOrValueTypeSig requestedNamed &&
               existingNamed.TypeDefOrRef.FullName == requestedNamed.TypeDefOrRef.FullName;
    }
}