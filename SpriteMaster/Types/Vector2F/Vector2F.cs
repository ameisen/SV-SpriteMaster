using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NumericsVector2 = System.Numerics.Vector2;

namespace SpriteMaster.Types;

[DebuggerDisplay("[{X}, {Y}]")]
[StructLayout(LayoutKind.Explicit, Pack = sizeof(float) * 2, Size = sizeof(float) * 2)]
internal unsafe partial struct Vector2F : ILongHash {

	internal static readonly Vector2F Zero = (0.0f, 0.0f);
	internal static readonly Vector2F One = (1.0f, 1.0f);
	internal static readonly Vector2F MinusOne = (-1.0f, -1.0f);
	internal static readonly Vector2F Empty = Zero;

	[FieldOffset(0)]
	private NumericsVector2 NumericVector;

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

	internal float Width {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => X;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => X = value;
	}
	internal float Height {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => Y;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Y = value;
	}

	[MethodImpl(Runtime.MethodImpl.Hot), DebuggerStepThrough, DebuggerHidden()]
	private static int CheckIndex(int index) {
#if DEBUG
		if (index < 0 || index >= 2) {
			throw new IndexOutOfRangeException(nameof(index));
		}
#endif
		return index;
	}

	internal float this[int index] {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => Value[CheckIndex(index)];
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Value[CheckIndex(index)] = value;
	}

	internal readonly float Area => X * Y;

	internal readonly bool IsEmpty => NumericVector.Equals(NumericsVector2.Zero);
	internal readonly bool IsZero => IsEmpty;
	internal readonly float MinOf => MathF.Min(X, Y);
	internal readonly float MaxOf => MathF.Max(X, Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(float X, float Y) => NumericVector = new(X, Y);

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
	internal Vector2F(in XVector2 Vector) : this(Vector.X, Vector.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(Vector2F vec) : this(vec.NumericVector) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(Vector2I v) : this(v.X, v.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(NumericsVector2 v) => NumericVector = v;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(DrawingPoint v) : this(v.X, v.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(XNA.Point v) : this(v.X, v.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(XTilePoint v) : this(v.X, v.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(DrawingSize v) : this(v.Width, v.Height) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2F(XTileSize v) : this(v.Width, v.Height) { }

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
	internal void Set(XVector2 vec) => Set(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(in (float X, float Y) vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator (float X, float Y)(Vector2F vec) => (vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2I(Vector2F vec) => new((int)vec.X, (int)vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator XVector2(Vector2F vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(Vector2I vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(XVector2 vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(NumericsVector2 vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator NumericsVector2(Vector2F vec) => vec.NumericVector;
	  
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(DrawingPoint vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(XNA.Point vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(XTilePoint vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(DrawingSize vec) => new(vec);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2F(XTileSize vec) => new(vec);

	internal readonly float LengthSquared => NumericsVector2.Dot(NumericVector, NumericVector);
	internal readonly float Length => MathF.Sqrt(LengthSquared);

	internal readonly Vector2F Normalized => NumericsVector2.Normalize(NumericVector);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override readonly string ToString() => $"{{{X}, {Y}}}";

	// C# GetHashCode on all integer primitives, even longs, just returns it truncated to an int.
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly override int GetHashCode() => NumericVector.GetHashCode();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly ulong ILongHash.GetLongHashCode() => (ulong)NumericVector.GetHashCode();
}
