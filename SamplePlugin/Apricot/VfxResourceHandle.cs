using System.Runtime.InteropServices;

namespace SamplePlugin.Apricot;


[StructLayout(LayoutKind.Explicit)]
public struct VfxResourceHandle {
    [FieldOffset(0)] public ulong Value;

    [FieldOffset(0)] public uint Id;
    [FieldOffset(4)] public uint Index;
}
