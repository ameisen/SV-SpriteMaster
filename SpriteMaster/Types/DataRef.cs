using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal readonly ref struct DataRef<T> where T : struct {
	private readonly T[]? DataInternal;
	internal readonly int Offset;
	internal readonly int Length;

	internal T[] Data {
		get {
			if (DataInternal is null) {
				throw new NullReferenceException(nameof(Data));
			}
			return DataInternal;
		}
	}

	internal bool IsEmpty => DataInternal is null || Length == 0;

	internal bool IsNull => DataInternal is null;

	internal bool IsEntire => DataInternal is not null && Offset == 0 && Length == DataInternal.Length;

	internal static DataRef<T> Null => new(null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal DataRef(T[]? data, int offset = 0, int length = 0) {
		offset.AssertPositiveOrZero();

		DataInternal = data;
		Offset = offset;
		Length = (length == 0 && DataInternal is not null) ? DataInternal.Length : length;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator DataRef<T>(T[]? data) {
		if (data is null)
			return Null;
		return new DataRef<T>(data);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(in DataRef<T> lhs, in DataRef<T> rhs) => lhs.DataInternal == rhs.DataInternal && lhs.Offset == rhs.Offset;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(in DataRef<T> lhs, object rhs) => lhs.DataInternal == rhs;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(in DataRef<T> lhs, in DataRef<T> rhs) => lhs.DataInternal == rhs.DataInternal && lhs.Offset == rhs.Offset;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(in DataRef<T> lhs, object rhs) => lhs.DataInternal != rhs;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal bool Equals(in DataRef<T> other) => this == other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override bool Equals(object? other) => other is not null && this == other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override int GetHashCode() {
		// TODO : This isn't right. We need to hash Data _from_ the offset.
		return Hashing.Combine32(DataInternal, Offset);
	}
}
