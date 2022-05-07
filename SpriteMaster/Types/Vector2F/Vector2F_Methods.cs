using System;
using System.Runtime.CompilerServices;

using NumericsVector2 = System.Numerics.Vector2;

namespace SpriteMaster.Types;

internal partial struct Vector2F {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min() => new(MathF.Min(X, Y));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max() => new(MathF.Max(X, Y));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min(Vector2F v) => NumericsVector2.Min(this, v);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max(Vector2F v) => NumericsVector2.Max(this, v);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Clamp(Vector2F min, Vector2F max) => NumericsVector2.Clamp(this, min, max);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min(float v) => NumericsVector2.Min(this, new NumericsVector2(v));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max(float v) => NumericsVector2.Max(this, new NumericsVector2(v));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Clamp(float min, float max) => NumericsVector2.Clamp(this, new(min), new(max));

	internal readonly Vector2F Abs => NumericsVector2.Abs(this);
}