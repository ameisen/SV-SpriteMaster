using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;
using TeximpNet.Compression;
using TeximpNet.DDS;

namespace SpriteMaster.Resample {
	internal static class BlockCompress {
		// We set this to false if block compression fails, as we assume that for whatever reason nvtt does not work on that system.
		private static bool BlockCompressionFunctional = true;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FlipColorBytes (byte[] p) {
			var span = new Span<byte>(p).As<uint>();
			foreach (int i in 0..span.Length) {
				var color = span[i];
				color =
					(color & 0xFF000000U) |
					(color & 0x0000FF00U) |
					((color & 0x00FF0000U) >> 16) |
					((color & 0x000000FFU) << 16);
				span[i] = color;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe byte[] Compress (byte[] data, ref TextureFormat format, Vector2I dimensions, bool HasAlpha, bool IsPunchThroughAlpha, bool IsMasky, bool HasR, bool HasG, bool HasB) {
			if (!BlockCompressionFunctional) {
				return null;
			}

			var oldSpriteFormat = format;

			FlipColorBytes(data);

			try {
				var bitmapData = data;

				using (var compressor = new Compressor()) {
					compressor.Input.AlphaMode = (HasAlpha) ? AlphaMode.Premultiplied : AlphaMode.None;
					compressor.Input.GenerateMipmaps = false;
					var textureFormat =
						(!HasAlpha) ?
							TextureFormat.NoAlpha :
							((false && IsPunchThroughAlpha && Config.Resample.BlockCompressionQuality != CompressionQuality.Fastest) ?
								TextureFormat.WithPunchthroughAlpha :
								(IsMasky ?
									TextureFormat.WithHardAlpha :
									TextureFormat.WithAlpha));
					compressor.Compression.Format = textureFormat;
					compressor.Compression.Quality = Config.Resample.BlockCompressionQuality;
					compressor.Compression.SetQuantization(true, true, IsPunchThroughAlpha);

					{
						compressor.Compression.GetColorWeights(out var r, out var g, out var b, out var a);
						a = HasAlpha ? (a * 20.0f) : 0.0f;
						// Relative luminance of the various channels.
						r = HasR ? (r * 0.2126f) : 0.0f;
						g = HasG ? (g * 0.7152f) : 0.0f;
						b = HasB ? (b * 0.0722f) : 0.0f;

						compressor.Compression.SetColorWeights(r, g, b, a);
					}

					compressor.Output.IsSRGBColorSpace = true;
					compressor.Output.OutputHeader = false;

					//public MipData (int width, int height, int rowPitch, IntPtr data, bool ownData = true)

					fixed (byte* p = bitmapData) {
						using (var mipData = new MipData(dimensions.Width, dimensions.Height, dimensions.Width * sizeof(int), (IntPtr)p, false)) {
							compressor.Input.SetData(mipData, true);
							var memoryBuffer = new byte[((SurfaceFormat)textureFormat).SizeBytes(dimensions.Area)];
							using (var stream = memoryBuffer.Stream()) {
								if (compressor.Process(stream)) {
									format = textureFormat;
									return memoryBuffer;
								}
								else {
									Debug.WarningLn($"Failed to use {(CompressionFormat)textureFormat} compression: " + compressor.LastErrorString);
								}
							}
						}
					}
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
				BlockCompressionFunctional = false;
			}
			format = oldSpriteFormat;
			FlipColorBytes(data);
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static unsafe bool Compress (ref byte[] data, ref TextureFormat format, Vector2I dimensions, bool HasAlpha, bool IsPunchThroughAlpha, bool IsMasky, bool HasR, bool HasG, bool HasB) {
			var oldFormat = format;

			try {
				// We do this ourselves because TexImpNet's allocator has an overflow bug which causes the conversion to fail if it converts it itself.
				var byteData = Compress(data, ref format, dimensions, HasAlpha, IsPunchThroughAlpha, IsMasky, HasR, HasG, HasB);
				if (byteData == null) {
					return false;
				}
				data = byteData;
				return true;
			}
			catch {
				format = oldFormat;
				return false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsBlockMultiple(int value) {
			return (value % 4) == 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsBlockMultiple (Vector2I value) {
			return IsBlockMultiple(value.X) && IsBlockMultiple(value.Y);
		}
	}
}
