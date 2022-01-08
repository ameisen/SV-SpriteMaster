using BCnEncoder.Encoder;
using BCnEncoder.Shared;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Resample.Encoder;

using TeximpQuality = TeximpNet.Compression.CompressionQuality;

static class BCnBlockEncoder {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static CompressionQuality Convert(this TeximpQuality quality) => quality switch {
		TeximpQuality.Fastest => CompressionQuality.Fast,
		TeximpQuality.Normal => CompressionQuality.Balanced,
		TeximpQuality.Production => CompressionQuality.BestQuality,
		TeximpQuality.Highest => CompressionQuality.BestQuality,
		_ => CompressionQuality.BestQuality
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static CompressionFormat Convert(this TextureFormat format) => (XNA.Graphics.SurfaceFormat)format switch {
		XNA.Graphics.SurfaceFormat.Dxt5 => CompressionFormat.Bc3,
		XNA.Graphics.SurfaceFormat.Dxt5SRgb => CompressionFormat.Bc3,
		XNA.Graphics.SurfaceFormat.Dxt3 => CompressionFormat.Bc2,
		XNA.Graphics.SurfaceFormat.Dxt3SRgb => CompressionFormat.Bc2,
		XNA.Graphics.SurfaceFormat.Dxt1a => CompressionFormat.Bc1WithAlpha,
		XNA.Graphics.SurfaceFormat.Dxt1 => CompressionFormat.Bc1,
		XNA.Graphics.SurfaceFormat.Dxt1SRgb => CompressionFormat.Bc1,
		_ => throw new ArgumentException($"Format '{format}' is not a recognized compressible format")
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static PixelFormat InputConvert(this TextureFormat format) => (XNA.Graphics.SurfaceFormat)format switch {
		XNA.Graphics.SurfaceFormat.Color => PixelFormat.Rgba32,
		XNA.Graphics.SurfaceFormat.ColorSRgb => PixelFormat.Rgba32,
		XNA.Graphics.SurfaceFormat.Bgra32 => PixelFormat.Argb32,
		XNA.Graphics.SurfaceFormat.Bgra32SRgb => PixelFormat.Argb32,
		XNA.Graphics.SurfaceFormat.Bgr32 => PixelFormat.Argb32,
		XNA.Graphics.SurfaceFormat.Bgr32SRgb => PixelFormat.Argb32,
		_ => throw new ArgumentException($"Format '{format}' is not a recognized pixel format")
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Span<byte> Encode(ReadOnlySpan<byte> data, ref TextureFormat format, Vector2I dimensions, bool hasAlpha, bool isPunchthroughAlpha, bool isMasky, bool hasR, bool hasG, bool hasB) {
		var textureFormat = BlockEncoderCommon.GetBestTextureFormat(hasAlpha, isPunchthroughAlpha, isMasky);

		var encoder = new BcEncoder();
		encoder.OutputOptions.GenerateMipMaps = false;
		encoder.OutputOptions.Quality = Config.Resample.BlockCompression.Quality.Convert();
		encoder.OutputOptions.Format = textureFormat.Convert();
		encoder.Options.IsParallel = false;
		var result = encoder.EncodeToRawBytes(data, dimensions.Width, dimensions.Height, format.InputConvert());
		format = textureFormat;
		return result[0];
	}
}
