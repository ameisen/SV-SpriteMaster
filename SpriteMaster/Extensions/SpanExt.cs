using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Extensions;

static class SpanExt {
	internal static Span<T> MakeUninitialized<T>(int count, bool pinned = false) where T : struct => GC.AllocateUninitializedArray<T>(count, pinned: pinned);

	internal static Span<T> MakePinned<T>(int count) where T : struct => MakeUninitialized<T>(count, pinned: true);

	internal static unsafe Span<T> ToSpan<T>(this UnmanagedMemoryStream stream) where T : unmanaged => new Span<T>(stream.PositionPointer, (int)((stream.Length - stream.Position) / sizeof(T)));

	internal static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(this UnmanagedMemoryStream stream) where T : unmanaged => new ReadOnlySpan<T>(stream.PositionPointer, (int)((stream.Length - stream.Position) / sizeof(T)));
}
