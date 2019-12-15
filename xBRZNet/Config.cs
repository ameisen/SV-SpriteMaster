using System;
using System.Runtime.CompilerServices;

namespace xBRZNet2
{
	public struct Config
	{
		internal readonly bool WrappedX;
		internal readonly bool WrappedY;

		// These are the default values:
		internal readonly double LuminanceWeight;
		internal readonly double EqualColorTolerance;
		internal readonly double DominantDirectionThreshold;
		internal readonly double SteepDirectionThreshold;

		// Precalculated
		internal readonly double EqualColorTolerancePow2;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Config(
			bool wrappedX = false,
			bool wrappedY = false,
			double luminanceWeight = 1.0,
			double equalColorTolerance = 30.0,
			double dominantDirectionThreshold = 3.6,
			double steepDirectionThreshold = 2.2
		)
		{
			WrappedX = wrappedX;
			WrappedY = wrappedY;
			LuminanceWeight = luminanceWeight;
			EqualColorTolerance = equalColorTolerance;
			DominantDirectionThreshold = dominantDirectionThreshold;
			SteepDirectionThreshold = steepDirectionThreshold;

			EqualColorTolerancePow2 = Math.Pow(EqualColorTolerance, 2.0);
		}
	}
}
