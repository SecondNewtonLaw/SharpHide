namespace SharpHide.Core.Engine.Protections;

public sealed class UserRenameProtection : Protection {
    public override long Priority => 60;
    public override string Name => "user-rename";
    public override string Description => "Renames the user's own private types and members";

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PostProcess, this);

    public override void Initialize(ObfuscationContext context) {
        if (context.Settings.KeepNames) {
            context.Log("    skipped (keep-names)");
            return;
        }

        context.CaptureRenameReferences();

        var entry = context.Module.EntryPoint;
        var protectedNames = context.XamlProtectedNames;
        int types = 0, members = 0;

        foreach (var type in context.OriginalTypes) {
            if (type.IsGlobalModuleType)
                continue;

            var originalTypeName = type.FullName;

            var internalType = !type.IsPublic && !type.IsNestedPublic;
            if (internalType && !protectedNames.Contains(type.Name)) {
                type.Namespace = string.Empty;
                type.Name = context.NewExoticName();
                context.RenameMap[type.Name] = originalTypeName;
                types++;
            }

            foreach (var method in type.Methods) {
                if (internalType || !(method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly))
                    foreach (var parameter in method.ParamDefs)
                        parameter.Name = context.NewExoticName();

                if (method == entry || method.IsPublic || method.IsConstructor || method.IsStaticConstructor
                    || method.IsSpecialName || method.IsRuntimeSpecialName
                    || method.IsVirtual || method.IsAbstract || method.HasOverrides
                    || protectedNames.Contains(method.Name))
                    continue;
                var originalMethodName = method.Name;
                method.Name = context.NewExoticName();
                context.RenameMap[method.Name] = originalTypeName + "." + originalMethodName;
                members++;
            }

            foreach (var field in type.Fields) {
                if (field.IsPublic || protectedNames.Contains(field.Name))
                    continue;
                field.Name = context.NewExoticName();
                members++;
            }
        }

        context.ApplyRenameReferences();

        context.Log($"    {types} types, {members} members renamed");
    }
}
