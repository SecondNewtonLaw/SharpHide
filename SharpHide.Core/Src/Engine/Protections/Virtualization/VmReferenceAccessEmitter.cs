using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core.Injection;
using SharpHide.Vm;

namespace SharpHide.Core.Engine.Protections;

internal sealed class VmReferenceAccessDescriptor(TypeSig type, bool native) {
    public readonly TypeSig Type = type;
    public TypeSig StorageType = type;
    public readonly bool Native = native;
    public MethodDef Cell = null!;
    public MethodDef Copy = null!;
    public MethodDef? NativeCopy;
}

internal static class VmReferenceAccessEmitter {
    public static VmReferenceAccessDescriptor Create(ModuleDef module, MethodDef source, InjectResult runtime, TypeSig type,
        bool native) {
        var descriptor = new VmReferenceAccessDescriptor(type, native);
        if (type.ElementType is ElementType.Ptr or ElementType.FnPtr)
            descriptor.StorageType = module.CorLibTypes.IntPtr;
        descriptor.Cell = CreateMethod(source, "$vmcell", descriptor.Native
            ? MethodSig.CreateStatic(module.CorLibTypes.IntPtr, module.CorLibTypes.Object, module.CorLibTypes.Int32)
            : MethodSig.CreateStatic(new ByRefSig(descriptor.StorageType), module.CorLibTypes.Object, module.CorLibTypes.Int32));
        var frame = runtime.ResolveMethod(typeof(VmRunner).GetMethod(nameof(VmRunner.CreateFrame))!)!;
        var scopeDefinition = frame.MethodSig.Params[^1].RemovePinnedAndModifiers().ToTypeDefOrRef();
        var referenceType = scopeDefinition.DeclaringType!.ResolveTypeDef()!;
        descriptor.Copy = CreateMethod(source, "$vmcopy", MethodSig.CreateStatic(module.CorLibTypes.Void,
            referenceType.ToTypeSig(), module.CorLibTypes.IntPtr));
        if (descriptor.Native)
            descriptor.NativeCopy = CreateMethod(source, "$vmnativecopy", MethodSig.CreateStatic(module.CorLibTypes.Void,
                module.CorLibTypes.IntPtr, module.CorLibTypes.IntPtr));
        EmitCell(module, descriptor.Cell, type, descriptor.Native);
        EmitCopy(module, descriptor.Copy, referenceType, runtime, type, descriptor.StorageType);
        if (descriptor.NativeCopy is not null)
            EmitNativeCopy(descriptor.NativeCopy, type);
        return descriptor;
    }

    private static MethodDef CreateMethod(MethodDef source, string suffix, MethodSig signature) {
        var method = new MethodDefUser(UniqueName(source, suffix), signature,
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName) {
            Body = new CilBody { InitLocals = true },
        };
        VmTypeLayout.CopyGenericParameters(source, method);
        source.DeclaringType.Methods.Add(method);
        return method;
    }

    private static string UniqueName(MethodDef source, string suffix) {
        var prefix = source.Name + suffix;
        var name = prefix;
        for (var number = 2; source.DeclaringType.Methods.Any(candidate => candidate.Name == name); number++)
            name = $"{prefix}{number}";
        return name;
    }

    private static void EmitCell(ModuleDef module, MethodDef method, TypeSig type, bool native) {
        var il = method.Body.Instructions;
        il.Add(OpCodes.Ldarg_0.ToInstruction());
        il.Add(OpCodes.Unbox_Any.ToInstruction(module.CorLibTypes.IntPtr.TypeDefOrRef));
        il.Add(OpCodes.Ldobj.ToInstruction(module.Import(typeof(TypedReference))));
        il.Add(OpCodes.Refanyval.ToInstruction(type.ToTypeDefOrRef()));
        if (native)
            il.Add(OpCodes.Conv_I.ToInstruction());
        il.Add(OpCodes.Ret.ToInstruction());
    }

    private static void EmitCopy(ModuleDef module, MethodDef method, TypeDef referenceType, InjectResult runtime, TypeSig type, TypeSig storageType) {
        var il = method.Body.Instructions;
        il.Add(OpCodes.Ldarg_1.ToInstruction());
        il.Add(OpCodes.Ldarg_0.ToInstruction());
        if (type.RemovePinnedAndModifiers().ElementType == ElementType.TypedByRef) {
            var getBytes = referenceType.Methods.Single(candidate => candidate.Name == nameof(VmReference.GetBytes));
            il.Add(OpCodes.Callvirt.ToInstruction(getBytes));
        }
        else {
            var getAddress = referenceType.Methods.Single(candidate => candidate.Name == nameof(VmReference.GetAddress));
            il.Add(OpCodes.Callvirt.ToInstruction(new MethodSpecUser(getAddress, new GenericInstMethodSig(storageType))));
        }
        il.Add(OpCodes.Mkrefany.ToInstruction(type.ToTypeDefOrRef()));
        il.Add(OpCodes.Stobj.ToInstruction(module.Import(typeof(TypedReference))));
        il.Add(OpCodes.Ret.ToInstruction());
    }

    private static void EmitNativeCopy(MethodDef method, TypeSig type) {
        var il = method.Body.Instructions;
        il.Add(OpCodes.Ldarg_1.ToInstruction());
        il.Add(OpCodes.Ldarg_0.ToInstruction());
        il.Add(OpCodes.Cpobj.ToInstruction(type.ToTypeDefOrRef()));
        il.Add(OpCodes.Ret.ToInstruction());
    }
}
