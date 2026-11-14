namespace SharpHide.Vm;

public static class VmOperations {
    public static bool Execute(VmOperation operation, VmValue[] registers, int firstInput, int inputCount,
        VmPins? pins = null, IntPtr referenceCell = default, VmFrame? frame = null, int resultIndex = 0) {
        if ((uint)firstInput > (uint)registers.Length || (uint)inputCount > (uint)(registers.Length - firstInput))
            VmCrash.Fail();

        var inputs = new VmValue[inputCount];
        Array.Copy(registers, firstInput, inputs, 0, inputCount);
        var resultType = VmValue.StorageType(operation.ResultType);
        if (frame is not null)
            referenceCell = frame.Cell(operation.ResultBank, resultIndex);
        if (operation.ResultBank >= 0 && referenceCell == IntPtr.Zero)
            VmCrash.Fail();
        if (resultType == typeof(IntPtr))
            foreach (var input in inputs)
                if (input.Value is VmReference reference)
                    (pins ?? VmCrash.Fail<VmPins>()).Pin(reference);
        if (resultType != typeof(void) && firstInput >= registers.Length)
            VmCrash.Fail();

        var value = operation.Bridge(inputs.Select(input => input.Value).ToArray(), referenceCell);
        if (resultType.IsByRef)
            value = frame is null
                ? VmReference.Cell(referenceCell, resultType.GetElementType()!)
                : VmReference.Cell(frame.ReferenceScope, referenceCell, resultType.GetElementType()!);
        else if (operation.ResultBank >= 0)
            value = new VmNativeValue(referenceCell, resultType, operation.ResultBank,
                operation.NativeCopy ?? VmCrash.Fail<Action<IntPtr, IntPtr>>());
        if (resultType == typeof(void))
            return false;
        registers[firstInput] = VmValue.Load(value, resultType);
        return true;
    }
}
