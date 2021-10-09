using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal sealed class TexelTimer {
		private double DurationPerTexel = 0.0;
		private const int MaxDurationCounts = 50;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal void Add(int texels, TimeSpan duration) {
			// Avoid a division by zero
			if (texels == 0) {
				return;
			}

			var texelDuration = (double)duration.Ticks / texels;
			DurationPerTexel -= DurationPerTexel / MaxDurationCounts;
			DurationPerTexel += texelDuration / MaxDurationCounts;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal void Add (TextureAction action, TimeSpan duration) {
			Add(action.Texels, duration);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal TimeSpan Estimate (int texels) {
			return TimeSpan.FromTicks((DurationPerTexel * texels).NextLong());
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal TimeSpan Estimate(TextureAction action) {
			return Estimate(action.Texels);
		}
	}
}
