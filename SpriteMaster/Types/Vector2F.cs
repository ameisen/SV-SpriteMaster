using SpriteMaster.Extensions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SystemVector2 = System.Numerics.Vector2;

namespace SpriteMaster.Types;

[DebuggerDisplay("[{X}, {Y}}")]
[StructLayout(LayoutKind.Explicit, Pack = sizeof(float) * 2, Size = sizeof(float) * 2)]
unsafe struct Vector2F : 
	ICloneable,
	IComparable,
	IComparable<Vector2F>,
	IComparable<(float, float)>,
	IComparable<XNA.Vector2>,
	IEquatable<Vector2F>,
	IEquatable<(float, float)>,
	IEquatable<XNA.Vector2> {

	internal static readonly Vector2F Zero = (0.0f, 0.0f);
	internal static readonly Vector2F One = (1.0f, 1.0f);
	internal static readonly Vector2F MinusOne = (-1.0f, -1.0f);
	internal static readonly Vector2F Empty = Zero;

	[FieldOffset(0)]
	private SystemVector2 NumericVector;

	[FieldOffset(0)]
	private fixed float Value[2];

	internal float X {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => NumericVector.X;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => NumericVector.X = value;
	}
	internal float Y {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => NumericVector.Y;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => NumericVector.Y = value;
	}


	internal float Width { [MethodImpl(Runtime.MethodImpl.Hot)] readonly get => X; [MethodImpl(Runtime.MethodImpl.Hot)] set { X = value; } }
	internal float Height { [MethodImpl(Runtime.MethodImpl.Hot)] readonly get => Y; [MethodImpl(Runtime.MethodImpl.Hot)] set { Y = value; } }

	internal float this[int index] {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get {
#if DEBUG
			if (index < 0 || index >= 2) {
				throw new IndexOutOfRangeException(nameof(index));
			}
#endif
			return Value[index];
		}
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set {
#if DEBUG
			if (index < 0 || index >= 2) {
				throw new IndexOutOfRangeException(nameof(index));
			}
#endif
			Value[index] = value;
		}
	}

	internal readonly float Area => X * Y;

	internal readonly bool IsEmpty => NumericVector.Equals(SystemVector2.Zero);
	internal readonly bool IsZero => IsEmpty;
	internal readonly float MinOf => Math.Min(X, Y);
	internal readonly float MaxOf => Math.Max(X, Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(float X, float Y) {
		NumericVector = new(X, Y);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2F From(float X, float Y) => new(X, Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(in (float X, float Y) vec) : this(vec.X, vec.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2F From(in (float X, float Y) vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(float value) => NumericVector = new(value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2F From(float value) => new(value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(in XNA.Vector2 Vector) : this(Vector.X, Vector.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(Vector2F vec) : this(vec.NumericVector) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(Vector2I v) : this(v.X, v.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(SystemVector2 v) => NumericVector = v;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(float x, float y) => NumericVector = new(x, y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(in (float X, float Y) vec) => NumericVector = new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(float v) => Set(v, v);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(Vector2F vec) => NumericVector = vec.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(Vector2I vec) => Set(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Set(XNA.Vector2 vec) => Set(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(in (float X, float Y) vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator (float X, float Y)(Vector2F vec) => (vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2I(Vector2F vec) => new((int)vec.X, (int)vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator XNA.Vector2(Vector2F vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(Vector2I vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(XNA.Vector2 vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(SystemVector2 vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator SystemVector2(Vector2F vec) => vec.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min() => new(Math.Min(X, Y));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max() => new(Math.Max(X, Y));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min(Vector2F v) => SystemVector2.Min(this, v);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max(Vector2F v) => SystemVector2.Max(this, v);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Clamp(Vector2F min, Vector2F max) => SystemVector2.Clamp(this, min, max);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Min(float v) => SystemVector2.Min(this, new SystemVector2(v));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Max(float v) => SystemVector2.Max(this, new SystemVector2(v));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Clamp(float min, float max) => SystemVector2.Clamp(this, new(min), new(max));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2F Clone() => this;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly object ICloneable.Clone() => this;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2F lhs, Vector2F rhs) => SystemVector2.Add(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2F lhs, Vector2F rhs) => SystemVector2.Subtract(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator *(Vector2F lhs, Vector2F rhs) => SystemVector2.Multiply(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator /(Vector2F lhs, Vector2F rhs) => SystemVector2.Divide(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator %(Vector2F lhs, Vector2F rhs) => new(
		lhs.X % rhs.X,
		lhs.Y % rhs.Y
	);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator +(Vector2F lhs, float rhs) => SystemVector2.Add(lhs, new(rhs));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator -(Vector2F lhs, float rhs) => SystemVector2.Subtract(lhs, new(rhs));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator *(Vector2F lhs, float rhs) => SystemVector2.Multiply(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator /(Vector2F lhs, float rhs) => SystemVector2.Divide(lhs, rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2F operator %(Vector2F lhs, float rhs) => new(
		lhs.X % rhs,
		lhs.Y % rhs
	);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override readonly string ToString() => $"{{{X}, {Y}}}";

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
	public readonly int CompareTo(XNA.Vector2 other) => CompareTo((Vector2F)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly int IComparable.CompareTo(object other) => other switch {
		Vector2F vec => CompareTo(vec),
		Vector2I vec => CompareTo(vec),
		XNA.Vector2 vec => CompareTo(vec),
		_ => throw new ArgumentException(),
	};

	// C# GetHashCode on all integer primitives, even longs, just returns it truncated to an int.
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly override int GetHashCode() => NumericVector.GetHashCode();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly override bool Equals(object other) => other switch {
		Vector2F vec => Equals(vec),
		Vector2I vec => Equals(vec),
		XNA.Vector2 vec => Equals(vec),
		_ => throw new ArgumentException(),
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(Vector2F other) => NumericVector == other.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals((float, float) other) => this == (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool Equals(in (float X, float Y) other) => this == (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(Vector2I other) => this == (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(XNA.Vector2 other) => this == (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool NotEquals(Vector2F other) => NumericVector != other.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool NotEquals(in (float X, float Y) other) => this != (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool NotEquals(Vector2I other) => this != (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool NotEquals(XNA.Vector2 other) => this != (Vector2F)other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Vector2F lhs, Vector2F rhs) => lhs.NumericVector == rhs.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Vector2F lhs, Vector2F rhs) => lhs.NumericVector != rhs.NumericVector;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Vector2F lhs, in (float X, float Y) rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Vector2F lhs, in (float X, float Y) rhs) => lhs.NotEquals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(in (float X, float Y) lhs, Vector2F rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(in (float X, float Y) lhs, Vector2F rhs) => rhs.NotEquals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Vector2F lhs, Vector2I rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Vector2F lhs, Vector2I rhs) => lhs.NotEquals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Vector2I lhs, Vector2F rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Vector2I lhs, Vector2F rhs) => rhs.NotEquals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(Vector2F lhs, XNA.Vector2 rhs) => lhs.Equals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(Vector2F lhs, XNA.Vector2 rhs) => lhs.NotEquals(rhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(XNA.Vector2 lhs, Vector2F rhs) => rhs.Equals(lhs);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(XNA.Vector2 lhs, Vector2F rhs) => rhs.NotEquals(lhs);
}
