﻿#if !SHIPPING
using SpriteMaster.Types;
using System;

namespace SpriteMaster.Resample.Scalers.SuperXBR;

internal sealed partial class Scaler {
	internal sealed class ScalerInterface : IScaler {
		internal static readonly ScalerInterface Instance = new();

		public IScalerInfo Info => ScalerInfo.Instance;

		public uint MinScale => Scaler.MinScale;

		public uint MaxScale => Scaler.MaxScale;

		public uint ClampScale(uint scale) => Scaler.ClampScale(scale);

		public Span<Color16> Apply(
			in Resample.Scalers.Config configuration,
			uint scaleMultiplier,
			ReadOnlySpan<Color16> sourceData,
			Vector2I sourceSize,
			Span<Color16> targetData,
			Vector2I targetSize
		) =>
			Scaler.Apply(configuration as Config, scaleMultiplier, sourceData, sourceSize, targetData, targetSize);

		public Resample.Scalers.Config CreateConfig(Vector2B wrapped, bool hasAlpha, bool gammaCorrected) => new Config(
			wrapped: wrapped,
			hasAlpha: hasAlpha,
			gammaCorrected: gammaCorrected
		);
	}
}
#endif
