using Microsoft.Xna.Framework.Graphics;
using Pastel;
using SpriteMaster.Resample;
using SpriteMaster.Types;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Extensions;

static class Textures {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static int Area(this Texture2D texture) => texture.Width * texture.Height;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2I Extent(this Texture2D texture) => new(texture.Width, texture.Height);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static long SizeBytes(this SurfaceFormat format, int texels) {
		switch (format) {
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt1SRgb:
			case SurfaceFormat.Dxt1a:
			case var _ when format == TextureFormat.DXT1a:
				return texels / 2;
		}

		long elementSize = format switch {
			SurfaceFormat.Color => 4,
			SurfaceFormat.Bgr565 => 2,
			SurfaceFormat.Bgra5551 => 2,
			SurfaceFormat.Bgra4444 => 2,
			SurfaceFormat.Dxt3 => 1,
			SurfaceFormat.Dxt3SRgb => 1,
			SurfaceFormat.Dxt5 => 1,
			SurfaceFormat.Dxt5SRgb => 1,
			SurfaceFormat.NormalizedByte2 => 2,
			SurfaceFormat.NormalizedByte4 => 4,
			SurfaceFormat.Rgba1010102 => 4,
			SurfaceFormat.Rg32 => 4,
			SurfaceFormat.Rgba64 => 8,
			SurfaceFormat.Alpha8 => 1,
			SurfaceFormat.Single => 4,
			SurfaceFormat.Vector2 => 8,
			SurfaceFormat.Vector4 => 16,
			SurfaceFormat.HalfSingle => 2,
			SurfaceFormat.HalfVector2 => 4,
			SurfaceFormat.HalfVector4 => 8,
			_ => throw new ArgumentException(nameof(format))
		};

		return texels * elementSize;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsBlock(this SurfaceFormat format) {
		switch (format) {
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt1SRgb:
			case SurfaceFormat.Dxt3:
			case SurfaceFormat.Dxt3SRgb:
			case SurfaceFormat.Dxt5:
			case SurfaceFormat.Dxt5SRgb:
			case SurfaceFormat.Dxt1a:
				return true;
			default:
				return false;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static int BlockEdge(this SurfaceFormat format) {
		switch (format) {
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt1SRgb:
			case SurfaceFormat.Dxt3:
			case SurfaceFormat.Dxt3SRgb:
			case SurfaceFormat.Dxt5:
			case SurfaceFormat.Dxt5SRgb:
			case SurfaceFormat.Dxt1a:
				return 4;
			default:
				return 1;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static long SizeBytes(this Texture2D texture) => texture.Format.SizeBytes(texture.Area());

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static long SizeBytes(this ManagedTexture2D texture) => (long)texture.Area() * 4;

	internal static TeximpNet.Surface Resize(this TeximpNet.Surface source, in Vector2I size, TeximpNet.ImageFilter filter = TeximpNet.ImageFilter.Lanczos3, bool discard = true) {
		if (size == new Vector2I(source)) {
			try {
				return source.Clone();
			}
			finally {
				if (discard) {
					source.Dispose();
				}
			}
		}

		var output = source.Clone();
		try {
			if (!output.Resize(size.Width, size.Height, filter)) {
				throw new Exception("Failed to resize surface");
			}

			if (discard) {
				source.Dispose();
			}

			return output;
		}
		catch {
			output.Dispose();
			throw;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Anonymous(this Texture2D texture) => texture.Name.IsBlank();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Anonymous(this ScaledTexture texture) => texture.Name.IsBlank();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this string name) => name.IsBlank() ? "Unknown" : name.Replace('\\', '/').Replace("//", "/");
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this string name, in DrawingColor color) => (name.IsBlank() ? "Unknown" : name.Replace('\\', '/').Replace("//", "/")).Pastel(color);
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this Texture2D texture) => texture.Name.SafeName();
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this Texture2D texture, in DrawingColor color) => texture.Name.SafeName(in color);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this ScaledTexture texture) => texture.Name.SafeName();
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string SafeName(this ScaledTexture texture, in DrawingColor color) => texture.Name.SafeName(in color);

	private const int ImageElementSize = 4;

	internal static unsafe void DumpTexture<T>(string path, T[] source, in Vector2I sourceSize, in double? adjustGamma = null, in Bounds? destBounds = null, in (int i0, int i1, int i2, int i3)? swap = null) where T : unmanaged {
		using var fixedSpan = source.AsFixedSpan();
		DumpTexture<T>(path, fixedSpan, sourceSize, adjustGamma, destBounds, swap);
	}

	internal static unsafe void DumpTexture<T>(string path, in FixedSpan<T> source, in Vector2I sourceSize, in double? adjustGamma = null, in Bounds? destBounds = null, in (int i0, int i1, int i2, int i3)? swap = null) where T : unmanaged {
		T[] subData;
		Bounds destBound;
		if (destBounds.HasValue) {
			destBound = destBounds.Value;
			subData = GC.AllocateUninitializedArray<T>(destBound.Area, pinned: true);
			uint sourceStride = (uint)(sourceSize.Width * 4);
			uint destStride = (uint)(destBound.Width * 4);
			uint sourceOffset = (uint)((sourceStride * destBound.Top) + (destBound.Left * 4));
			uint destOffset = 0;
			byte* sourcePtr = (byte*)source.TypedPointer;
			fixed (T* dataPtrT = subData) {
				byte* dataPtr = (byte*)dataPtrT;

				for (int y = 0; y < destBound.Height; ++y) {
					Unsafe.CopyBlock(
						dataPtr + destOffset,
						sourcePtr + sourceOffset,
						destStride
					);
					destOffset += destStride;
					sourceOffset += sourceStride;
				}
			}
		}
		else {
			subData = source.ToArray();
			destBound = sourceSize;
		}

		SynchronizedTasks.AddPendingAction(() => {
			using var dumpTexture = new Texture2D(
				StardewValley.Game1.graphics.GraphicsDevice,
				destBound.Width,
				destBound.Height,
				mipmap: false,
				format: SurfaceFormat.Color
			);
			dumpTexture.SetData(subData);
			using var dumpFile = File.Create(path);
			dumpTexture.SaveAsPng(dumpFile, destBound.Width, destBound.Height);
		});
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsValid(this ScaledTexture texture) {
		return texture != null && texture.Texture != null && !texture.Texture.IsDisposed;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsDisposed(this ScaledTexture texture) {
		return texture == null || (texture.Texture != null && texture.Texture.IsDisposed);
	}
}
