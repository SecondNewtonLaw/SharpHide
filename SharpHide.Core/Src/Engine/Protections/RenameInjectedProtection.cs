using System.Runtime.CompilerServices;
using dnlib.DotNet;

namespace SharpHide.Core.Engine.Protections;

// Renames injected members and marks injected types as compiler-generated.
public sealed class RenameInjectedProtection : Protection {
    public override long Priority => 30;
    public override string Name => "rename-injected";
    public override string Description => "Randomizes the names of every injected type and member";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        if (context.Settings.KeepNames) {
            context.Log("    skipped (keep-names)");
            return;
        }

        var compilerGenerated = (ICustomAttributeType)new Importer(context.Module).Import(
            typeof(CompilerGeneratedAttribute).GetConstructor(Type.EmptyTypes)!);
        var compilerGeneratedName = typeof(CompilerGeneratedAttribute).FullName!;

        var count = 0;
        foreach (var type in context.Module.GetTypes()) {
            if (!context.IsInjected(type))
                continue;

            type.Namespace = string.Empty;
            type.Name = context.NewExoticName();
            if (!type.CustomAttributes.IsDefined(compilerGeneratedName))
                type.CustomAttributes.Add(new CustomAttribute(compilerGenerated));
            count++;

            foreach (var method in type.Methods) {
                foreach (var parameter in method.ParamDefs)
                    parameter.Name = context.NewExoticName();
                if (method.IsConstructor || method.IsStaticConstructor || method.IsSpecialName
                    || method.IsRuntimeSpecialName || method.IsVirtual || method.HasOverrides)
                    continue;
                method.Name = context.NewExoticName();
            }

            foreach (var field in type.Fields)
                if (!field.IsSpecialName && !field.IsRuntimeSpecialName)
                    field.Name = context.NewExoticName();
        }

        context.ApplyRenameReferences();

        context.Log($"    {count} injected types renamed");
    }
}
