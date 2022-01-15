using SpriteMaster.xBRZ.Common;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Blend;

using PreprocessType = Byte;

static class BlendInfo {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType GetTopR(this PreprocessType b) => ((PreprocessType)(b >> 2) & 0x3).BlendType();
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType GetBottomR(this PreprocessType b) => ((PreprocessType)(b >> 4) & 0x3).BlendType();
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static BlendType GetBottomL(this PreprocessType b) => ((PreprocessType)(b >> 6) & 0x3).BlendType();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType SetTopL(this PreprocessType b, BlendType bt) => (PreprocessType)(b | bt.Value());
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType SetTopR(this PreprocessType b, BlendType bt) => (PreprocessType)(b | (bt.Value() << 2));
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType SetBottomR(this PreprocessType b, BlendType bt) => (PreprocessType)(b | (bt.Value() << 4));
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType SetBottomL(this PreprocessType b, BlendType bt) => (PreprocessType)(b | (bt.Value() << 6));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool BlendingNeeded(this PreprocessType b) => b != 0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PreprocessType Rotate(this PreprocessType b, RotationDegree rotDeg) {
		var l = (PreprocessType)((PreprocessType)rotDeg << 1);
		var r = (PreprocessType)(8 - l);

		return (PreprocessType)(b << l | b >> r);
	}
}
