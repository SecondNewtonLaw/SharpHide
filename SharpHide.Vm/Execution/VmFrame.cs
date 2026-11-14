using System.Runtime.CompilerServices;

namespace SharpHide.Vm;

public sealed class VmFrame {
    private int _allocationRegister;
    private readonly byte[] _code;
    private readonly object?[] _metadata;
    private readonly bool[] _boundaries;
    private readonly VmReference[] _arguments;
    private readonly VmReference[] _locals;
    private readonly VmExceptionRegion[] _regions;
    private readonly VmPins _pins;
    private readonly VmReference.Scope _scope;
    private readonly int[] _nativeBanks;
    private readonly int _bodyStart;
    private readonly int _registerCount;
    private readonly Stack<VmUnwind> _unwinds = new();
    private readonly Dictionary<VmExceptionRegion, VmExceptionState> _caught = new();
    private readonly Queue<int> _changedRoots = new();
    private VmEvaluation? _current;
    private VmEvaluation? _interrupted;
    private VmExceptionState? _search;
    private VmExceptionRegion? _filter;
    private int _candidate;
    private bool _searchComplete;
    private bool _escapeRequested;
    private bool _allocating;
    private UIntPtr _allocationSize;
    private IntPtr[] _cells = [];
    private VmReference?[] _roots = [];
    public readonly Type ReturnType;
    public readonly int ReturnBank;
    public readonly int CellCount;
    public readonly Type[] NativeTypes;
    public readonly int UnwindDepth;
    public object? Result;
    public bool Transferring;
    public IntPtr ResultCell;
    internal VmReference.Scope ReferenceScope => _scope;

    public VmFrame(byte[] code, object?[] metadata, bool[] boundaries, VmReference[] arguments, VmReference[] locals,
        VmExceptionRegion[] regions, VmPins pins, int bodyStart, int registerCount, bool referenceCalls, Type[] nativeTypes,
        int[] nativeBanks, VmReference.Scope scope) {
        _code = code;
        _metadata = metadata;
        _boundaries = boundaries;
        _arguments = arguments;
        _locals = locals;
        _regions = regions;
        _pins = pins;
        _scope = scope;
        _nativeBanks = nativeBanks;
        _bodyStart = bodyStart;
        _registerCount = registerCount;
        ReturnType = (Type)metadata[0]!;
        ReturnBank = nativeBanks[0];
        NativeTypes = nativeTypes;
        UnwindDepth = regions.Count(r => r.Kind is 2 or 4);
        CellCount = GetCellCount(arguments.Length, locals.Length, registerCount, regions.Length, UnwindDepth,
            referenceCalls || NativeTypes.Length > 1, NativeTypes.Length - 1);
        if (CellCount < 0)
            VmCrash.Fail();
        _current = new VmEvaluation(bodyStart, bodyStart, code.Length, VmOpcodes.Ret, registerCount, 0);
    }

    public static int GetCellCount(int arguments, int locals, int stack, int regions, int unwindDepth, bool referenceCalls, int nativeTypes = 0) {
        var count = referenceCalls ? 1L + arguments + locals + (long)stack * (regions + 1L) : 0;
        return (2L + nativeTypes) * count + unwindDepth + 4 > VmOpcodes.MaxFrameSize ? -1 : (int)count;
    }

    public IntPtr Cell(int bank, int index) {
        if ((uint)bank >= (uint)NativeTypes.Length || (uint)index >= (uint)CellCount)
            return IntPtr.Zero;
        return _cells[bank * CellCount + index];
    }

    public void AttachCells(IntPtr[] cells) {
        if (cells.Length != CellCount * NativeTypes.Length)
            VmCrash.Fail();
        _cells = cells;
        _roots = new VmReference?[CellCount];
        if (cells.Length == 0)
            return;
        for (var i = 0; i < _arguments.Length; i++) {
            var value = _arguments[i].Read();
            if (_nativeBanks[1 + i] >= 0) {
                var captured = (VmNativeValue)Capture(value, 1 + i).Value!;
                _arguments[i] = VmReference.Pointer(_scope, captured.Address, value.Type, captured.Bank);
            }
            else if (value.Type.IsByRef)
                _arguments[i].Write(Capture(value, 1 + i));
        }
        for (var i = 0; i < _locals.Length; i++) {
            var type = (Type)_metadata[1 + _arguments.Length + i]!;
            var bank = _nativeBanks[1 + _arguments.Length + i];
            if (bank >= 0) {
                _locals[i] = VmReference.Pointer(_scope, Cell(bank, 1 + _arguments.Length + i), type, bank);
                continue;
            }
            var value = _locals[i].Read();
            if (value.Type.IsByRef)
                _locals[i].Write(Capture(value, 1 + _arguments.Length + i));
        }
    }

    private VmValue Capture(VmValue value, int index) {
        if (value.Value is VmNativeValue native) {
            if (CellCount == 0)
                VmCrash.Fail();
            var cell = Cell(native.Bank, index);
            native.CopyTo(cell);
            _roots[index] = null;
            _changedRoots.Enqueue(index);
            return VmValue.Load(new VmNativeValue(cell, value.Type, native.Bank, native.Copy), value.Type);
        }
        if (CellCount == 0 || !value.Type.IsByRef)
            return value;
        var reference = (VmReference)value.Store(value.Type, _scope)!;
        reference.CopyToCell(_cells[index]);
        var captured = VmReference.Cell(_scope, _cells[index], value.Type.GetElementType()!);
        _roots[index] = captured;
        _changedRoots.Enqueue(index);
        return VmValue.Load(captured, value.Type);
    }

    public int NextRoot() => _changedRoots.TryDequeue(out var index) ? index : -1;

    public static ref byte Root(VmFrame frame, int index) {
        if (frame._roots[index] is { } reference)
            return ref reference.GetBytes();
        return ref Unsafe.NullRef<byte>();
    }

    public void PinRoot(int index, IntPtr address) {
        if (_roots[index] is { } reference) {
            reference.PinnedAddress = address;
            reference.IsPinned = true;
        }
    }

    public static ref T ReferenceResult<T>(VmFrame frame) where T : allows ref struct => ref ((VmReference)frame.Result!).GetAddress<T>();
    public object? ValueResult() {
        if (ReturnBank < 0)
            return Result;
        ((VmNativeValue)Result!).CopyTo(ResultCell);
        return null;
    }
    public bool Ready() => _current is not null && !_allocating;
    public bool NeedsAllocation() => _allocating;
    public UIntPtr AllocationSize() => _allocationSize;

    public void Allocated(IntPtr address) {
        var current = _current!;
        current.Registers[_allocationRegister] = VmValue.Load(address, typeof(IntPtr));
        _allocating = false;
    }

    public bool HasHandler() => _search?.Handler is not null;
    public VmExceptionState Error() => _search!;
    public void ResumeTransfer() => Transferring = false;

    public void BeginSearch(Exception error) {
        var origin = _current!.Origin;
        if (_searchComplete && _search?.Origin == origin && ReferenceEquals(_search.Error.SourceException, error))
            return;
        _search = new VmExceptionState(error, origin, null);
        _interrupted = _current;
        _candidate = 0;
        _filter = null;
        _searchComplete = false;
    }

    public bool SearchStep() {
        if (_searchComplete)
            return false;
        if (_filter is not null) {
            if (!_current!.Complete) {
                try {
                    Step();
                }
                catch (BadImageFormatException) when (VmCrash.ConsumeIntegrityFault()) {
                    throw;
                }
                catch (Exception) {
                    _current!.Result = 0;
                    _current.Complete = true;
                }
                return true;
            }
            if ((int)_current.Result! != 0)
                _search = new VmExceptionState(_search!.Error.SourceException, _search.Origin, _filter);
            _current = _interrupted;
            _filter = null;
        }
        while (_search!.Handler is null && _candidate < _regions.Length) {
            var region = _regions[_candidate++];
            if (_search.Origin < region.TryStart || _search.Origin >= region.TryEnd)
                continue;
            if (region.Kind == 0 && region.CatchType!.IsInstanceOfType(_search.Error.SourceException)) {
                _search = new VmExceptionState(_search.Error.SourceException, _search.Origin, region);
                break;
            }
            if (region.Kind != 1)
                continue;
            _filter = region;
            _current = new VmEvaluation(region.FilterStart, region.FilterStart, region.HandlerStart,
                VmOpcodes.Endfilter, _registerCount, _interrupted!.Level + 1);
            _current.Registers[0] = VmValue.Load(_search.Error.SourceException, typeof(object));
            return true;
        }
        _current = _interrupted;
        _searchComplete = true;
        _escapeRequested = _search.Handler is null;
        return false;
    }

    public bool Catch(bool unwindDriver) {
        var error = _search!;
        var handler = error.Handler!;
        while (_current is not null && (handler.HandlerStart < _current.Start || handler.HandlerStart >= _current.End))
            _current = _unwinds.Count == 0 ? null : _unwinds.Pop().Resume;
        _current ??= new VmEvaluation(handler.HandlerStart, _bodyStart, _code.Length, VmOpcodes.Ret, _registerCount, 0);
        var resume = _current;
        resume.Position = handler.HandlerStart;
        resume.Registers[0] = VmValue.Load(error.Error.SourceException, typeof(object));
        _caught[handler] = error;
        _searchComplete = false;
        _escapeRequested = false;
        StartUnwind(error.Origin, handler.HandlerStart, error, resume);
        Transferring = unwindDriver && resume.Stop == VmOpcodes.Ret;
        return Transferring;
    }

    public bool BeginEscape() {
        if (!_escapeRequested || Transferring)
            return false;
        _escapeRequested = false;
        _unwinds.Clear();
        StartUnwind(_search!.Origin, -1, _search, null);
        return _current is not null;
    }

    private void StartUnwind(int origin, int target, VmExceptionState? error, VmEvaluation? resume) {
        var regions = _regions.Where(r => (r.Kind == 2 || r.Kind == 4 && error is not null) &&
            origin >= r.TryStart && origin < r.TryEnd && !(target >= r.TryStart && target < r.TryEnd) &&
            error?.Unwound.Contains(r) != true).ToArray();
        if (regions.Length == 0) {
            _current = resume;
            return;
        }
        _unwinds.Push(new VmUnwind(resume, regions, error));
        AdvanceUnwind();
    }

    private void AdvanceUnwind() {
        var pending = _unwinds.Peek();
        if (pending.Next == pending.Regions.Length) {
            _unwinds.Pop();
            _current = pending.Resume;
            return;
        }
        var region = pending.Regions[pending.Next++];
        pending.Error?.Unwound.Add(region);
        _current = new VmEvaluation(region.HandlerStart, region.HandlerStart, region.HandlerEnd,
            VmOpcodes.Endfinally, _registerCount, pending.Resume?.Level + 1 ?? 0);
    }

    private int Int() {
        var current = _current!;
        if (current.Position < 0 || current.Position > _code.Length - 4)
            VmCrash.Fail();
        var position = current.Position;
        current.Position += 4;
        return _code[position] | _code[position + 1] << 8 | _code[position + 2] << 16 | _code[position + 3] << 24;
    }

    private static void ExecuteBitwise(VmValue[] registers, int register, byte op) {
        registers[register] = VmBitwise.Execute(op, registers[register], op == VmOpcodes.BitNot ? default : registers[register + 1]);
    }

    private static void ExecuteArithmetic(VmValue[] registers, int register, byte op) {
        var left = registers[register];
        var right = op == VmOpcodes.Negate ? default : registers[register + 1];
        registers[register] = VmArithmetic.Execute(op, left, right);
    }

    private void ExecuteOperation(VmValue[] registers, int register, int registerBase) {
        var index = Int();
        var inputCount = Int();
        var operation = (VmOperation)_metadata[index]!;
        if (VmOperations.Execute(operation, registers, register, inputCount, _pins,
            frame: this, resultIndex: registerBase + register))
            registers[register] = Capture(registers[register], registerBase + register);
    }

    private void BeginAllocation(VmEvaluation current, VmValue[] registers, int register) {
        if (_regions.Any(r => current.Origin >= (r.Kind == 1 ? r.FilterStart : r.HandlerStart) && current.Origin < r.HandlerEnd))
            VmCrash.Fail();
        var size = registers[register].Value;
        _allocationSize = size switch {
            int value => unchecked((UIntPtr)(uint)value),
            IntPtr value => unchecked((UIntPtr)(nuint)(nint)value),
            _ => VmCrash.Fail<UIntPtr>(),
        };
        _allocating = true;
        _allocationRegister = register;
    }

    public void Step() {
        var current = _current!;
        if (current.Position < current.Start || current.Position >= current.End ||
            !_boundaries[current.Position])
            VmCrash.Fail();
        current.Origin = current.Position;
        var registers = current.Registers;
        var registerBase = 1 + _arguments.Length + _locals.Length + current.Level * _registerCount;
        var op = _code[_code[current.Position++]];
        var register = Int();
        if (op is >= VmOpcodes.BitAnd and <= VmOpcodes.BitNot) {
            ExecuteBitwise(registers, register, op);
            return;
        }
        if (op is >= VmOpcodes.Add and <= VmOpcodes.RemainderUnsigned) {
            ExecuteArithmetic(registers, register, op);
            return;
        }
        if (op == VmOpcodes.Operation) {
            ExecuteOperation(registers, register, registerBase);
            return;
        }
        if (op == VmOpcodes.Localloc) {
            BeginAllocation(current, registers, register);
            return;
        }
        if (op == VmOpcodes.Leave) {
            var target = _bodyStart + Int();
            current.Position = target;
            StartUnwind(current.Origin, target, null, current);
            return;
        }
        if (op == VmOpcodes.Rethrow) {
            var handler = _regions.Where(r => _caught.ContainsKey(r) && current.Origin >= r.HandlerStart && current.Origin < r.HandlerEnd)
                .OrderBy(r => r.HandlerEnd - r.HandlerStart).FirstOrDefault();
            if (handler is null)
                VmCrash.Fail();
            _caught[handler].Error.Throw();
        }
        if (op is VmOpcodes.Endfinally or VmOpcodes.Endfilter) {
            if (op != current.Stop)
                VmCrash.Fail();
            if (op == VmOpcodes.Endfilter) {
                current.Result = (int)registers[register].Value!;
                current.Complete = true;
            }
            else
                AdvanceUnwind();
            return;
        }
        if (op == VmOpcodes.Ret && current.Stop != VmOpcodes.Ret)
            VmCrash.Fail();
        switch (op) {
            case VmOpcodes.Ldarg or VmOpcodes.Ldloc: {
                var index = Int();
                var value = (op == VmOpcodes.Ldarg ? _arguments[index] : _locals[index]).Read();
                registers[register] = Capture(value, registerBase + register);
                break;
            }
            case VmOpcodes.Starg or VmOpcodes.Stloc: {
                var index = Int();
                var slot = op == VmOpcodes.Starg ? _arguments[index] : _locals[index];
                var value = registers[register];
                if (slot.ElementType.IsByRef)
                    value = Capture(VmValue.Load(value.Store(slot.ElementType, _scope), slot.ElementType),
                        1 + (op == VmOpcodes.Starg ? 0 : _arguments.Length) + index);
                slot.Write(value);
                break;
            }
            case VmOpcodes.Ldarga or VmOpcodes.Ldloca: {
                var index = Int();
                var reference = op == VmOpcodes.Ldarga ? _arguments[index] : _locals[index];
                registers[register] = Capture(new VmValue(reference, reference.ElementType.MakeByRefType()), registerBase + register);
                break;
            }
            case VmOpcodes.Ldc:
                registers[register] = VmValue.Load(Int(), typeof(int));
                break;
            case VmOpcodes.Dup:
                registers[register + 1] = Capture(registers[register].Copy(_scope), registerBase + register + 1);
                break;
            case VmOpcodes.Pop:
                break;
            case VmOpcodes.Br:
                current.Position = _bodyStart + Int();
                break;
            case VmOpcodes.BrTrue or VmOpcodes.BrFalse: {
                var target = _bodyStart + Int();
                if (registers[register].IsTrue() == (op == VmOpcodes.BrTrue))
                    current.Position = target;
                break;
            }
            case VmOpcodes.Switch: {
                var count = Int();
                var index = (int)registers[register].Value!;
                var next = current.Position + count * 4;
                if ((uint)index < (uint)count) {
                    current.Position += index * 4;
                    current.Position = _bodyStart + Int();
                }
                else
                    current.Position = next;
                break;
            }
            case VmOpcodes.Ret:
                Result = ReturnType == typeof(void) ? null : registers[register].Store(ReturnType, _scope);
                if (ReturnType.IsByRef)
                    Result = Capture(VmValue.Load(Result, ReturnType), 0).Value;
                _current = null;
                break;
        }
    }
}
