using System.Runtime.CompilerServices;

using NumericsVector2 = System.Numerics.Vector2;

namespace SpriteMaster.Types;

internal partial struct Vector2F {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2F vector) => vector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2F vector) => NumericsVector2.Negate(vector);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2F lhs, Vector2F rhs) => NumericsVector2.Add(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2F lhs, Vector2F rhs) => NumericsVector2.Subtract(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator *(Vector2F lhs, Vector2F rhs) => NumericsVector2.Multiply(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator /(Vector2F lhs, Vector2F rhs) => NumericsVector2.Divide(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator %(Vector2F lhs, Vector2F rhs) => new(
		lhs.X % rhs.X,
		lhs.Y % rhs.Y
	);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2F lhs, float rhs) => NumericsVector2.Add(lhs, new(rhs));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2F lhs, float rhs) => NumericsVector2.Subtract(lhs, new(rhs));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator *(Vector2F lhs, float rhs) => NumericsVector2.Multiply(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator /(Vector2F lhs, float rhs) => NumericsVector2.Divide(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator %(Vector2F lhs, float rhs) => new(
		lhs.X % rhs,
		lhs.Y % rhs
	);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2I lhs, Vector2F rhs) => NumericsVector2.Add((Vector2F)lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2I lhs, Vector2F rhs) => NumericsVector2.Subtract((Vector2F)lhs, rhs);
}
