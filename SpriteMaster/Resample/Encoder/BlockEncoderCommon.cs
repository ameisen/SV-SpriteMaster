using TeximpNet.Compression;

namespace SpriteMaster.Resample.Encoder;

static class BlockEncoderCommon {
	internal static TextureFormat GetBestTextureFormat(bool hasAlpha, bool isPunchthroughAlpha, bool isMasky) =>
		(!hasAlpha) ?
			TextureFormat.WithNoAlpha :
			(
				(isPunchthroughAlpha && Config.Resample.BlockCompression.Quality != CompressionQuality.Fastest) ?
					TextureFormat.WithPunchthroughAlpha :
					isMasky ?
						TextureFormat.WithHardAlpha :
						TextureFormat.WithAlpha
			);
}
