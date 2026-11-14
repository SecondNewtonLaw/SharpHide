using dnlib.DotNet;

namespace SharpHide.Core.Engine.Protections;

internal static class VmSignatureResolver {
    public static TypeSig Resolve(TypeSig type, ITypeDefOrRef declaringType, MethodSpec? method = null) {
        var typeArguments = declaringType.ToTypeSig() is GenericInstSig instance
            ? instance.GenericArguments
            : null;
        return Substitute(type, typeArguments, method?.GenericInstMethodSig?.GenericArguments);
    }

    public static TypeSig Substitute(TypeSig type, IList<TypeSig>? typeArguments, IList<TypeSig>? methodArguments) {
        MethodSig SubstituteMethod(MethodSig signature) {
            var clone = signature.Clone();
            clone.RetType = Visit(signature.RetType);
            for (var index = 0; index < signature.Params.Count; index++)
                clone.Params[index] = Visit(signature.Params[index]);
            if (signature.ParamsAfterSentinel is not null)
                clone.ParamsAfterSentinel = signature.ParamsAfterSentinel.Select(Visit).ToArray();
            return clone;
        }

        return Visit(type);

        TypeSig Visit(TypeSig signature) {
            while (true) {
                switch (signature) {
                    case GenericVar typeVariable
                        when typeArguments is not null && typeVariable.Number < typeArguments.Count: {
                        var replacement = typeArguments[(int)typeVariable.Number];
                        if (replacement is GenericVar variable && variable.Number == typeVariable.Number)
                            return signature;
                        signature = replacement;
                        continue;
                    }
                    case GenericMVar methodVariable
                        when methodArguments is not null && methodVariable.Number < methodArguments.Count: {
                        var replacement = methodArguments[(int)methodVariable.Number];
                        if (replacement is GenericMVar variable && variable.Number == methodVariable.Number)
                            return signature;
                        signature = replacement;
                        continue;
                    }
                    default:
                        return signature switch {
                            GenericInstSig instance => new GenericInstSig(instance.GenericType,
                                instance.GenericArguments.Select(Visit).ToArray()),
                            ByRefSig reference => new ByRefSig(Visit(reference.Next)),
                            PtrSig pointer => new PtrSig(Visit(pointer.Next)),
                            SZArraySig array => new SZArraySig(Visit(array.Next)),
                            ArraySig array => new ArraySig(Visit(array.Next), array.Rank, array.Sizes,
                                array.LowerBounds),
                            CModReqdSig modifier => new CModReqdSig(modifier.Modifier, Visit(modifier.Next)),
                            CModOptSig modifier => new CModOptSig(modifier.Modifier, Visit(modifier.Next)),
                            PinnedSig pinned => new PinnedSig(Visit(pinned.Next)),
                            FnPtrSig pointer => new FnPtrSig(SubstituteMethod(pointer.MethodSig)),
                            _ => signature,
                        };
                }
            }
        }
    }
}