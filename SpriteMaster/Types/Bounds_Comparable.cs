using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Bounds :
	IComparable,
	IComparable<Bounds>,
	IComparable<Bounds?>,
	IComparable<DrawingRectangle>,
	IComparable<DrawingRectangle?>,
	IComparable<XRectangle>,
	IComparable<XRectangle?>,
	IComparable<XTileRectangle>,
	IComparable<XTileRectangle?> {

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(object? other) => other switch {
		Bounds bounds => CompareTo(bounds),
		DrawingRectangle rect => CompareTo((Bounds)rect),
		XRectangle rect => CompareTo((Bounds)rect),
		XTileRectangle rect => CompareTo((Bounds)rect),
		_ => throw new ArgumentException(Exceptions.BuildArgumentException(nameof(other), other))
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Bounds other) => Offset.CompareTo(other.Offset) << 16 | (Extent.CompareTo(other.Extent) & 0xFFFF);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Bounds? other) => other.HasValue ? CompareTo(other.Value) : CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingRectangle other) => CompareTo((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(DrawingRectangle? other) => other.HasValue ? CompareTo((Bounds)other.Value) : CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XRectangle other) => CompareTo((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XRectangle? other) => other.HasValue ? CompareTo((Bounds)other.Value) : CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTileRectangle other) => CompareTo((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(XTileRectangle? other) => other.HasValue ? CompareTo((Bounds)other.Value) : CompareTo((object?)null);
}
