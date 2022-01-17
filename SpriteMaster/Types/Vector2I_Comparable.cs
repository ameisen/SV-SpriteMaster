using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Types;

partial struct Vector2I :
	IComparable,
	IComparable<Vector2I>,
	IComparable<Vector2I?>,
	IComparable<(int, int)>,
	IComparable<(int, int)?>,
	IComparable<DrawingPoint>,
	IComparable<DrawingPoint?>,
	IComparable<XNA.Point>,
	IComparable<XNA.Point?>,
	IComparable<XTilePoint>,
	IComparable<XTilePoint?>,
	IComparable<DrawingSize>,
	IComparable<DrawingSize?>,
	IComparable<XTileSize>,
	IComparable<XTileSize?>
{
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Vector2I other) => Packed.CompareTo(other.Packed);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Vector2I? other) => other.HasValue ? Packed.CompareTo(other.Value.Packed) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo((int, int) other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo((int, int)? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingPoint other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingPoint? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XNA.Point other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XNA.Point? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTilePoint other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTilePoint? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingSize other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingSize? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTileSize other) => CompareTo((Vector2I)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTileSize? other) => other.HasValue ? CompareTo((Vector2I)other.Value) : Packed.CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly int IComparable.CompareTo(object? other) => other switch {
		Vector2I vec => CompareTo(vec),
		DrawingPoint vec => CompareTo((Vector2I)vec),
		XNA.Point vec => CompareTo((Vector2I)vec),
		XTilePoint vec => CompareTo((Vector2I)vec),
		DrawingSize vec => CompareTo((Vector2I)vec),
		XTileSize vec => CompareTo((Vector2I)vec),
		_ => throw new ArgumentException(),
	};
}
