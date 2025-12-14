using System.Runtime.InteropServices;

namespace SamplePlugin.Apricot;

[StructLayout(LayoutKind.Explicit, Size = 0x238)]
public struct ApricotParticle {
    [FieldOffset(0x70)] public unsafe RgbFunctionCurve* Rgb;
}
