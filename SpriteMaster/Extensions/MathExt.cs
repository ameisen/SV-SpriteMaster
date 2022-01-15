using SpriteMaster.Types.Fixed;
using System;

#nullable enable

namespace SpriteMaster.Extensions;

static class MathExt {
	internal static Fixed8 Min(this Fixed8 a, Fixed8 b) => Math.Min(a.Value, b.Value);
	internal static Fixed16 Min(this Fixed16 a, Fixed16 b) => Math.Min(a.Value, b.Value);

	internal static Fixed8 Max(this Fixed8 a, Fixed8 b) => Math.Max(a.Value, b.Value);
	internal static Fixed16 Max(this Fixed16 a, Fixed16 b) => Math.Max(a.Value, b.Value);

	internal static Fixed8 Clamp(this Fixed8 v, Fixed8 min, Fixed8 max) => Math.Clamp(v.Value, min.Value, max.Value);
	internal static Fixed16 Clamp(this Fixed16 v, Fixed16 min, Fixed16 max) => Math.Clamp(v.Value, min.Value, max.Value);
}
