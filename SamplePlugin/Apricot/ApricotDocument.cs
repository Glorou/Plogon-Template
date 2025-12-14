using System.Runtime.InteropServices;

namespace SamplePlugin.Apricot;

[StructLayout(LayoutKind.Explicit)]
public struct ApricotDocument {
    [FieldOffset(0x40)] public unsafe ApricotParticle* Particles;

    [FieldOffset(0xE6)] public byte ParticleCount;
}
