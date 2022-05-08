using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Extensions;

internal static class Reinterpret {
	#region ReinterpretAs

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TTo ReinterpretAs<TFrom, TTo>(this TFrom value) where TFrom : struct where TTo : struct {
		Marshal.SizeOf<TTo>().AssertLessEqual(Marshal.SizeOf<TFrom>());
		return Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in value));
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe TTo ReinterpretAsUnsafe<TFrom, TTo>(this TFrom value) where TFrom : unmanaged where TTo : unmanaged {
		sizeof(TTo).AssertLessEqual(sizeof(TFrom));
		return *(TTo*)&value;
	}

	internal static TTo ReinterpretAs<TTo>(this bool value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<bool, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this byte value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<byte, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this sbyte value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<sbyte, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this ushort value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<ushort, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this short value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<short, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this uint value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<uint, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this int value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<int, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this ulong value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<ulong, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this long value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<long, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this half value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<half, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this float value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<float, TTo>(value);

	internal static TTo ReinterpretAs<TTo>(this double value) where TTo : unmanaged =>
		ReinterpretAsUnsafe<double, TTo>(value);

	#endregion

	#region ReinterpretAsRef

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ref TTo ReinterpretAsRef<TFrom, TTo>(in TFrom value) where TFrom : struct where TTo : struct {
		Marshal.SizeOf<TTo>().AssertLessEqual(Marshal.SizeOf<TFrom>());
		return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in value));
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ref TTo ReinterpretAsRefUnsafe<TFrom, TTo>(in TFrom value) where TFrom : unmanaged where TTo : unmanaged {
		sizeof(TTo).AssertLessEqual(sizeof(TFrom));
		return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(in value));
	}

	#endregion

	#region ReinterpretAsRef

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe Span<TTo> ReinterpretAsSpan<TFrom, TTo>(in TFrom value) where TFrom : struct where TTo : struct {
		Marshal.SizeOf<TTo>().AssertLessEqual(Marshal.SizeOf<TFrom>());
		return new(Unsafe.AsPointer(ref Unsafe.AsRef(in value)), Marshal.SizeOf<TFrom>() / Marshal.SizeOf<TTo>());
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe Span<TTo> ReinterpretAsSpanUnsafe<TFrom, TTo>(in TFrom value) where TFrom : unmanaged where TTo : unmanaged {
		sizeof(TTo).AssertLessEqual(sizeof(TFrom));
		return new(Unsafe.AsPointer(ref Unsafe.AsRef(in value)), sizeof(TFrom) / sizeof(TTo));
	}

	#endregion
}
