using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Garbage {

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void MarkCompact() {
			GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Collect(bool compact = false) {
			if (compact) {
				MarkCompact();
			}
			GC.Collect();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Mark(long size) {
			Contract.AssertPositiveOrZero(size);
			GC.AddMemoryPressure(size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Unmark(long size) {
			Contract.AssertPositiveOrZero(size);
			GC.RemoveMemoryPressure(size);
		}

		internal static void MarkOwned(SurfaceFormat format, int texels) {
			if (!Config.GarbageCollectAccountOwnedTexture)
				return;
			Contract.AssertPositiveOrZero(texels);
			var size = format.SizeBytes(texels);
			Mark(size);
		}

		internal static void UnmarkOwned (SurfaceFormat format, int texels) {
			if (!Config.GarbageCollectAccountOwnedTexture)
				return;
			Contract.AssertPositiveOrZero(texels);
			var size = format.SizeBytes(texels);
			Unmark(size);
		}

		internal static void MarkUnowned (SurfaceFormat format, int texels) {
			if (!Config.GarbageCollectAccountUnownedTextures)
				return;
			Contract.AssertPositiveOrZero(texels);
			var size = format.SizeBytes(texels);
			Mark(size);
		}

		internal static void UnmarkUnowned (SurfaceFormat format, int texels) {
			if (!Config.GarbageCollectAccountUnownedTextures)
				return;
			Contract.AssertPositiveOrZero(texels);
			var size = format.SizeBytes(texels);
			Unmark(size);
		}
	}
}
