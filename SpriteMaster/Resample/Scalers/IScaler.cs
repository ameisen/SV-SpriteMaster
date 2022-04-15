using SpriteMaster.Types;
using System;

namespace SpriteMaster.Resample.Scalers;

interface IScaler {
	Config CreateConfig(
		Vector2B wrapped,
		bool hasAlpha,
		bool gammaCorrected
	);

	IScalerInfo Info { get; }

	uint MinScale { get; }
	uint MaxScale { get; }
	uint ClampScale(uint scale);

	Span<Color16> Apply(
		in Config configuration,
		uint scaleMultiplier,
		ReadOnlySpan<Color16> sourceData,
		Vector2I sourceSize,
		Span<Color16> targetData,
		Vector2I targetSize
	);

	internal static IScalerInfo DefaultInfo => DefaultScaler.ScalerInfo.Instance;

	internal static IScaler Default => new DefaultScaler.Scaler.ScalerInterface();

	internal static IScalerInfo? GetScalerInfo(Resample.Scaler scaler) => scaler switch {
		Resample.Scaler.xBRZ =>
			Resample.Scalers.xBRZ.ScalerInfo.Instance,
#if !SHIPPING
		Resample.Scaler.SuperXBR =>
			Resample.Scalers.SuperXBR.ScalerInfo.Instance,
#endif
		Resample.Scaler.EPX =>
			Resample.Scalers.EPX.ScalerInfo.Instance,
#if !SHIPPING
		Resample.Scaler.Bilinear =>
			throw new NotImplementedException("Bilinear scaling is not implemented"),
#endif
		Resample.Scaler.None => null,
		_ =>
			throw new InvalidOperationException($"Unknown Scaler Type: {SMConfig.Resample.Scaler}")
	};

	internal static IScalerInfo? CurrentInfo => GetScalerInfo(SMConfig.Resample.Scaler);

	internal static IScaler Current => SMConfig.Resample.Scaler switch {
		Resample.Scaler.xBRZ =>
			new Resample.Scalers.xBRZ.Scaler.ScalerInterface(),
#if !SHIPPING
		Resample.Scaler.SuperXBR =>
			new Resample.Scalers.SuperXBR.Scaler.ScalerInterface(),
#endif
		Resample.Scaler.EPX =>
			new Resample.Scalers.EPX.Scaler.ScalerInterface(),
#if !SHIPPING
		Resample.Scaler.Bilinear =>
			throw new NotImplementedException("Bilinear scaling is not implemented"),
#endif
		_ =>
			throw new InvalidOperationException($"Unknown Scaler Type: {SMConfig.Resample.Scaler}")
	};
}
