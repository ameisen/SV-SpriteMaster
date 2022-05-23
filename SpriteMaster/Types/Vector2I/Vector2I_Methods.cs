using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Vector2I {
	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Min() => new(Math.Min(X, Y));

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Max() => new(Math.Max(X, Y));

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Min(Vector2I v) => new(
		Math.Min(X, v.X),
		Math.Min(Y, v.Y)
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Max(Vector2I v) => new(
		Math.Max(X, v.X),
		Math.Max(Y, v.Y)
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Clamp(Vector2I min, Vector2I max) => new(
		X.Clamp(min.X, max.X),
		Y.Clamp(min.Y, max.Y)
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Min(int v) => new(
		Math.Min(X, v),
		Math.Min(Y, v)
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Max(int v) => new(
		Math.Max(X, v),
		Math.Max(Y, v)
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal readonly Vector2I Clamp(int min, int max) => new(
		X.Clamp(min, max),
		Y.Clamp(min, max)
	);

	internal readonly Vector2I Abs => (Math.Abs(X), Math.Abs(Y));
}
