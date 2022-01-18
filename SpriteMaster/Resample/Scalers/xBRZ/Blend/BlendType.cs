using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers.xBRZ.Blend;

using PreprocessType = Byte;

enum BlendType : PreprocessType {
	// These blend types must fit into 2 bits.
	None = 0, //do not blend
	Normal = 1, //a normal indication to blend
	Dominant = 2 //a strong indication to blend
}

static class BlendTypeExtension {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType Value(this BlendType type) => (byte)type;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType BlendType(this PreprocessType value) => (BlendType)value;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType BlendType(this int value) => (BlendType)value;
}
