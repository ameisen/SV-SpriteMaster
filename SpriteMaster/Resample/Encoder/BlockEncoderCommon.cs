namespace SpriteMaster.Resample.Encoder;

internal static class BlockEncoderCommon {
	internal static TextureFormat GetBestTextureFormat(bool hasAlpha, bool isPunchthroughAlpha, bool isMasky) =>
		(!hasAlpha) ?
			TextureFormat.WithNoAlpha :
			(
				(isPunchthroughAlpha && SMConfig.Resample.BlockCompression.Quality != CompressionQuality.High) ?
					TextureFormat.WithPunchthroughAlpha :
					isMasky ?
						TextureFormat.WithHardAlpha :
						TextureFormat.WithAlpha
			);
}
