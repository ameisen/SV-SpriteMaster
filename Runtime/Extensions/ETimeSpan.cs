using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class ETimeSpan {
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Multiply (this TimeSpan timespan, int multiplier) => new TimeSpan(timespan.Ticks * multiplier);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Multiply (this TimeSpan timespan, float multiplier) => new TimeSpan((long)Math.Round(timespan.Ticks * multiplier));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Multiply (this TimeSpan timespan, double multiplier) => new TimeSpan((long)Math.Round(timespan.Ticks * multiplier));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Divide (this TimeSpan timespan, int divisor) => new TimeSpan(timespan.Ticks / divisor);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Divide (this TimeSpan timespan, float divisor) => new TimeSpan((long)Math.Round(timespan.Ticks / divisor));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Divide (this TimeSpan timespan, double divisor) => new TimeSpan((long)Math.Round(timespan.Ticks / divisor));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Double (this TimeSpan timespan) => new TimeSpan(timespan.Ticks << 1);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static TimeSpan Halve (this TimeSpan timespan) => new TimeSpan(timespan.Ticks >> 1);
	}
}
