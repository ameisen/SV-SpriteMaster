using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[DebuggerDisplay("[{Bytes[0]}, {Bytes[1]}, {Bytes[2]}, {Bytes[3]}]")]
[DebuggerDisplay("[{Shorts[0]}, {Shorts[1]}]")]
[DebuggerDisplay("{Packed}")]
[StructLayout(LayoutKind.Explicit, Pack = sizeof(uint), Size = sizeof(uint))]
internal unsafe struct PackedUInt {
	[FieldOffset(0)]
	internal fixed byte Bytes[4];

	[FieldOffset(0)]
	internal fixed ushort Shorts[2];

	[FieldOffset(0)]
	internal uint Packed;

	internal PackedUInt(byte b0, byte b1, byte b2, byte b3) : this() {
		Bytes[0] = b0;
		Bytes[1] = b1;
		Bytes[2] = b2;
		Bytes[3] = b3;
	}

	internal PackedUInt(ushort s0, ushort s1) : this() {
		Shorts[0] = s0;
		Shorts[1] = s1;
	}

	internal PackedUInt(uint packed) : this() {
		Packed = packed;
	}

	public static implicit operator uint(PackedUInt value) => value.Packed;
	public static implicit operator PackedUInt(uint value) => new(value);
}
