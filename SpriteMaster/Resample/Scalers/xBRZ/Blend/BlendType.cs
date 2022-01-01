using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Blend;

enum BlendType : byte {
	// These blend types must fit into 2 bits.
	None = 0, //do not blend
	Normal = 1, //a normal indication to blend
	Dominant = 2 //a strong indication to blend
}

static class BlendTypeExtension {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte Byte(this BlendType type) => (byte)type;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType BlendType(this byte value) => (BlendType)value;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType BlendType(this int value) => (BlendType)value;
}
