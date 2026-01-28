using SpriteMaster.Types.Spans;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[StructLayout(LayoutKind.Auto)]
internal unsafe readonly struct PointerSpan<T> where T : unmanaged {
	internal readonly T* Pointer = null;
	internal readonly int Length = 0;

	internal readonly bool IsEmpty => Pointer is null;

	internal readonly ReadOnlySpan<T> AsReadOnlySpan => new(Pointer, Length);

	internal readonly Span<T> AsSpan => new(Pointer, Length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PointerSpan() {
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal PointerSpan(T* pointer, int length) {
		Pointer = pointer;
		Length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PointerSpan<T>(ReadOnlyPinnedSpan<T> pinnedSpan) => new(pinnedSpan.GetPointer(), pinnedSpan.Length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PointerSpan<T>(ReadOnlyPinnedSpan<T>.FixedSpan fixedSpan) => new(fixedSpan.Pointer, fixedSpan.Length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PointerSpan<T>(PinnedSpan<T> pinnedSpan) => new(pinnedSpan.GetPointer(), pinnedSpan.Length);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator PointerSpan<T>(PinnedSpan<T>.FixedSpan fixedSpan) => new(fixedSpan.Pointer, fixedSpan.Length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator T*(PointerSpan<T> pointerSpan) => pointerSpan.Pointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator nuint(PointerSpan<T> pointerSpan) => (nuint)pointerSpan.Pointer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator nint(PointerSpan<T> pointerSpan) => (nint)pointerSpan.Pointer;
}
