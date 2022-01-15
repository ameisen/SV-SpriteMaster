using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Common;

static class IMath {
	[DebuggerStepThrough]
	private readonly ref struct Argument<T> where T : unmanaged {
		internal readonly T Value;
		internal readonly string Name;

		[DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
		internal Argument(T value, string name) {
			Value = value;
			Name = name;
		}

		[DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
		public static implicit operator T(in Argument<T> arg) {
			return arg.Value;
		}
	}

	[DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
	private static Argument<T> AsArg<T>(this T value, string name) where T : unmanaged => new(value, name);

	[DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
	private static string GetCheckNotNegativeMessage<T>(in Argument<T> argument, string memberName) where T : unmanaged {
		return memberName != null ?
			$"Argument {argument.Name} with value {argument.Value} from method {memberName} was less than zero" :
			$"Argument {argument.Name} with value {argument.Value} was less than zero";
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
	private static void CheckNotNegative(
		this in Argument<int> argument,
		[CallerMemberName] string memberName = null
	) {
		if (argument < 0) {
			throw new ArgumentOutOfRangeException(GetCheckNotNegativeMessage(argument, memberName));
		}
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(Runtime.MethodImpl.Hot)]
	private static void CheckNotNegative(
		this in Argument<long> argument,
		[CallerMemberName] string memberName = null
	) {
		if (argument < 0) {
			throw new ArgumentOutOfRangeException(GetCheckNotNegativeMessage(argument, memberName));
		}
	}

	// Precalculate square roots.
	private const int SQRT_MIN = 0;
	private const int SQRT_MAX = 100;
	private static readonly int[] SQRT_TABLE = new int[SQRT_MAX - SQRT_MIN];
	static IMath() {
		for (int i = 0; i < SQRT_TABLE.Length; ++i) {
			SQRT_TABLE[i] = (int)Math.Sqrt(i + SQRT_MIN);
		}
	}

	// Truncating Square Root
	internal static int Sqrt(this int value) {
		value.AsArg(nameof(value)).CheckNotNegative();

		if (value >= SQRT_MIN && value < SQRT_MAX) {
			return SQRT_TABLE[value];
		}

		return (int)Math.Sqrt(value);
	}

	// Truncating Square Root
	internal static uint Sqrt(this uint value) {
		if (value >= SQRT_MIN && value < SQRT_MAX) {
			return (uint)SQRT_TABLE[value];
		}

		return (uint)Math.Sqrt(value);
	}

	// Truncating Square Root
	internal static long Sqrt(this long value) {
		value.AsArg(nameof(value)).CheckNotNegative();

		if (value >= SQRT_MIN && value < SQRT_MAX) {
			return (long)SQRT_TABLE[value];
		}

		return (long)Math.Sqrt(value);
	}

	// Truncating Square Root
	internal static ulong Sqrt(this ulong value) {
		if (value >= SQRT_MIN && value < SQRT_MAX) {
			return (ulong)SQRT_TABLE[value];
		}

		return (ulong)Math.Sqrt(value);
	}

	internal static byte TruncateNarrow(this ushort value) => (byte)value;
	internal static ushort TruncateNarrow(this uint value) => (ushort)value;
	internal static uint TruncateNarrow(this ulong value) => (uint)value;

	internal static ushort Widen(this byte value) => (ushort)((value << 8) | value);

	internal static uint Widen(this ushort value) => (uint)((value << 16) | value);

	internal static ulong Widen(this uint value) => (ulong)((value << 32) | value);

	internal static byte Narrow(this ushort value) => (byte)(((uint)value + ((byte)sbyte.MaxValue) + 1) / 0x101U);

	internal static ushort Narrow(this uint value) => (ushort)(((ulong)value + ((ushort)short.MaxValue) + 1) / 0x101UL);

	internal static byte asUnsigned(this sbyte value) => (byte)value;

	internal static ushort asUnsigned(this short value) => (ushort)value;

	internal static uint asUnsigned(this int value) => (uint)value;

	internal static ulong asUnsigned(this long value) => (ulong)value;

	internal static sbyte asSigned(this byte value) => (sbyte)value;

	internal static short asSigned(this ushort value) => (short)value;

	internal static int asSigned(this uint value) => (int)value;

	internal static long asSigned(this ulong value) => (long)value;

	// actually should be in a floating point lib

	// Square Root
	internal static float Sqrt(this float v) => (float)Math.Sqrt(v);

	// Square Root
	internal static double Sqrt(this double v) => Math.Sqrt(v);

	// Square
	internal static float Square(this float v) => v * v;

	// Square
	internal static double Square(this double v) => v * v;

	// Linear Interpolation
	internal static float Lerp(this float x, float y, float s) => x * (1.0f - s) + y * s;

	// Linear Interpolation
	internal static float Lerp(this double x, float y, float s) => (float)(x * (1.0f - s) + y * s);

	// Linear Interpolation
	internal static float Lerp(this float x, double y, float s) => (float)(x * (1.0f - s) + y * s);

	// Linear Interpolation
	internal static float Lerp(this float x, float y, double s) => (float)(x * (1.0 - s) + y * s);

	// Linear Interpolation
	internal static double Lerp(this double x, double y, double s) => x * (1.0 - s) + y * s;
}
