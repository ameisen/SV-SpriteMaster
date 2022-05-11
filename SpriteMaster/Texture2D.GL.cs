using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;
using SpriteMaster.Extensions;
using SpriteMaster.Harmonize;
using SpriteMaster.Types;
using SpriteMaster.Types.Spans;
using System;
using System.Runtime.InteropServices;
using static Microsoft.Xna.Framework.Graphics.Texture2D;

namespace SpriteMaster;

internal static class Texture2DGL {
	private static class GLExt {
		private static class Generic<T> where T : class? {
			internal delegate T? LoadFunctionDelegate(string function, bool throwIfNotFound = false);

			internal static readonly LoadFunctionDelegate LoadFunction =
				typeof(MonoGame.OpenGL.GL).GetStaticMethod("LoadFunction")?.
					MakeGenericMethod(typeof(T)).CreateDelegate<LoadFunctionDelegate>() ??
					((_, _) => null);
		}

		[System.Security.SuppressUnmanagedCodeSecurity]
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		[MonoNativeFunctionWrapper]
		internal delegate void TexStorage2DDelegate(
			TextureTarget target,
			int levels,
			PixelInternalFormat internalFormat,
			int width,
			int height
		);

		internal static readonly TexStorage2DDelegate? TexStorage2D =
			Generic<TexStorage2DDelegate>.LoadFunction("glTexStorage2D");
	}

	internal static void SetDataInternal(
		Texture2D @this,
		int level,
		Bounds? rect,
		ReadOnlyPinnedSpan<byte> data,
		bool useSubImage = true
 	) {
		rect ??= (@this.Extent() >> level).Max(1);

		// TODO : check if the texture has been initialized yet

		if (@this.glFormat == PixelFormat.CompressedTextureFormats) {
			if (useSubImage) {
				MonoGame.OpenGL.GL.CompressedTexSubImage2D(
					TextureTarget.Texture2D,
					level,
					rect.Value.X,
					rect.Value.Y,
					rect.Value.Width,
					rect.Value.Height,
					@this.glInternalFormat,
					data.Length,
					data.GetIntPointer()
				);
			}
			else {
				MonoGame.OpenGL.GL.CompressedTexImage2D(
					TextureTarget.Texture2D,
					level,
					@this.glInternalFormat,
					rect.Value.Width,
					rect.Value.Height,
					0,
					data.Length,
					data.GetIntPointer()
				);
			}
		}
		else {
			if (useSubImage) {
				MonoGame.OpenGL.GL.TexSubImage2D(
					TextureTarget.Texture2D,
					level,
					rect.Value.X,
					rect.Value.Y,
					rect.Value.Width,
					rect.Value.Height,
					@this.glFormat,
					@this.glType,
					data.GetIntPointer()
				);
			}
			else {
				MonoGame.OpenGL.GL.TexImage2D(
					TextureTarget.Texture2D,
					level,
					@this.glInternalFormat,
					rect.Value.Width,
					rect.Value.Height,
					0,
					@this.glFormat,
					@this.glType,
					data.GetIntPointer()
				);
			}
		}

		GraphicsExtensions.CheckGLError();
	}

	internal static void Construct(
		Texture2D @this,
		ReadOnlyPinnedSpan<byte>.FixedSpan dataIn,
		Vector2I size,
		bool mipmap,
		SurfaceFormat format,
		SurfaceType type,
		bool shared
	) {
		@this.glTarget = TextureTarget.Texture2D;
		format.GetGLFormat(@this.GraphicsDevice, out @this.glInternalFormat, out @this.glFormat, out @this.glType);

		// Use glTexStorage2D if it's available.
		// Presently, since we are not yet overriding 'SetData' to use glMeowTexSubImage2D,
		// only use it if we are populating the texture now
		bool useSubImage = !dataIn.IsEmpty && GLExt.TexStorage2D is not null;

		// Calculate the number of texture levels
		int levels = 1;
		if (useSubImage) {
			if (mipmap) {
				var tempDimensions = size;
				while (tempDimensions != (1, 1)) {
					tempDimensions >>= 1;
					tempDimensions = tempDimensions.Min(1);
					++levels;
				}
			}
		}

		// Mostly taken from MonoGame, but completely refactored.
		// Returns the size given dimensions, adjusted/aligned for block formats.
		Func<Vector2I, int> getLevelSize = format switch {
			// PVRTC has explicit calculations for imageSize
			// https://www.khronos.org/registry/OpenGL/extensions/IMG/IMG_texture_compression_pvrtc.txt
			SurfaceFormat.RgbPvrtc2Bpp or
			SurfaceFormat.RgbaPvrtc2Bpp =>
				static size => {
					var maxDimensions = size.Max((16, 8));
					return ((maxDimensions.X * maxDimensions.Y) << 1 + 7) >> 3;
				}
			,

			SurfaceFormat.RgbPvrtc4Bpp or
			SurfaceFormat.RgbaPvrtc4Bpp =>
				static size => {
					var maxDimensions = size.Max((8, 8));
					return ((maxDimensions.X * maxDimensions.Y) << 2 + 7) >> 3;
				}
			,

			_ when @this.glFormat == PixelFormat.CompressedTextureFormats =>
				size => {
					int blockSize = format.GetSize();
					var blockDimensions = format.BlockEdge();

					var blocks = (size + (blockDimensions - 1)) / blockDimensions;
					return blocks.X * blocks.Y * blockSize;
				}
			,

			_ =>
				size => (int)format.SizeBytes(size.Area)
		};

		Threading.BlockOnUIThread(() => {
			ReadOnlyPinnedSpan<byte> data = default;

			if (!dataIn.IsEmpty) {
				data = dataIn.AsSpan;
			}

			@this.GenerateGLTextureIfRequired();

			if (!data.IsEmpty) {
				MonoGame.OpenGL.GL.PixelStore(PixelStoreParameter.UnpackAlignment, Math.Min(@this.Format.GetSize(), 8));
			}

			if (useSubImage) {
				// https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glTexStorage2D.xhtml
				// Using glTexStorage and glMeowTexSubImage2D to populate textures is more efficient,
				// as the way that MonoGame normally does it requires the texture to be kept largely in flux,
				// and also requires it to be discarded a significant number of times.
				GLExt.TexStorage2D!(
					TextureTarget.Texture2D,
					levels,
					@this.glInternalFormat,
					size.Width,
					size.Height
				);
			}

			if (!dataIn.IsEmpty || !useSubImage) {
				var levelDimensions = size;
				int level = 0;
				int currentOffset = 0;

				// Loop over every level and populate it, starting from the largest.
				while (true) {
					int levelSize = getLevelSize(levelDimensions);
					SetDataInternal(
						@this: @this,
						level: level++,
						rect: null,
						data: data.Slice(currentOffset, levelSize),
						useSubImage: useSubImage
					);
					currentOffset += levelSize;

					if (levelDimensions == (1, 1) || !mipmap)
						break;

					levelDimensions >>= 1;
					levelDimensions = levelDimensions.Min(1);
					++level;
				}
			}
		});
	}
}
