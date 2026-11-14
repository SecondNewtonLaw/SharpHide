using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpHide.Core.Injection;

public static class InjectionExtensions {
    private static readonly ConcurrentDictionary<Module, ModuleDefMD> Modules = new();
    private static readonly ConditionalWeakTable<ModuleDef, InjectContext> Contexts = new();

    private static ModuleDefMD SourceOf(MemberInfo m) => Modules.GetOrAdd(m.Module, ModuleDefMD.Load);

    internal static TypeDef ResolveType(Type type) =>
        (TypeDef)SourceOf(type).ResolveToken((uint)type.MetadataToken, default);

    internal static MethodDef ResolveMethod(MethodBase method) =>
        (MethodDef)SourceOf(method).ResolveToken((uint)method.MetadataToken, default);

    internal static FieldDef ResolveField(FieldInfo field) =>
        (FieldDef)SourceOf(field).ResolveToken((uint)field.MetadataToken, default);

    private static InjectContext Context(ModuleDef target, bool forceClone) =>
        forceClone ? new InjectContext(target) : Contexts.GetValue(target, m => new InjectContext(m));

    internal static InjectContext ContextFor(ModuleDef target) => Context(target, false);

    extension(ModuleDef self) {
        public InjectResult Inject(Type type, bool forceClone = false) {
            var ctx = Context(self, forceClone);
            var entry = ctx.CloneType(ResolveType(type));
            ctx.Drain();
            return new InjectResult(ctx, entry);
        }

        public InjectResult Inject(MethodBase method, TypeDef? into = null) {
            var ctx = Context(self, false);
            var entry = ctx.CloneMethod(ResolveMethod(method), into ?? self.GlobalType);
            ctx.Drain();
            return new InjectResult(ctx, entry);
        }

        public InjectResult Inject(FieldInfo field, TypeDef? into = null) {
            var ctx = Context(self, false);
            var entry = ctx.CloneField(ResolveField(field), into ?? self.GlobalType);
            ctx.Drain();
            return new InjectResult(ctx, entry);
        }

        public EmbeddedResource InjectResource(string name, byte[] data,
            dnlib.DotNet.ManifestResourceAttributes flags = dnlib.DotNet.ManifestResourceAttributes.Public) {
            var res = new EmbeddedResource(name, data, flags);
            self.Resources.Add(res);
            return res;
        }

        public MethodDef InjectModuleInitializer(MethodBase initializer) {
            var entry = self.Inject(initializer.DeclaringType!).ResolveMethod(initializer)!;
            var cctor = self.GlobalType.FindOrCreateStaticConstructor();
            cctor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction((IMethod)entry));
            return entry;
        }
    }
}
