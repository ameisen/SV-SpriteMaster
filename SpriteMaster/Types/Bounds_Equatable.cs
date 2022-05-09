using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal partial struct Bounds :
	IEquatable<Bounds>,
	IEquatable<Bounds?>,
	IEquatable<DrawingRectangle>,
	IEquatable<DrawingRectangle?>,
	IEquatable<XRectangle>,
	IEquatable<XRectangle?>,
	IEquatable<XTileRectangle>,
	IEquatable<XTileRectangle?> {

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override readonly bool Equals(object? other) => other switch {
		Bounds bounds => Equals(bounds),
		DrawingRectangle rect => Equals(rect),
		XRectangle rect => Equals(rect),
		XTileRectangle rect => Equals(rect),
		_ => false,
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(Bounds other) => (Offset == other.Offset) & (Extent == other.Extent);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(Bounds? other) => other.HasValue && ((Offset == other.Value.Offset) & (Extent == other.Value.Extent));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(DrawingRectangle other) => Equals((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(DrawingRectangle? other) => other.HasValue && Equals((Bounds)other.Value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(XRectangle other) => Equals((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(XRectangle? other) => other.HasValue && Equals((Bounds)other.Value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(XTileRectangle other) => Equals((Bounds)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(XTileRectangle? other) => other.HasValue && Equals((Bounds)other.Value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Bounds lhs, Bounds rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Bounds lhs, Bounds rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Bounds lhs, DrawingRectangle rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Bounds lhs, DrawingRectangle rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(DrawingRectangle lhs, Bounds rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(DrawingRectangle lhs, Bounds rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Bounds lhs, XRectangle rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Bounds lhs, XRectangle rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(XRectangle lhs, Bounds rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(XRectangle lhs, Bounds rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Bounds lhs, XTileRectangle rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Bounds lhs, XTileRectangle rhs) => !(lhs == rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(XTileRectangle lhs, Bounds rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(XTileRectangle lhs, Bounds rhs) => !(lhs == rhs);
}
