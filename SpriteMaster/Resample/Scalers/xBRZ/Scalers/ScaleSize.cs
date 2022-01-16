using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Resample.Scalers.xBRZ.Scalers;

static class ScaleSize {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static IScaler ToIScaler(this uint scaleSize, Config config) => scaleSize switch {
		2U => new Scaler2X(config),
		3U => new Scaler3X(config),
		4U => new Scaler4X(config),
		5U => new Scaler5X(config),
		6U => new Scaler6X(config),
		_ => throw new ArgumentOutOfRangeException(nameof(scaleSize))
	};
}
