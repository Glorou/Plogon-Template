using System.Numerics;
using System.Runtime.InteropServices;

namespace SamplePlugin.Apricot;

[StructLayout(LayoutKind.Explicit)]
public struct VfxResourceInstance {
    [FieldOffset(0x60)] public VfxResourceHandle Handle;
    [FieldOffset(0xA0)] public Vector4 Color;
}
