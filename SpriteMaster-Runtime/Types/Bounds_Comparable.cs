using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Bounds :
	IComparable,
	IComparable<Bounds>,
	IComparable<Bounds?>
#if !SM_LIBRARY
	,
	IComparable<DrawingRectangle>,
	IComparable<DrawingRectangle?>,
	IComparable<XRectangle>,
	IComparable<XRectangle?>
#endif
{

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(object? other) => other switch {
		Bounds bounds => CompareTo(bounds),
#if !SM_LIBRARY
		DrawingRectangle rect => CompareTo((Bounds)rect),
		XRectangle rect => CompareTo((Bounds)rect),
		_ => Extensions.Exceptions.ThrowArgumentException<int>(nameof(other), other)
#else
		_ => throw new ArgumentException(nameof(other))
#endif
	};

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Bounds other) => Offset.CompareTo(other.Offset) << 16 | (Extent.CompareTo(other.Extent) & 0xFFFF);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(Bounds? other) => other.HasValue ? CompareTo(other.Value) : CompareTo((object?)null);

#if !SM_LIBRARY
	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingRectangle other) => CompareTo((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(DrawingRectangle? other) => other.HasValue ? CompareTo((Bounds)other.Value) : CompareTo((object?)null);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(XRectangle other) => CompareTo((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public readonly int CompareTo(XRectangle? other) => other.HasValue ? CompareTo((Bounds)other.Value) : CompareTo((object?)null);
#endif
}
