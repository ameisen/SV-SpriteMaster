using JetBrains.Annotations;
using SpriteMaster.Resample;
using SpriteMaster.Types;
using System.Drawing;
using Root = SpriteMaster;

namespace SpriteMaster;

[PublicAPI]
public static class SpriteMaster {
	[PublicAPI]
	public readonly ref struct Result {
		public ReadOnlySpan<byte> Data { get; init; }
		public Size Extent { get; init; }
		public Quad Padding { get; init; }

		public Point InnerOffset => new(Padding.Left, Padding.Top);

		public Size InnerExtent => new Size(
			Extent.Width - (Padding.Left + Padding.Right),
			Extent.Height - (Padding.Top + Padding.Bottom)
		);
	}

	[PublicAPI]
	public readonly struct ValueResult(scoped in Result result) {
		public byte[] Data { get; init; } = result.Data.ToArray();
		public Size Extent { get; init; } = result.Extent;
		public Quad Padding { get; init; }

		public Point InnerOffset => new(Padding.Left, Padding.Top);

		public Size InnerExtent => new Size(
			Extent.Width - (Padding.Left + Padding.Right),
			Extent.Height - (Padding.Top + Padding.Bottom)
		);
	}

	[PublicAPI]
	public static Result ResampleImage(
		SMConfig config,
		ScalerEnum scaler,
		ReadOnlySpan<byte> data,
		uint scale,
		int width,
		int height,
		bool horizontalWrappedHint = false,
		bool verticalWrappedHint = false
	) {
		byte[] dataBytes = data.ToArray();

		BasicSpriteInfo info = new(
			referenceData: dataBytes,
			bounds: new Bounds(0, 0, width, height),
			textureType: TextureType.Image,
			rawOffsetStride: (0, width),
			scaler: scaler,
			flags: SpriteInfoBase.SpriteFlags.None,
			wrapped: (horizontalWrappedHint, verticalWrappedHint)
		);

		var result = Root.Resample.Resampler.CreateNewTextureDirect(
			input: info,
			scale: scale,
			config: config
		);

		if (result.Status != Resampler.ResampleStatus.Success) {
			throw new InvalidOperationException($"Failed to resample sprite: status = `{result.Status}`");
		}

		return new() {
			Data = result.Data,
			Extent = new( result.Size.X, result.Size.Y ),
			Padding = new(result.Padding.X, result.Padding.Y)
		};
	}

	[PublicAPI]
	public static Task<ValueResult> ResampleImageAsync(
		SMConfig config,
		ScalerEnum scaler,
		ReadOnlySpan<byte> data,
		uint scale,
		int width,
		int height,
		bool horizontalWrappedHint = false,
		bool verticalWrappedHint = false
	) {
		byte[] dataBytes = data.ToArray();

		return Task.Run(
			() => {
				BasicSpriteInfo info = new(
					referenceData: dataBytes,
					bounds: new Bounds(0, 0, width, height),
					textureType: TextureType.Image,
					rawOffsetStride: (0, width),
					scaler: scaler,
					flags: SpriteInfoBase.SpriteFlags.None,
					wrapped: (horizontalWrappedHint, verticalWrappedHint)
				);

				var result = Root.Resample.Resampler.CreateNewTextureDirect(
					input: info,
					scale: scale,
					config: config
				);

				if (result.Status != Resampler.ResampleStatus.Success) {
					throw new InvalidOperationException($"Failed to resample sprite: status = `{result.Status}`");
				}

				return new ValueResult() {
					Data = result.Data.ToArray(),
					Extent = new( result.Size.X, result.Size.Y ),
					Padding = new(result.Padding.X, result.Padding.Y)
				};
			}
		);
	}

	[PublicAPI]
	public static Task<ValueResult?> ResampleImageSafeAsync(
		SMConfig config,
		ScalerEnum scaler,
		ReadOnlySpan<byte> data,
		uint scale,
		int width,
		int height,
		bool horizontalWrappedHint = false,
		bool verticalWrappedHint = false
	) {
		byte[] dataBytes = data.ToArray();

		return Task.Run<ValueResult?>(
			() => {
				BasicSpriteInfo info = new(
					referenceData: dataBytes,
					bounds: new Bounds(0, 0, width, height),
					textureType: TextureType.Image,
					rawOffsetStride: (0, width),
					scaler: scaler,
					flags: SpriteInfoBase.SpriteFlags.None,
					wrapped: (horizontalWrappedHint, verticalWrappedHint)
				);

				var result = Root.Resample.Resampler.CreateNewTextureDirect(
					input: info,
					scale: scale,
					config: config
				);

				if (result.Status != Resampler.ResampleStatus.Success) {
					//throw new InvalidOperationException($"Failed to resample sprite: status = `{result.Status}`");
					return null;
				}

				return new ValueResult() {
					Data = result.Data.ToArray(),
					Extent = new( result.Size.X, result.Size.Y ),
					Padding = new(result.Padding.X, result.Padding.Y)
				};
			}
		);
	}
}
