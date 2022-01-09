using System;
using System.IO;
using System.Runtime.InteropServices;

#nullable enable

namespace SpriteMaster.Extensions;

static class SpanExt {
	internal static Span<T> MakeUninitialized<T>(int count, bool pinned = false) where T : struct => GC.AllocateUninitializedArray<T>(count, pinned: pinned);

	internal static Span<T> MakePinned<T>(int count) where T : struct => MakeUninitialized<T>(count, pinned: true);

	internal static unsafe Span<T> ToSpan<T>(this UnmanagedMemoryStream stream) where T : unmanaged => new Span<T>(stream.PositionPointer, (int)((stream.Length - stream.Position) / sizeof(T)));

	internal static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(this UnmanagedMemoryStream stream) where T : unmanaged => new ReadOnlySpan<T>(stream.PositionPointer, (int)((stream.Length - stream.Position) / sizeof(T)));

	internal static Span<T> ToSpanUnsafe<T>(this ReadOnlySpan<T> span) => MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), span.Length);

	internal static void CopyTo<T>(this Span<T> inSpan, Span<T> outSpan, int inOffset, int outOffset, int count) =>
		inSpan.Slice(inOffset, count).CopyTo(outSpan.Slice(outOffset, count));

	internal static void CopyTo<T>(this ReadOnlySpan<T> inSpan, Span<T> outSpan, int inOffset, int outOffset, int count) =>
		inSpan.Slice(inOffset, count).CopyTo(outSpan.Slice(outOffset, count));
}
