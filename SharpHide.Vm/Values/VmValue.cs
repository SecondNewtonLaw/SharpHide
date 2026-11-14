using System.Runtime.CompilerServices;

namespace SharpHide.Vm;

public readonly struct VmValue(object? value, Type type) {
    public readonly object? Value = value;
    public readonly Type Type = type;

    public static Type StorageType(Type type) => type.IsPointer || type.IsFunctionPointer ? typeof(IntPtr) : type;

    public static VmValue Load(object? value, Type type) {
        type = StorageType(type);
        if (value is VmNativeValue)
            return new VmValue(value, type);
        if (type.IsByRef || type.IsByRefLike)
            return new VmValue(value, type);
        if (Nullable.GetUnderlyingType(type) is not null)
            return new VmValue(value, type);
        if (!type.IsValueType)
            return new VmValue(value, typeof(object));

        if (type.IsEnum) {
            type = Enum.GetUnderlyingType(type);
            value = Convert.ChangeType(value, type);
        }

        return value switch {
            bool v => new VmValue(v ? 1 : 0, typeof(int)),
            char v => new VmValue((int)v, typeof(int)),
            sbyte v => new VmValue((int)v, typeof(int)),
            byte v => new VmValue((int)v, typeof(int)),
            short v => new VmValue((int)v, typeof(int)),
            ushort v => new VmValue((int)v, typeof(int)),
            uint v => new VmValue(unchecked((int)v), typeof(int)),
            ulong v => new VmValue(unchecked((long)v), typeof(long)),
            UIntPtr v => new VmValue(unchecked((IntPtr)(nint)(nuint)v), typeof(IntPtr)),
            _ => new VmValue(RuntimeHelpers.GetObjectValue(value), type),
        };
    }

    public object? Store(Type type) => Store(type, null);

    internal object? Store(Type type, VmReference.Scope? scope) {
        type = StorageType(type);
        if (Value is VmNativeValue native) {
            if (native.Type == type)
                return native;
            return VmCrash.Fail<object?>();
        }
        if (type.IsByRef) {
            if (Value is IntPtr address)
                return scope is null
                    ? VmReference.Pointer(address, type.GetElementType()!)
                    : VmReference.Pointer(scope, address, type.GetElementType()!);
            if (Value is null)
                return scope is null
                    ? VmReference.Pointer(IntPtr.Zero, type.GetElementType()!)
                    : VmReference.Pointer(scope, IntPtr.Zero, type.GetElementType()!);
            if (Value is VmReference)
                return Value;
            return VmCrash.Fail<object?>();
        }
        if (Nullable.GetUnderlyingType(type) is { } underlying) {
            if (Value is null)
                return null;
            return VmValue.Load(Value, underlying).Store(underlying, scope);
        }
        if (!type.IsValueType) {
            if (Value is not null && !type.IsInstanceOfType(Value))
                return VmCrash.Fail<object?>();
            return Value;
        }

        if (type.IsEnum)
            return Enum.ToObject(type, Store(Enum.GetUnderlyingType(type))!);
        if (type == typeof(bool))
            return (int)Value! != 0;
        if (type == typeof(char))
            return unchecked((char)(int)Value!);
        if (type == typeof(sbyte))
            return unchecked((sbyte)(int)Value!);
        if (type == typeof(byte))
            return unchecked((byte)(int)Value!);
        if (type == typeof(short))
            return unchecked((short)(int)Value!);
        if (type == typeof(ushort))
            return unchecked((ushort)(int)Value!);
        if (type == typeof(uint))
            return unchecked((uint)(int)Value!);
        if (type == typeof(ulong))
            return unchecked((ulong)(long)Value!);
        if (type == typeof(UIntPtr))
            return unchecked((UIntPtr)(nuint)(nint)(IntPtr)Value!);
        if (type == typeof(float) && Value is double d)
            return (float)d;
        if (type == typeof(double) && Value is float f)
            return (double)f;
        if (Value is null || Value.GetType() != type)
            return VmCrash.Fail<object?>();

        return RuntimeHelpers.GetObjectValue(Value);
    }

    public VmValue Copy() => Copy(null);

    internal VmValue Copy(VmReference.Scope? scope) => Type.IsValueType ? Load(Store(Type, scope), Type) : this;

    public bool IsTrue() => Type.IsByRef ? Value is VmReference { IsNull: false } : Type == typeof(object) ? Value is not null : Value switch {
        int v => v != 0,
        long v => v != 0,
        IntPtr v => v != IntPtr.Zero,
        _ => VmCrash.Fail<bool>(),
    };
}
