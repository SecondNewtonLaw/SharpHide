using System.Reflection;
using dnlib.DotNet;

namespace SharpHide.Core.Injection;

public sealed class InjectResult {
    private readonly InjectContext _ctx;

    internal InjectResult(InjectContext ctx, IMemberDef entry) {
        _ctx = ctx;
        Entry = entry;
    }

    private IMemberDef Entry { get; }
    public TypeDef? EntryType => Entry as TypeDef;
    public MethodDef? EntryMethod => Entry as MethodDef;
    public FieldDef? EntryField => Entry as FieldDef;

    public TypeDef? ResolveType(Type type) => _ctx.Lookup(InjectionExtensions.ResolveType(type)) as TypeDef;

    public MethodDef? ResolveMethod(MethodBase method) =>
        _ctx.Lookup(InjectionExtensions.ResolveMethod(method)) as MethodDef;

    public MethodDef?[] MassResolveMethod(params MethodBase[] method) {
        var methods = new MethodDef?[method.Length];

        for (var i = 0; i < method.Length; i++)
            methods[i] = _ctx.Lookup(InjectionExtensions.ResolveMethod(method[i])) as MethodDef;

        return methods;
    }

    public FieldDef? ResolveField(FieldInfo field) => _ctx.Lookup(InjectionExtensions.ResolveField(field)) as FieldDef;

    public void Include(Type type) {
        var entry = _ctx.CloneType(InjectionExtensions.ResolveType(type));
        _ctx.Drain();
    }
}