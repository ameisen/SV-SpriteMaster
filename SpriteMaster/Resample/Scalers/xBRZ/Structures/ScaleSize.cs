using SpriteMaster.Resample.Scalers.xBRZ.Structures;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers.xBRZ.Scalers;

internal static class ScaleSize {
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowArgumentOutOfRangeException<T>(string name) =>
		throw new ArgumentOutOfRangeException(name);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static AbstractScaler ToIScaler(this uint scaleSize, Config config) => scaleSize switch {
		2U => new Scaler2X(config),
		3U => new Scaler3X(config),
		4U => new Scaler4X(config),
		5U => new Scaler5X(config),
		6U => new Scaler6X(config),
		_ => ThrowArgumentOutOfRangeException<AbstractScaler>(nameof(scaleSize))
	};
}
