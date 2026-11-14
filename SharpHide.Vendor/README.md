# SharpHide.Vendor

Source markers for projects protected by SharpHide. Reference this project from application code; successful protection consumes its attributes and region calls, so protected output does not need `SharpHide.Vendor.dll` at runtime.

`[Virtualize]` on a method protects that method. Applying it to a class or struct protects every eligible method. `[DoNotVirtualize]` on a method or type wins over inherited selection and keeps original CLR IL.

```csharp
using SharpHide.Vendor;

[Virtualize]
internal static class SensitiveCode {
    public static int Transform(int value) => value * 17 + 3;

    [DoNotVirtualize]
    public static int HotPath(int value) => value + 1;
}
```

`ObfuscationMacros.VMBegin()` and `VMEnd()` select one or more regions inside an otherwise normal method. Boundaries require an empty IL evaluation stack. Branches cannot cross a boundary, exception regions are unsupported, and selected regions cannot reassign byref locals. Rejected Vendor regions keep CLR IL and lose their no-op marker calls.

```csharp
var result = input;
ObfuscationMacros.VMBegin();
result = Transform(result);
ObfuscationMacros.VMEnd();
return result;
```

`[ControlFlow]` overrides global flattening settings on one method or type. Available properties: `MinBlocks`, `MinSize`, `MaxSize`, `BogusStates`, `OpaqueDepth`, and `DecryptHashChance`. Omitted properties keep project values. `[DoNotControlFlow]` skips flattening.
