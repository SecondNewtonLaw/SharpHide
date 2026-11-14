using dnlib.DotNet;
using SharpHide.Core.Engine.Protections;

namespace SharpHide.Tests;

public class CallHidingTests {
    private static ModuleDef NewModule() {
        var name = "Boxing_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) {
            Kind = ModuleKind.Dll,
            RuntimeVersion = "v4.0.30319",
        };
        new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);
        return module;
    }

    private static TypeDef AddValueType(ModuleDef module, string name, bool byRefLike) {
        var type = new TypeDefUser(name, module.CorLibTypes.GetTypeRef("System", "ValueType")) {
            Attributes = dnlib.DotNet.TypeAttributes.Public | dnlib.DotNet.TypeAttributes.Sealed
                         | dnlib.DotNet.TypeAttributes.SequentialLayout,
        };
        if (byRefLike) {
            var attributeType = new TypeRefUser(module, "System.Runtime.CompilerServices",
                "IsByRefLikeAttribute", module.CorLibTypes.AssemblyRef);
            var constructor = new MemberRefUser(module, ".ctor",
                MethodSig.CreateInstance(module.CorLibTypes.Void), attributeType);
            type.CustomAttributes.Add(new CustomAttribute(constructor));
        }
        module.Types.Add(type);
        return type;
    }

    [Fact]
    public void Rejects_ByRefLike_Struct() {
        var module = NewModule();
        var refStruct = AddValueType(module, "MyRefStruct", byRefLike: true);
        Assert.True(CallHidingProtection.IsUnsupported(refStruct.ToTypeSig()));
    }

    [Fact]
    public void Allows_Normal_ValueType() {
        var module = NewModule();
        var plain = AddValueType(module, "PlainStruct", byRefLike: false);
        Assert.False(CallHidingProtection.IsUnsupported(plain.ToTypeSig()));
        Assert.False(CallHidingProtection.IsUnsupported(module.CorLibTypes.Int32));
        Assert.False(CallHidingProtection.IsUnsupported(module.CorLibTypes.String));
    }

    [Fact]
    public void Rejects_ByRef_Pointer_And_TypedReference() {
        var module = NewModule();
        Assert.True(CallHidingProtection.IsUnsupported(new ByRefSig(module.CorLibTypes.Int32)));
        Assert.True(CallHidingProtection.IsUnsupported(new PtrSig(module.CorLibTypes.Int32)));
        Assert.True(CallHidingProtection.IsUnsupported(module.CorLibTypes.TypedReference));
    }

    [Fact]
    public void Rejects_Open_Generic_Parameter() {
        Assert.True(CallHidingProtection.IsUnsupported(new GenericMVar(0)));
        Assert.True(CallHidingProtection.IsUnsupported(new GenericVar(0)));
    }
}
