using SpriteMaster.Extensions;
using SpriteMaster.Resample.Passes;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using SpriteMaster.Types.Spans;
using System;
using System.Runtime.InteropServices;

namespace SpriteMaster.Resample;

internal sealed partial class Resampler {
	internal enum ResampleStatus {
		Unknown = -1,
		Success = 0,
		Failure = 1,
		DisabledGradient = 2,
		DisabledSolid = 3,
		Disabled = 4,
	}

	// TODO : use MemoryFailPoint class. Extensively.

	private enum GammaState {
		Linear,
		Gamma
	}
}
