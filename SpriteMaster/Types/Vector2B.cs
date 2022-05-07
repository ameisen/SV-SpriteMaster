using SpriteMaster.Extensions;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[DebuggerDisplay("[{X}, {Y}]")]
[StructLayout(LayoutKind.Sequential, Pack = sizeof(byte), Size = sizeof(byte))]
internal struct Vector2B :
	ICloneable,
	IComparable,
	IComparable<Vector2B>,
	IComparable<bool>,
	IComparable<(bool, bool)>,
	IEquatable<Vector2B>,
	IEquatable<bool>,
	IEquatable<(bool, bool)> {
	internal static readonly Vector2B True = new(packed: AllValue);
	internal static readonly Vector2B False = new(packed: ZeroByte);

	/*
	// TODO : would an int be faster? Since it would be a native type?
	// On x86, at least, populating a register with a byte should clear the upper bits anyways,
	// and our operations don't care about the upper bits.
	*/

	private const byte ZeroByte = 0;
	private const byte OneByte = 1;
	private const byte XBit = 0;
	private const byte YBit = 1;
	private const byte XValue = 1 << XBit;
	private const byte YValue = 1 << YBit;
	private const byte AllValue = XValue | YValue;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static byte GetX(bool value) => (byte)(value.ToByte() << XBit);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static byte GetY(bool value) => (byte)(value.ToByte() << YBit);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static byte Get(bool x, bool y) => (byte)(GetX(x) | GetY(y));

	private byte Packed = 0;

	internal bool X {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => (Packed & XValue) != ZeroByte;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Packed.SetBit(XBit, value);
	}
	internal bool Y {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => (Packed & YValue) != ZeroByte;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Packed.SetBit(YBit, value);
	}

	internal bool Width {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => X;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => X = value;
	}
	internal bool Height {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => Y;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Y = value;
	}

	internal bool Negative {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => X;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => X = value;
	}
	internal bool Positive {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => Y;
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Y = value;
	}

	internal readonly bool None => Packed == ZeroByte;
	internal readonly bool Any => Packed != ZeroByte;
	internal readonly bool All => Packed == AllValue;

	internal readonly Vector2B Invert => (!X, !Y);

	[MethodImpl(Runtime.MethodImpl.Hot), DebuggerStepThrough, DebuggerHidden()]
	private static int CheckIndex(int index) {
#if DEBUG
		if (index < 0 || index >= 2) {
			throw new IndexOutOfRangeException(nameof(index));
		}
#endif
		return index;
	}

	internal bool this[int index] {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		readonly get => Packed.GetBit(CheckIndex(index));
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set => Packed.SetBit(CheckIndex(index), value);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2B(byte packed) => Packed = packed;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2B From(byte packed) => new(packed: packed);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2B(bool x, bool y) : this(packed: Get(x, y)) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2B From(bool x, bool y) => new(x, y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2B(in (bool X, bool Y) value) : this(value.X, value.Y) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2B From(in (bool X, bool Y) value) => new(value.X, value.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2B(bool value) : this(value ? AllValue : ZeroByte) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2B From(bool value) => new(value: value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Vector2B(Vector2B vector) : this(vector.Packed) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Vector2B From(Vector2B vector) => new(vector: vector);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly Vector2B Clone() => this;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	readonly object ICloneable.Clone() => this;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator Vector2B(in (bool X, bool Y) vec) => new(vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator (bool X, bool Y)(Vector2B vec) => (vec.X, vec.Y);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator &(Vector2B lhs, Vector2B rhs) => new((byte)(lhs.Packed & rhs.Packed));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator &(Vector2B lhs, bool rhs) => rhs ? lhs : False;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator |(Vector2B lhs, Vector2B rhs) => new((byte)(lhs.Packed | rhs.Packed));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator |(Vector2B lhs, bool rhs) => rhs ? True : lhs;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator ^(Vector2B lhs, Vector2B rhs) => new((byte)(lhs.Packed ^ rhs.Packed));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static Vector2B operator ^(Vector2B lhs, bool rhs) => new((byte)(lhs.Packed ^ (rhs ? OneByte : ZeroByte)));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override readonly string ToString() => $"[{X}, {Y}]";

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(object? obj) => obj switch {
		Vector2B vector => CompareTo(vector),
		Tuple<bool, bool> vector => CompareTo(new Vector2B(vector.Item1, vector.Item2)),
		ValueTuple<bool, bool> vector => CompareTo(vector),
		bool boolean => CompareTo(boolean),
		_ => throw new ArgumentException(Exceptions.BuildArgumentException(nameof(obj), obj))
	};

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(Vector2B other) => Packed.CompareTo(other.Packed);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo((bool, bool) other) => CompareTo((Vector2B)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly int CompareTo(bool other) => Packed.CompareTo(other ? OneByte : ZeroByte);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(Vector2B other) => Packed == other.Packed;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals((bool, bool) other) => Equals((Vector2B)other);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly bool Equals(bool other) => Packed == (other ? OneByte : ZeroByte);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly TypeCode GetTypeCode() => TypeCode.Object;
}
