using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;
readonly ref struct DataRef<T> where T : struct {
	internal readonly T[] Data;
	internal readonly int Offset;
	internal readonly int Length;

	internal readonly bool IsEmpty => Data is null || Length == 0;

	internal readonly bool IsNull => Data is null;

	internal readonly bool IsEntire => !IsNull && Offset == 0 && Length == Data.Length;

	internal static DataRef<T> Null => new(null);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal DataRef(T[] data, int offset = 0, int length = 0) {
		Contracts.AssertPositiveOrZero(offset);

		Data = data;
		Offset = offset;
		Length = (length == 0 && Data is not null) ? Data.Length : length;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static implicit operator DataRef<T>(T[] data) {
		if (data is null)
			return Null;
		return new DataRef<T>(data);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(in DataRef<T> lhs, in DataRef<T> rhs) => lhs.Data == rhs.Data && lhs.Offset == rhs.Offset;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator ==(in DataRef<T> lhs, object rhs) => lhs.Data == rhs;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(in DataRef<T> lhs, in DataRef<T> rhs) => lhs.Data == rhs.Data && lhs.Offset == rhs.Offset;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool operator !=(in DataRef<T> lhs, object rhs) => lhs.Data != rhs;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal readonly bool Equals(in DataRef<T> other) => this == other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly override bool Equals(object other) => this == other;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public readonly override int GetHashCode() {
		// TODO : This isn't right. We need to hash Data _from_ the offset.
		return (int)Hashing.Combine(Data.GetHashCode(), Offset.GetHashCode());
	}
}
