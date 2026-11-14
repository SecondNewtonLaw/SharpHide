using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpHide.Core;
using SharpHide.Core.Engine.Protections;

namespace SharpHide.Tests;

public class RenameMapTests {
    [Fact]
    public void User_Rename_Records_A_Support_Map() {
        var name = "Map_" + Guid.NewGuid().ToString("N");
        var module = new ModuleDefUser(name + ".dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser(name, new Version(1, 0, 0, 0)).Modules.Add(module);

        var type = new TypeDefUser("Ns", "Secret", module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = TypeAttributes.Class,
        };
        module.Types.Add(type);

        var method = new MethodDefUser("Hidden", MethodSig.CreateStatic(module.CorLibTypes.Void),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static) { Body = new CilBody() };
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(method);

        var context = new ObfuscationContext(module, new Random(1));
        new UserRenameProtection().Initialize(context);

        Assert.NotEqual("Secret", type.Name.String);
        Assert.Equal("Ns.Secret", context.RenameMap[type.Name.String]);
        Assert.Equal("Ns.Secret.Hidden", context.RenameMap[method.Name.String]);
    }

    [Fact]
    public void User_Rename_Updates_Generic_Field_References_Without_Injected_Rename() {
        var module = new ModuleDefUser("generic.dll", Guid.NewGuid()) { Kind = ModuleKind.Dll };
        new AssemblyDefUser("generic", new Version(1, 0, 0, 0)).Modules.Add(module);

        var type = new TypeDefUser("Ns", "Pair", module.CorLibTypes.Object.TypeDefOrRef) {
            Attributes = TypeAttributes.Class,
        };
        type.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
        module.Types.Add(type);

        var field = new FieldDefUser("Value", new FieldSig(new GenericVar(0)), FieldAttributes.Private);
        type.Fields.Add(field);

        var instance = new GenericInstSig(new ClassSig(type), module.CorLibTypes.Int32);
        var fieldReference = new MemberRefUser(module, field.Name, field.FieldSig, new TypeSpecUser(instance));
        var method = new MethodDefUser("Read", MethodSig.CreateStatic(module.CorLibTypes.Void),
            MethodImplAttributes.IL | MethodImplAttributes.Managed,
            MethodAttributes.Private | MethodAttributes.Static) { Body = new CilBody() };
        method.Body.Instructions.Add(OpCodes.Ldfld.ToInstruction(fieldReference));
        method.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
        type.Methods.Add(method);

        new UserRenameProtection().Initialize(new ObfuscationContext(module, new Random(1)));

        Assert.NotEqual("Value", field.Name.String);
        Assert.Equal(field.Name, fieldReference.Name);
    }
}
