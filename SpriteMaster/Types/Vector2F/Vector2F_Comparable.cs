using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

partial struct Vector2F :
	IComparable,
	IComparable<Vector2F>,
	IComparable<(float, float)>,
	IComparable<XVector2> {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Vector2F other) {
		var result = X.CompareTo(other.X);
		if (result == 0) {
			return Y.CompareTo(other.Y);
		}
		return result;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo((float, float) other) => CompareTo((Vector2F)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Vector2I other) => CompareTo((Vector2F)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XVector2 other) => CompareTo((Vector2F)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly int IComparable.CompareTo(object? other) => other switch {
		Vector2F vec => CompareTo(vec),
		Vector2I vec => CompareTo(vec),
		XVector2 vec => CompareTo(vec),
		Tuple<float, float> vector => CompareTo(new Vector2F(vector.Item1, vector.Item2)),
		ValueTuple<float, float> vector => CompareTo(vector),
		_ => throw new ArgumentException(Exceptions.BuildArgumentException(nameof(other), other))
	};
}
