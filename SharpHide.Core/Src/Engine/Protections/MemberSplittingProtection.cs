using dnlib.DotNet;

namespace SharpHide.Core.Engine.Protections;

public sealed class MemberSplittingProtection : Protection {
    public override long Priority => 88;
    public override string Name => "member-splitting";
    public override string Description => "Relocates static methods into nested fragment types";

    public override IReadOnlyDictionary<string, double> DefaultOptions => new Dictionary<string, double> {
        ["rate"] = 0.5,
    };

    public override IReadOnlyDictionary<string, string> OptionHelp => new Dictionary<string, string> {
        ["rate"] = "Fraction 0-1 of eligible members relocated into other injected types (1 = all).",
    };

    public override void Populate(ProtectionPipeline pipeline) =>
        pipeline.Insert(ProtectionPipeline.PipelineStage.PreProcess, this);

    public override void Initialize(ObfuscationContext context) {
        var entry = context.Module.EntryPoint;
        int moved = 0, fragments = 0;

        foreach (var type in context.OriginalTypes.ToArray()) {
            if (type.IsGlobalModuleType || type.IsInterface || type.HasGenericParameters)
                continue;

            var candidates = type.Methods.Where(method => IsMovable(method, entry)).ToArray();
            if (candidates.Length < 2)
                continue;

            var fragment = new TypeDefUser(string.Empty, context.NewName(),
                context.Module.CorLibTypes.Object.TypeDefOrRef) {
                Attributes = TypeAttributes.NestedAssembly | TypeAttributes.Class
                             | TypeAttributes.Abstract | TypeAttributes.Sealed,
            };
            type.NestedTypes.Add(fragment);
            fragments++;

            foreach (var method in candidates) {
                if (!context.Chance(Option(context, "rate")))
                    continue;
                type.Methods.Remove(method);
                method.Attributes = (method.Attributes & ~MethodAttributes.MemberAccessMask)
                                    | MethodAttributes.Assembly;
                fragment.Methods.Add(method);
                moved++;
            }
        }

        context.Log($"    {moved} methods into {fragments} fragments");
    }

    private static bool IsMovable(MethodDef method, MethodDef? entry) =>
        method.IsStatic && method != entry && method.HasBody
        && !method.IsConstructor && !method.IsStaticConstructor
        && !method.IsSpecialName && !method.IsRuntimeSpecialName
        && (method.IsAssembly || method.IsPrivate);
}
