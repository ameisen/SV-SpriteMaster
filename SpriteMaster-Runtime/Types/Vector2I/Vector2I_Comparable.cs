using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Vector2I :
	IComparable,
	IComparable<Vector2I>,
	IComparable<Vector2I?>,
	IComparable<(int, int)>,
	IComparable<(int, int)?>
#if !SM_LIBRARY
	,
	IComparable<DrawingPoint>,
	IComparable<DrawingPoint?>,
	IComparable<XNA.Point>,
	IComparable<XNA.Point?>,
	IComparable<DrawingSize>,
	IComparable<DrawingSize?>
#endif
{
	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Vector2I other) => Packed.CompareTo(other.Packed);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Vector2I? other) => other.HasValue ? Packed.CompareTo(other.Value.Packed) : Packed.CompareTo(null);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo((int, int) other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo((int, int)? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo(null);

#if !SM_LIBRARY
	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingPoint other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingPoint? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo(null);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(XNA.Point other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(XNA.Point? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo(null);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingSize other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingSize? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo(null);
#endif

	[MethodImpl(Runtime.MethodImpl.Inline)]
	readonly int IComparable.CompareTo(object? other) => other switch {
		Vector2I vec => CompareTo(vec),
#if !SM_LIBRARY
		DrawingPoint vec => CompareTo((Vector2I)vec),
		XNA.Point vec => CompareTo((Vector2I)vec),
		DrawingSize vec => CompareTo((Vector2I)vec),
#endif
		Tuple<int, int> vector => CompareTo(new Vector2I(vector.Item1, vector.Item2)),
		ValueTuple<int, int> vector => CompareTo(vector),
#if !SM_LIBRARY
		_ => Extensions.Exceptions.ThrowArgumentException<int>(nameof(other), other)
#else
		_ => throw new ArgumentException(nameof(other))
#endif
	};
}
