using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;

namespace SpriteMaster.Resample;

internal static class TextureDecode {
	private static readonly DecodeDelegate[] PreferredDecoders = new DecodeDelegate[] {
		Decoder.MonoBlockDecoder.Decode,
		//Decoder.GraphicsBlockDecoder.Decode,
		Decoder.InternalBlockDecoder.Decode
	};

	private delegate Span<byte> DecodeDelegate(ReadOnlySpan<byte> data, Vector2I size, SurfaceFormat format);

	internal static Span<byte> DecodeBlockCompressedTexture(SurfaceFormat format, Vector2I size, ReadOnlySpan<byte> data) {
		if (data.IsEmpty) {
			return Span<byte>.Empty;
		}

		foreach (var decoder in PreferredDecoders) {
			try {
				var result = decoder(data, size, format);
				if (!result.IsEmpty) {
					return result;
				}
			}
			catch {
				// ignored
			}
		}

		throw new InvalidOperationException("Failed to decode compressed texture data");
	}
}
