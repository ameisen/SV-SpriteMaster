using SpriteMaster.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers;

internal interface IScaler {
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
		Config scalerConfig,
		uint scaleMultiplier,
		ReadOnlySpan<Color16> sourceData,
		Vector2I sourceSize,
		Span<Color16> targetData,
		Vector2I targetSize
	);

	internal static IScalerInfo DefaultInfo => DefaultScaler.ScalerInfo.Instance;

	internal static IScaler Default => new DefaultScaler.Scaler.ScalerInterface();

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowUnknownScalerTypeException<T>(Scaler scaler) =>
		throw new InvalidOperationException($"Unknown Scaler Type: {scaler}");

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowBilinearNotImplementedException<T>() =>
		throw new NotImplementedException("Bilinear scaling is not implemented");

	internal static IScalerInfo? GetScalerInfo(Scaler scaler) => scaler switch {
		Scaler.xBRZ => xBRZ.ScalerInfo.Instance,
#if !SHIPPING
		Scaler.SuperXBR => Resample.Scalers.SuperXBR.ScalerInfo.Instance,
#endif
		Scaler.EPX => EPX.ScalerInfo.Instance,
		Scaler.EPXLegacy => EPX.ScalerInfo.InstanceLegacy,
		Scaler.xBREPX => xBREPX.ScalerInfo.Instance,
		Scaler.None => null,
		_ => ThrowUnknownScalerTypeException<IScalerInfo>(scaler)
	};

	

	internal static IScalerInfo? CurrentInfo => GetScalerInfo(Resampler.CurrentConfiguredScaler);

	internal static IScaler? Current => Resampler.CurrentConfiguredScaler switch {
		// ReSharper disable UseSymbolAlias
		Scaler.xBRZ => xBRZ.Scaler.ScalerInterface.Instance,
#if !SHIPPING
		Scaler.SuperXBR => Resample.Scalers.SuperXBR.Scaler.ScalerInterface.Instance,
#endif
		Scaler.EPX => EPX.Scaler.ScalerInterface.Instance,
		Scaler.EPXLegacy => EPX.Scaler.ScalerInterface.InstanceLegacy,
		Scaler.xBREPX => xBREPX.Scaler.ScalerInterface.Instance,
		Scaler.None => null,
		// ReSharper restore UseSymbolAlias
		_ => ThrowUnknownScalerTypeException<IScaler>(Resampler.CurrentConfiguredScaler)
	};
}
