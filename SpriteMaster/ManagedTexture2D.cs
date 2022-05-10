using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using SpriteMaster.Types.Spans;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpriteMaster;

internal sealed class ManagedTexture2D : InternalTexture2D {
	private static ulong TotalAllocatedSize = 0L;
	private static volatile uint TotalManagedTextures = 0;
	private const bool UseMips = false;
	private const bool UseShared = false;

	internal readonly WeakReference<XTexture2D> Reference;
	internal readonly ManagedSpriteInstance SpriteInstance;
	internal readonly Vector2I Dimensions;
	private volatile bool Disposed = false;

	internal static void DumpStats(List<string> output) {
		output.AddRange(new[]{
			"\tManagedTexture2D:",
			$"\t\tTotal Managed Textures : {TotalManagedTextures}",
			$"\t\tTotal Texture Size     : {Interlocked.Read(ref TotalAllocatedSize).AsDataSize()}"
			});
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ManagedTexture2D(
		ReadOnlyPinnedSpan<byte>.FixedSpan data,
		ManagedSpriteInstance instance,
		XTexture2D reference,
		Vector2I dimensions,
		SurfaceFormat format,
		string? name = null
	) : base(
		graphicsDevice: reference.GraphicsDevice.IsDisposed ? DrawState.Device : reference.GraphicsDevice,
		width: dimensions.Width,
		height: dimensions.Height, 
		mipmap: UseMips,
		format: format,
		type: SurfaceType.SwapChainRenderTarget, // this prevents the texture from being constructed immediately
		shared: UseShared,
		arraySize: 1
	) {
		Construct(data, dimensions, UseMips, format, SurfaceType.Texture, UseShared);

		Name = name ?? $"{reference.NormalizedName()} [internal managed <{format}>]";

		Reference = reference.MakeWeak();
		SpriteInstance = instance;
		Dimensions = dimensions - instance.BlockPadding;

		reference.Disposing += OnParentDispose;

		Interlocked.Add(ref TotalAllocatedSize, (ulong)this.SizeBytes());
		Interlocked.Increment(ref TotalManagedTextures);

		Garbage.MarkOwned(format, dimensions.Area);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	~ManagedTexture2D() {
		if (!IsDisposed) {
			//Debug.Error($"Memory leak: ManagedTexture2D '{Name}' was finalized without the Dispose method called");
			Dispose(false);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void OnParentDispose(object? resource, EventArgs args) => OnParentDispose(resource as XTexture2D);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void OnParentDispose(XTexture2D? referenceTexture) {
		if (!IsDisposed) {
			Debug.Trace($"Disposing ManagedTexture2D '{Name}'");
			Dispose();
		}

		referenceTexture?.Meta().Dispose();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);

		if (Disposed) {
			return;
		}
		Disposed = true;

		if (Reference.TryGet(out var reference)) {
			reference.Disposing -= OnParentDispose;
		}

		Garbage.UnmarkOwned(Format, Width * Height);
		Interlocked.Add(ref TotalAllocatedSize, (ulong)-this.SizeBytes());
		Interlocked.Decrement(ref TotalManagedTextures);
	}

	private unsafe void Construct<T>(
		ReadOnlyPinnedSpan<T>.FixedSpan dataIn,
		Vector2I size,
		bool mipmap,
		SurfaceFormat format,
		SurfaceType type,
		bool shared
	) where T : unmanaged {
		glTarget = TextureTarget.Texture2D;
		format.GetGLFormat(GraphicsDevice, out glInternalFormat, out glFormat, out glType);

		Threading.BlockOnUIThread(() => {
			ReadOnlyPinnedSpan<byte> data = default;

			if (!dataIn.IsEmpty) {
				data = dataIn.AsSpan.Cast<T, byte>();
			}

			GenerateGLTextureIfRequired();

			if (!data.IsEmpty) {
				MonoGame.OpenGL.GL.PixelStore(PixelStoreParameter.UnpackAlignment, Math.Min(Format.GetSize(), 8));
			}

			Vector2I dimensions = size;
			int level = 0;
			int currentOffset = 0;

			while (true) {
				int levelSize;
				if (glFormat == PixelFormat.CompressedTextureFormats) {
					int imageSize;
					switch (format) {
						// PVRTC has explicit calculations for imageSize
						// https://www.khronos.org/registry/OpenGL/extensions/IMG/IMG_texture_compression_pvrtc.txt
						case SurfaceFormat.RgbPvrtc2Bpp:
						case SurfaceFormat.RgbaPvrtc2Bpp: {
							var maxDimensions = dimensions.Max((16, 8));
							imageSize = ((maxDimensions.X * maxDimensions.Y) << 1 + 7) >> 3;
							break;
						}
						case SurfaceFormat.RgbPvrtc4Bpp:
						case SurfaceFormat.RgbaPvrtc4Bpp: {
							var maxDimensions = dimensions.Max((8, 8));
							imageSize = ((maxDimensions.X * maxDimensions.Y) << 2 + 7) >> 3;
							break;
						}
						default:
						{
							int blockSize = format.GetSize();
							Vector2I blockDimensions = format.BlockEdge();

							Vector2I blocks = (dimensions + (blockDimensions - 1)) / blockDimensions;
							imageSize = blocks.X * blocks.Y * blockSize;
							break;
						}
					}

					levelSize = (int)format.SizeBytes(dimensions.Area);

					IntPtr dataPtr = data.IsEmpty ? IntPtr.Zero : (IntPtr)Unsafe.AsPointer(ref data.Slice(currentOffset, levelSize).GetPinnableReferenceUnsafe());

					MonoGame.OpenGL.GL.CompressedTexImage2D(
						TextureTarget.Texture2D,
						level,
						glInternalFormat, 
						dimensions.X,
						dimensions.Y,
						0,
						imageSize,
						dataPtr
					);
					GraphicsExtensions.CheckGLError();
				}
				else {
					levelSize = (int)format.SizeBytes(dimensions.Area);

					IntPtr dataPtr = data.IsEmpty ? IntPtr.Zero : (IntPtr)Unsafe.AsPointer(ref data.Slice(currentOffset, levelSize).GetPinnableReferenceUnsafe());

					MonoGame.OpenGL.GL.TexImage2D(
						TextureTarget.Texture2D,
						level,
						glInternalFormat, 
						dimensions.X, 
						dimensions.Y, 
						0,
						glFormat,
						glType,
						dataPtr
					);
					GraphicsExtensions.CheckGLError();
				}

				if (dimensions == (1, 1) || !mipmap)
					break;

				currentOffset += levelSize;

				if (dimensions.X > 1)
					dimensions.X >>= 1;
				if (dimensions.Y > 1)
					dimensions.Y >>= 1;
				++level;
			}
		});
	}
}
