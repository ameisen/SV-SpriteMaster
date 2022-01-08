using BCnEncoder.Encoder;
using SpriteMaster.Types;
using StbDxtSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace SpriteMaster.Resample.Encoder;

using TeximpQuality = TeximpNet.Compression.CompressionQuality;

static class StbBlockEncoder {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Span<byte> Encode(ReadOnlySpan<byte> data, ref TextureFormat format, Vector2I dimensions, bool hasAlpha, bool isPunchthroughAlpha, bool isMasky, bool hasR, bool hasG, bool hasB) {
		var textureFormat = BlockEncoderCommon.GetBestTextureFormat(hasAlpha, isPunchthroughAlpha, isMasky);

		switch ((XNA.Graphics.SurfaceFormat)textureFormat) {
			case XNA.Graphics.SurfaceFormat.Dxt5:
			case XNA.Graphics.SurfaceFormat.Dxt5SRgb:
			case XNA.Graphics.SurfaceFormat.Dxt1a:
				format = TextureFormat.BC3;
				return StbDxt.CompressDxt5(dimensions.Width, dimensions.Height, data.ToArray(), CompressionMode.HighQuality);

			case XNA.Graphics.SurfaceFormat.Dxt3:
			case XNA.Graphics.SurfaceFormat.Dxt3SRgb:
				format = TextureFormat.BC3;
				return StbDxt.CompressDxt5(dimensions.Width, dimensions.Height, data.ToArray(), CompressionMode.HighQuality | CompressionMode.Dithered);

			case XNA.Graphics.SurfaceFormat.Dxt1:
			case XNA.Graphics.SurfaceFormat.Dxt1SRgb:
				format = TextureFormat.BC1;
				return StbDxt.CompressDxt1(dimensions.Width, dimensions.Height, data.ToArray(), CompressionMode.HighQuality);

			default:
				throw new ArgumentException($"Format '{format}' is not a recognized compressible format");
		}
	}
}
