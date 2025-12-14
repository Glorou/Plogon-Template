using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;

namespace SamplePlugin.Apricot;

[StructLayout(LayoutKind.Explicit, Size = 0x340)]
public struct VfxObject {
    [FieldOffset(0)] public Object Object;

    [FieldOffset(0x2A0)] public unsafe VfxResourceInstance* ResourceInstance;
}
