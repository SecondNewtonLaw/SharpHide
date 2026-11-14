using System.Runtime.CompilerServices;

#pragma warning disable CS8601, CS8619

namespace SharpHide.Vm;

public delegate ref T VmAddress<T>(object? owner, int index) where T : allows ref struct;

public sealed class VmReference {
    public sealed class Scope {
        internal readonly Dictionary<Type, (Access Slot, Access Pointer)> Managed = new();
        internal readonly Dictionary<Type, Access> Cells = new();
        internal readonly Dictionary<Type, (Access Pointer, Access Cell)> Native = new();
        internal readonly Dictionary<Type, Action<VmReference, IntPtr>> Copies = new();
        internal readonly Dictionary<Type, SlotFactory> SlotFactories = new();
    }

    internal abstract class SlotFactory {
        public abstract VmReference Create(Scope scope, Type type, object? value);
    }

    private sealed class SlotFactory<T> : SlotFactory {
        public override VmReference Create(Scope scope, Type type, object? value) => SlotCore<T>(scope, type, value);
    }

    internal sealed class Access(
        Delegate address,
        VmAddress<byte> bytes,
        Func<object?, int, object?> read,
        Action<object?, int, object?> write,
        Func<object?, int, bool> validate) {
        public readonly Delegate Address = address;
        public readonly VmAddress<byte> Bytes = bytes;
        public readonly Func<object?, int, object?> Read = read;
        public readonly Action<object?, int, object?> Write = write;
        public readonly Func<object?, int, bool> Validate = validate;
    }

    private sealed class ManagedAccess<T> {
        [SpecialName]
        private readonly VmAddress<T> _address;

        public ManagedAccess(VmAddress<T> address) {
            _address = address;
        }

        [SpecialName]
        public ref byte Bytes(object? owner, int index) => ref Unsafe.As<T, byte>(ref _address(owner, index));
        [SpecialName]
        public object? Read(object? owner, int index) => _address(owner, index);
        [SpecialName]
        public void Write(object? owner, int index, object? value) => _address(owner, index) = (T)value!;
        [SpecialName]
        public bool Validate(object? owner, int index) => Unsafe.IsNullRef(ref _address(owner, index));
        [SpecialName]
        public Access Create() => new(_address, Bytes, Read, Write, Validate);
    }

    private sealed class NativeAccess(Type type, Func<object?, int, IntPtr> address, Action<IntPtr, IntPtr> copy) {
        public ref byte Bytes(object? owner, int index) => ref AddressBytes(address(owner, index));
        public object Read(object? owner, int index) => new VmNativeValue(address(owner, index), type, -1, copy);
        public void Write(object? owner, int index, object? value) => ((VmNativeValue)value!).CopyTo(address(owner, index));
        public bool Validate(object? owner, int index) => address(owner, index) == IntPtr.Zero;
        public Access Create() => new(address, Bytes, Read, Write, Validate);
    }

    private static readonly Scope DefaultScope = CreateScopeCore();
    public readonly object? Owner;
    public readonly int Index;
    public readonly Type ElementType;
    public readonly bool IsNull;
    public readonly bool IsCell;
    public readonly int NativeBank;
    public bool IsPinned;
    public IntPtr PinnedAddress;
    public readonly Delegate Address;
    private readonly VmAddress<byte> _bytes;
    private readonly Func<object?, int, object?> _read;
    private readonly Action<object?, int, object?> _write;
    private readonly Action<VmReference, IntPtr>? _copy;
    private readonly Scope _scope;

    private static Scope CreateScopeCore() {
        var scope = new Scope();
        Register<int>(scope, typeof(int), null, null);
        Register<VmReference>(scope, typeof(VmReference), null, null);
        Register<IntPtr>(scope, typeof(IntPtr), null, null);
        return scope;
    }

    public static Scope CreateScope() => CreateScopeCore();

    private VmReference(object? owner, int index, Type type, Access access, Scope scope,
        Action<VmReference, IntPtr>? copy = null, bool cell = false, int nativeBank = -1) {
        Owner = owner;
        Index = index;
        ElementType = type;
        IsCell = cell;
        NativeBank = nativeBank;
        Address = access.Address;
        _bytes = access.Bytes;
        _read = access.Read;
        _write = access.Write;
        _copy = copy;
        _scope = scope;
        IsNull = access.Validate(owner, index);
    }

    public static void Register<T>(Type type, VmAddress<T>? cell, Action<VmReference, IntPtr>? copy) {
        Register(DefaultScope, type, cell, copy);
    }

    public static void Register<T>(Scope scope, Type type, VmAddress<T>? cell, Action<VmReference, IntPtr>? copy) {
        var slot = new ManagedAccess<T>(new VmAddress<T>(SlotAddress<T>)).Create();
        var pointer = new ManagedAccess<T>(new VmAddress<T>(PointerAddress<T>)).Create();
        var cellAccess = cell is null ? null : new ManagedAccess<T>(cell).Create();
        lock (scope) {
            scope.Managed[typeof(T)] = (slot, pointer);
            scope.SlotFactories[typeof(T)] = new SlotFactory<T>();
            if (cellAccess is not null)
                scope.Cells[type] = cellAccess;
            if (copy is not null) {
                scope.Copies[type] = copy;
                scope.Copies[VmValue.StorageType(type)] = copy;
            }
        }
    }

    public static void RegisterNative(Type type, Func<object?, int, IntPtr> cell,
        Action<VmReference, IntPtr> copy, Action<IntPtr, IntPtr> nativeCopy) {
        RegisterNative(DefaultScope, type, cell, copy, nativeCopy);
    }

    public static void RegisterNative(Scope scope, Type type, Func<object?, int, IntPtr> cell,
        Action<VmReference, IntPtr> copy, Action<IntPtr, IntPtr> nativeCopy) {
        var pointer = new NativeAccess(type, PointerNativeAddress, nativeCopy).Create();
        var cellAccess = new NativeAccess(type, cell, nativeCopy).Create();
        lock (scope) {
            scope.Native[type] = (pointer, cellAccess);
            scope.Copies[type] = copy;
            scope.Copies[VmValue.StorageType(type)] = copy;
        }
    }

    public static VmReference Slot(Type type, object? value) {
        return Slot(DefaultScope, type, value);
    }

    public static VmReference Slot(Scope scope, Type type, object? value) {
        var storageType = type.IsByRef ? typeof(VmReference) : VmValue.StorageType(type);
        lock (scope)
            if (scope.SlotFactories.TryGetValue(storageType, out var factory))
                return factory.Create(scope, type, value);
        return VmCrash.Fail<VmReference>(27);
    }

    private static VmReference SlotCore<T>(Scope scope, Type type, object? value) {
        if (type.IsByRef && value is null)
            value = Pointer(scope, IntPtr.Zero, type.GetElementType()!);
        var storage = new StrongBox<T> {
            Value = value is null ? default! : (T)value,
        };
        return new VmReference(storage, 0, type, GetManaged(scope, typeof(T)).Slot, scope, GetCopy(scope, type));
    }

    public static VmReference Pointer(IntPtr address, Type type, int nativeBank = -1) {
        return Pointer(DefaultScope, address, type, nativeBank);
    }

    public static VmReference Pointer(Scope scope, IntPtr address, Type type, int nativeBank = -1) {
        var native = nativeBank >= 0 || type.IsByRefLike;
        var access = native ? GetNative(scope, type).Pointer : GetManaged(scope, VmValue.StorageType(type)).Pointer;
        return new VmReference(address, 0, type, access, scope, GetCopy(scope, type), nativeBank: native ? nativeBank : -1);
    }

    public static VmReference Cell(IntPtr address, Type type) {
        return Cell(DefaultScope, address, type);
    }

    public static VmReference Cell(Scope scope, IntPtr address, Type type) {
        var access = type.IsByRefLike ? GetNative(scope, type).Cell : GetCell(scope, type);
        return new VmReference(address, 0, type, access, scope, GetCopy(scope, type), true);
    }

    public void CopyToCell(IntPtr address) {
        (_copy ?? VmCrash.Fail<Action<VmReference, IntPtr>>())(this, address);
    }

    public VmValue Read() {
        var value = _read(Owner, Index);
        if (value is VmNativeValue native)
            value = new VmNativeValue(native.Address, native.Type, NativeBank, native.Copy);
        return VmValue.Load(value, ElementType);
    }

    public void Write(VmValue value) => _write(Owner, Index, value.Store(ElementType, _scope));

    public unsafe ref T GetAddress<T>() where T : allows ref struct {
        if (typeof(T).IsByRefLike) {
            if (typeof(T) != ElementType)
                VmCrash.Fail();
            return ref Unsafe.AsRef<T>((void*)((Func<object?, int, IntPtr>)Address)(Owner, Index));
        }
        return ref ((VmAddress<T>)Address)(Owner, Index);
    }

    public ref byte GetBytes() => ref _bytes(Owner, Index);

    private static (Access Slot, Access Pointer) GetManaged(Scope scope, Type type) {
        lock (scope)
            return scope.Managed.TryGetValue(type, out var access) ? access : VmCrash.Fail<(Access, Access)>();
    }

    private static Access GetCell(Scope scope, Type type) {
        lock (scope)
            return scope.Cells.TryGetValue(type, out var access) ? access : VmCrash.Fail<Access>();
    }

    private static Action<VmReference, IntPtr>? GetCopy(Scope scope, Type type) {
        lock (scope)
            return scope.Copies.GetValueOrDefault(type) ??
                (type.IsByRef ? scope.Copies.GetValueOrDefault(type.GetElementType()!) : null);
    }

    private static (Access Pointer, Access Cell) GetNative(Scope scope, Type type) {
        lock (scope)
            return scope.Native.TryGetValue(type, out var access) ? access : VmCrash.Fail<(Access, Access)>();
    }

    private static ref T SlotAddress<T>(object? owner, int index) => ref ((StrongBox<T>)owner!).Value;

    private static unsafe ref T PointerAddress<T>(object? owner, int index) => ref Unsafe.AsRef<T>((void*)(IntPtr)owner!);

    private static IntPtr PointerNativeAddress(object? owner, int index) => (IntPtr)owner!;

    private static unsafe ref byte AddressBytes(IntPtr address) => ref Unsafe.AsRef<byte>((void*)address);
}
