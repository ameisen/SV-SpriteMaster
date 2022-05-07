using SpriteMaster.Extensions;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[CLSCompliant(false)]
[DebuggerDisplay("[{Min} <-> {Max}}")]
[StructLayout(LayoutKind.Explicit, Pack = Vector2I.Alignment, Size = Vector2I.ByteSize)]
internal readonly struct ExtentI : IEquatable<ExtentI>, ILongHash {
	[FieldOffset(0)]
	private readonly Vector2I Value;

	[FieldOffset(0)]
	internal readonly int Min;

	[FieldOffset(sizeof(int))]
	internal readonly int Max;

	internal bool IsValid => Min <= Max;

	internal int Length => Max - Min;

	internal ExtentI(int min, int max) : this() {
		min.AssertLessEqual(max);
		Value = new(min, max);
	}

	internal ExtentI(ExtentI value) : this(value.Min, value.Max) { }

	internal ExtentI(in (int Min, int Max) value) : this(value.Min, value.Max) { }

	internal bool ContainsInclusive(int value) => value.WithinInclusive(Min, Max);

	internal bool ContainsExclusive(int value) => value.WithinExclusive(Min, Max);

	internal bool Contains(int value) => value.Within(Min, Max);

	internal bool ContainsInclusive(ExtentI value) => value.Min >= Min && value.Max <= Max;

	internal bool ContainsExclusive(ExtentI value) => value.Min > Min && value.Max < Max;

	internal bool Contains(ExtentI value) => ContainsInclusive(value);

	public bool Equals(ExtentI other) => Value == other.Value;

	public override bool Equals(object? other) {
		switch (other) {
			case ExtentI value: return Equals(value);
			case ValueTuple<int, int> value: return Equals(new ExtentI(value));
			default: return false;
		}
	}

	public override int GetHashCode() => Value.GetHashCode();

	ulong ILongHash.GetLongHashCode() => Value.GetLongHashCode();

	public static bool operator ==(ExtentI left, ExtentI right) => Equals(left, right);

	public static bool operator !=(ExtentI left, ExtentI right) => !Equals(left, right);
}
