using SpriteMaster.Extensions;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Colors;

static class ColorConstant {
	internal static class Shift {
		internal const int Alpha = 24;
		internal const int Red = 0;
		internal const int Green = 8;
		internal const int Blue = 16;
	}

	internal static class Mask {
		internal const uint Alpha = 0xFFU << Shift.Alpha;
		internal const uint Red = 0xFFU << Shift.Red;
		internal const uint Green = 0xFFU << Shift.Green;
		internal const uint Blue = 0xFFU << Shift.Blue;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static double ValueToScalar(byte value) => value / 255.0;
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static double ValueToScalar(ushort value) => value / 655_35.0;
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static double ValueToScalar(uint value) => value / 4_294_967_295.0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte ScalarToValue8(double scalar) => (byte)((scalar * 255.0) + 0.5);
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ushort ScalarToValue16(double scalar) => (ushort)((scalar * 655_35.0) + 0.5);
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static uint ScalarToValue32(double scalar) => (uint)((scalar * 4_294_967_295.0) + 0.5);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ushort Color8To16(byte value) => (ushort)((value << 8) | value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte Color16To8Fast(ushort value) => (byte)((uint)((value * 0xFF01) + 0x800000) >> 24);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte Color16To8Accurate(ushort value) => (byte)(((uint)value + 128) / 0x101);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte Color16to8(ushort value) => Color16To8Accurate(value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static half Color8ToHalf(byte value) {
		if (value == 0) {
			ushort shortValue = 0;
			return shortValue.ReinterpretAs<half>();
		}
		const uint InBits = sizeof(byte) * 8;
		uint uValue = (uint)value;
		int leadingZerosWithMSB = value.CountLeadingZeros() + 1;
		uValue = (byte)(uValue << leadingZerosWithMSB);
		uint remainingBits = (uint)(InBits - (leadingZerosWithMSB));
		int shiftRight = (leadingZerosWithMSB) + ((int)remainingBits - (int)Numeric.Float.Half.SignificandBits);
		if (InBits <= Numeric.Float.Half.SignificandBits || shiftRight < 0) {
			uValue <<= -shiftRight;
		}
		else {
			uValue >>= shiftRight;
		}
		uint mantissa = uValue;
		uint exponent = ((uint)-(int)(leadingZerosWithMSB + 1)) & (Numeric.Float.Half.ExponentMask >> 1);
		uint result = (mantissa | exponent << (int)Numeric.Float.Half.SignificandBits);
		return result.ReinterpretAs<half>();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static half Color16ToHalf(ushort value) {
		if (value == 0) {
			return value.ReinterpretAs<half>();
		}
		const uint InBits = sizeof(ushort) * 8;
		uint uValue = (uint)value;
		int leadingZerosWithMSB = value.CountLeadingZeros() + 1;
		uValue = (byte)(uValue << leadingZerosWithMSB);
		uint remainingBits = (uint)(InBits - (leadingZerosWithMSB));
		int shiftRight = (leadingZerosWithMSB) + ((int)remainingBits - (int)Numeric.Float.Half.SignificandBits);
		if (InBits <= Numeric.Float.Half.SignificandBits || shiftRight < 0) {
			uValue <<= -shiftRight;
		}
		else {
			uValue >>= shiftRight;
		}
		uint mantissa = uValue;
		uint exponent = ((uint)-(int)(leadingZerosWithMSB + 1)) & (Numeric.Float.Half.ExponentMask >> 1);
		uint result = (mantissa | exponent << (int)Numeric.Float.Half.SignificandBits);
		return result.ReinterpretAs<half>();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static float Color8ToFloat(byte value) {
		if (value == 0) {
			return 0.0f;
		}
		const uint InBits = sizeof(byte) * 8;
		uint uValue = (uint)value;
		int leadingZerosWithMSB = value.CountLeadingZeros() + 1;
		uValue = (byte)(uValue << leadingZerosWithMSB);
		uint remainingBits = (uint)(InBits - (leadingZerosWithMSB));
		int shiftRight = (leadingZerosWithMSB) + ((int)remainingBits - (int)Numeric.Float.Single.SignificandBits);
		if (InBits <= Numeric.Float.Single.SignificandBits || shiftRight < 0) {
			uValue <<= -shiftRight;
		}
		else {
			uValue >>= shiftRight;
		}
		uint mantissa = uValue;
		uint exponent = ((uint)-(int)(leadingZerosWithMSB + 1)) & (Numeric.Float.Single.ExponentMask >> 1);
		uint result = (mantissa | exponent << (int)Numeric.Float.Single.SignificandBits);
		return result.ReinterpretAs<float>();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static float Color16ToFloat(ushort value) {
		if (value == 0) {
			return 0.0f;
		}
		const uint InBits = sizeof(ushort) * 8;
		uint uValue = (uint)value;
		int leadingZerosWithMSB = value.CountLeadingZeros() + 1;
		uValue = (byte)(uValue << leadingZerosWithMSB);
		uint remainingBits = (uint)(InBits - (leadingZerosWithMSB));
		int shiftRight = (leadingZerosWithMSB) + ((int)remainingBits - (int)Numeric.Float.Single.SignificandBits);
		if (InBits <= Numeric.Float.Single.SignificandBits || shiftRight < 0) {
			uValue <<= -shiftRight;
		}
		else {
			uValue >>= shiftRight;
		}
		uint mantissa = uValue;
		uint exponent = ((uint)-(int)(leadingZerosWithMSB + 1)) & (Numeric.Float.Single.ExponentMask >> 1);
		uint result = (mantissa | exponent << (int)Numeric.Float.Single.SignificandBits);
		return result.ReinterpretAs<float>();
	}
}
