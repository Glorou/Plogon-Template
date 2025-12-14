using System.Runtime.InteropServices;

namespace SamplePlugin.Apricot;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public struct InstanceContainer {
    public const int Size = 0x88;

    [FieldOffset(0)] public unsafe ApricotInstance* Instance;
}
