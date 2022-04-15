using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions;

static class ETimeSpan {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Multiply(this in TimeSpan timespan, int multiplier) => new(timespan.Ticks * multiplier);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Multiply(this in TimeSpan timespan, float multiplier) => new((long)MathF.Round(timespan.Ticks * multiplier));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Multiply(this in TimeSpan timespan, double multiplier) => new((timespan.Ticks * multiplier).RoundToLong());

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Divide(this in TimeSpan timespan, int divisor) => new(timespan.Ticks / divisor);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Divide(this in TimeSpan timespan, float divisor) => new((timespan.Ticks / divisor).RoundToLong());

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Divide(this in TimeSpan timespan, double divisor) => new((timespan.Ticks / divisor).RoundToLong());

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Double(this in TimeSpan timespan) => new(timespan.Ticks << 1);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static TimeSpan Halve(this in TimeSpan timespan) => new(timespan.Ticks >> 1);
}
