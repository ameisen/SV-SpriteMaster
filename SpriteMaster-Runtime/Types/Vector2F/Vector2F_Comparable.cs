using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Vector2F :
	IComparable,
	IComparable<Vector2F>,
	IComparable<(float, float)>
#if !SM_LIBRARY
	,
	IComparable<XVector2>
#endif
{
	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Vector2F other) {
		var result = X.CompareTo(other.X);
		if (result == 0) {
			return Y.CompareTo(other.Y);
		}
		return result;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo((float, float) other) => CompareTo((Vector2F)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Vector2I other) => CompareTo((Vector2F)other);

#if !SM_LIBRARY
	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(XVector2 other) => CompareTo((Vector2F)other);
#endif

	[MethodImpl(Runtime.MethodImpl.Inline)]
	readonly int IComparable.CompareTo(object? other) => other switch {
		Vector2F vec => CompareTo(vec),
		Vector2I vec => CompareTo(vec),
#if !SM_LIBRARY
		XVector2 vec => CompareTo(vec),
#endif
		Tuple<float, float> vector => CompareTo(new Vector2F(vector.Item1, vector.Item2)),
		ValueTuple<float, float> vector => CompareTo(vector),
#if !SM_LIBRARY
		_ => Extensions.Exceptions.ThrowArgumentException<int>(nameof(other), other)
#else
		_ => throw new ArgumentException(nameof(other))
#endif
	};
}
