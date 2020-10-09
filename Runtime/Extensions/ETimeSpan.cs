using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	public static class ETimeSpan {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Multiply (this TimeSpan timespan, int multiplier) => new TimeSpan(timespan.Ticks * multiplier);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Multiply (this TimeSpan timespan, float multiplier) => new TimeSpan((long)Math.Round(timespan.Ticks * multiplier));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Multiply (this TimeSpan timespan, double multiplier) => new TimeSpan((long)Math.Round(timespan.Ticks * multiplier));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Divide (this TimeSpan timespan, int divisor) => new TimeSpan(timespan.Ticks / divisor);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Divide (this TimeSpan timespan, float divisor) => new TimeSpan((long)Math.Round(timespan.Ticks / divisor));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Divide (this TimeSpan timespan, double divisor) => new TimeSpan((long)Math.Round(timespan.Ticks / divisor));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Double (this TimeSpan timespan) => new TimeSpan(timespan.Ticks << 1);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Halve (this TimeSpan timespan) => new TimeSpan(timespan.Ticks >> 1);
	}
}
