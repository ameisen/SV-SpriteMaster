using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TeximpNet.Compression;
using TeximpNet.DDS;

namespace SpriteMaster.Resample;

static class BlockCompress {
	// A very simple implementation

	[DebuggerDisplay("[{X}, {Y}}")]
	[StructLayout(LayoutKind.Sequential, Pack = sizeof(ulong), Size = sizeof(ulong))]
	private ref struct Vector2U {
		internal uint X;
		internal uint Y;

		internal uint Width {
			readonly get => X;
			set => X = value;
		}
		internal uint Height {
			readonly get => Y;
			set => Y = value;
		}

		internal uint Area => X * Y;

		internal Vector2U(uint x, uint y) {
			X = x;
			Y = y;
		}

		internal Vector2U(Vector2I vec) : this((uint)vec.X, (uint)vec.Y) { }
		internal Vector2U(in (uint X, uint Y) vec) : this(vec.X, vec.Y) {}

		public static implicit operator Vector2U(Vector2I vec) => new(vec);
		public static implicit operator Vector2U(in (uint X, uint Y) vec) => new(vec);
		public static implicit operator Vector2I(Vector2U vec) => new((int)vec.X, (int)vec.Y);

		public override readonly string ToString() => $"{{{X}, {Y}}}";
	}

	// We set this to false if block compression fails, as we assume that for whatever reason nvtt does not work on that system.
	private static volatile bool BlockCompressionFunctional = true;

	private const int SwapIndex0 = 0;
	private const int SwapIndex1 = 2;
	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static unsafe void FlipColorBytes(byte* p, int length) {
		for (int i = 0; i < length; i += 4) {
			int index0 = i + SwapIndex0;
			int index1 = i + SwapIndex1;
			var temp = p[index0];
			p[index0] = p[index1];
			p[index1] = temp;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe byte[] Compress(byte[] data, ref TextureFormat format, Vector2I dimensions, bool HasAlpha, bool IsPunchThroughAlpha, bool IsMasky, bool HasR, bool HasG, bool HasB) {
		if (!BlockCompressionFunctional) {
			return null;
		}

		var oldSpriteFormat = format;
		fixed (byte* p = data) {
			try {
				//FlipColorBytes(p, data.Length);

				using var compressor = new Compressor();
				compressor.Input.AlphaMode = (HasAlpha) ? AlphaMode.Premultiplied : AlphaMode.None;
				compressor.Input.GenerateMipmaps = false;
				var textureFormat =
					(!HasAlpha) ?
						TextureFormat.WithNoAlpha :
						((false && IsPunchThroughAlpha && Config.Resample.BlockCompression.Quality != CompressionQuality.Fastest) ?
							TextureFormat.WithPunchthroughAlpha :
							(IsMasky ?
								TextureFormat.WithHardAlpha :
								TextureFormat.WithAlpha));
				compressor.Compression.Format = textureFormat;
				compressor.Compression.Quality = Config.Resample.BlockCompression.Quality;
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

				using var mipData = new MipData(dimensions.Width, dimensions.Height, dimensions.Width * sizeof(int), (IntPtr)p, false);
				compressor.Input.SetData(mipData, false);
				var memoryBuffer = new byte[((SurfaceFormat)textureFormat).SizeBytes(dimensions.Area)];
				using var stream = memoryBuffer.Stream();
				if (compressor.Process(stream)) {
					format = textureFormat;
					return memoryBuffer;
				}
				else {
					Debug.WarningLn($"Failed to use {(CompressionFormat)textureFormat} compression: " + compressor.LastErrorString);
					Debug.WarningLn($"Dimensions: [{dimensions.Width}, {dimensions.Height}]");
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
				BlockCompressionFunctional = false;
			}
			format = oldSpriteFormat;
			//FlipColorBytes(p, data.Length);
		}
		return null;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Compress(ref byte[] data, ref TextureFormat format, Vector2I dimensions, bool HasAlpha, bool IsPunchThroughAlpha, bool IsMasky, bool HasR, bool HasG, bool HasB) {
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

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsBlockMultiple(int value) => (value & 3) == 0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsBlockMultiple(uint value) => (value & 3) == 0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsBlockMultiple(Vector2I value) => IsBlockMultiple(value.X | value.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool IsBlockMultiple(Vector2U value) => IsBlockMultiple(value.X | value.Y);

	// https://www.khronos.org/opengl/wiki/S3_Texture_Compression
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
	private unsafe struct ColorBlock {
		/* The endianness of the data in the documentation is a bit confusing to me. */

		// Color should appear as BGR, 565.
		[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
		private unsafe readonly struct Color565 {
			[FieldOffset(0)]
			internal readonly ushort Packed;

			internal readonly uint PackedInt => Packed;

			internal Color565(ushort packed) {
				Packed = packed;
			}

			private enum Mask : uint {
				M5 = (1U << 5) - 1U,
				M6 = (1U << 6) - 1U,

				B = (BitSize.B == 5) ? M5 : M6,
				G = (BitSize.G == 5) ? M5 : M6,
				R = (BitSize.R == 5) ? M5 : M6,
			}

			private enum Multiplier : uint {
				M5 = 255U / Mask.M5,
				M6 = 255U / Mask.M6,

				B = (BitSize.B == 5) ? M5 : M6,
				G = (BitSize.G == 5) ? M5 : M6,
				R = (BitSize.R == 5) ? M5 : M6,
			}

			private enum BitSize : uint {
				B = 5,
				G = 6,
				R = 5
			}

			private enum Offset : int {
				B = 0,
				G = B + (int)BitSize.B,
				R = G + (int)BitSize.G
			}

			private readonly uint PackedB => (PackedInt >> (int)Offset.B) & (uint)Mask.B;
			private readonly uint PackedG => (PackedInt >> (int)Offset.G) & (uint)Mask.G;
			private readonly uint PackedR => (PackedInt >> (int)Offset.R) & (uint)Mask.R;


			internal readonly byte B => (byte)(((uint)Multiplier.B * PackedB) & 0xFF);
			internal readonly byte G => (byte)(((uint)Multiplier.G * PackedG) & 0xFF);
			internal readonly byte R => (byte)(((uint)Multiplier.R * PackedR) & 0xFF);

			// https://stackoverflow.com/a/2442609
			internal readonly uint AsPacked => 
				(uint)B << 16 |
				(uint)G << 8 |
				(uint)R;
		}

		[FieldOffset(0)]
		private readonly Color565 color0;
		[FieldOffset(2)]
		private readonly Color565 color1;
		[FieldOffset(4)]
		private fixed byte codes[4];

		private readonly byte GetCode(Vector2U position) {
			var code = codes[position.Y];
			return (byte)((byte)(code >> (int)(position.X << 1)) & 0b11);
		}

		private static uint Pack(uint b, uint g, uint r) =>
			(b << 16) |
			(g << 8) |
			r;

		// Returns the color, hopefully, as ABGR
		internal readonly uint GetColor(Vector2U position) {
			if (color0.Packed > color1.Packed) {
				var code = GetCode(position);
				switch (code) {
					case 0:
						return color0.AsPacked;
					case 1:
						return color1.AsPacked;
					case 2: {
							var b = ((uint)(2U * color0.B + color1.B)) / 3U;
							var g = ((uint)(2U * color0.G + color1.G)) / 3U;
							var r = ((uint)(2U * color0.R + color1.R)) / 3U;
							return Pack(b, g, r);
						}
					default:
					case 3: {
							var b = ((uint)(color0.B + 2U * color1.B)) / 3U;
							var g = ((uint)(color0.G + 2U * color1.G)) / 3U;
							var r = ((uint)(color0.R + 2U * color1.R)) / 3U;
							return Pack(b, g, r);
						}
				}
			}
			else {
				return GetColorDXT3(position);
			}
		}

		internal readonly uint GetColorDXT3(Vector2U position) {
			var code = GetCode(position);
			switch (code) {
				case 0:
					return color0.AsPacked;
				case 1:
					return color1.AsPacked;
				case 2: {
						var b = ((uint)(color0.B + color1.B)) >> 1;
						var g = ((uint)(color0.G + color1.G)) >> 1;
						var r = ((uint)(color0.R + color1.R)) >> 1;
						return Pack(b, g, r);
					}
				default:
				case 3: {
						return 0U;
					}
			}
		}
	}

	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
	private unsafe struct ColorBlockDxt3 {
		[FieldOffset(0)]
		private fixed ushort AlphaBlock[4];
		[FieldOffset(8)]
		private readonly ColorBlock Color;

		internal readonly uint GetColor(Vector2U position) {
			var alphaValue = (byte)((AlphaBlock[position.Y] >> (int)(position.X * 4)) & 0b1111);
			uint alpha = ((uint)alphaValue * 0b10001u) << 24;
			return Color.GetColorDXT3(position) | alpha;
		}
	}

	internal static Span<byte> Decompress(ReadOnlySpan<byte> data, SpriteInfo info) => Decompress(
		data,
		info.ReferenceSize,
		info.Reference.Format
	);

	internal static Span<byte> Decompress(ReadOnlySpan<byte> data, Vector2I size, SurfaceFormat format) {
		Vector2U uSize = size;

		if (!IsBlockMultiple(uSize)) {
			throw new ArgumentException($"{nameof(size)}: {uSize} not block multiple");
		}

		switch (format) {
			case SurfaceFormat.Dxt1: {
					var blocks = data.Cast<byte, ColorBlock>();
					var outData = SpanExt.MakePinned<byte>((int)uSize.Area);
					var outDataPacked = outData.Cast<byte, uint>();

					var widthBlocks = uSize.Width >> 2;

					uint blockIndex = 0;
					foreach (var block in blocks) {
						var index = blockIndex++;
						var xOffset = (uint)((index & widthBlocks - 1)) << 2;
						var yOffset = (uint)(index / widthBlocks) << 2;

						for (uint y = 0; y < 4; ++y) {
							var yOffsetInternal = yOffset + y;
							for (uint x = 0; x < 4; ++x) {
								var xOffsetInternal = xOffset + x;
								var offset = (yOffsetInternal * uSize.Width) + xOffsetInternal;
								outDataPacked[(int)offset] = block.GetColor((x, y)) | 0xFF000000U;
							}
						}
					}

					return outData;
				}
				break;
			case SurfaceFormat.Dxt3: {
					var blocks = data.Cast<byte, ColorBlockDxt3>();
					var outData = SpanExt.MakePinned<byte>((int)uSize.Area * sizeof(uint));
					var outDataPacked = outData.Cast<byte, uint>();

					var widthBlocks = uSize.Width >> 2;

					uint blockIndex = 0;
					foreach (var block in blocks) {
						var index = blockIndex++;
						var xOffset = (uint)((index & widthBlocks - 1)) << 2;
						var yOffset = (uint)(index / widthBlocks) << 2;

						for (uint y = 0; y < 4; ++y) {
							var yOffsetInternal = yOffset + y;
							for (uint x = 0; x < 4; ++x) {
								var xOffsetInternal = xOffset + x;
								var offset = (yOffsetInternal * uSize.Width) + xOffsetInternal;
								outDataPacked[(int)offset] = block.GetColor((x, y));
							}
						}
					}

					return outData;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(format));
		}
	}
}
