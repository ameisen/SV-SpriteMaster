using SpriteMaster.Extensions;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types {
	internal ref struct DataRef<T> where T : struct {
		internal readonly T[] Data;
		internal readonly int Offset;
		internal readonly int Length;

		internal readonly bool IsEmpty => Data == null || Length == 0;

		internal readonly bool IsNull => Data == null; 

		internal readonly bool IsEntire => !IsNull && Offset == 0 && Length == Data.Length;

		internal static DataRef<T> Null => new DataRef<T>(null);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal DataRef (T[] data, int offset = 0, int length = 0) {
			Contract.AssertPositiveOrZero(offset);

			Data = data;
			Offset = offset;
			Length = (length == 0 && Data != null) ? Data.Length : length;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static implicit operator DataRef<T> (T[] data) {
			if (data == null)
				return Null;
			return new DataRef<T>(data);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static bool operator == (DataRef<T> lhs, DataRef<T> rhs) => lhs.Data == rhs.Data && lhs.Offset == rhs.Offset;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static bool operator == (DataRef<T> lhs, object rhs) => lhs.Data == rhs;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static bool operator != (DataRef<T> lhs, DataRef<T> rhs) => lhs.Data == rhs.Data && lhs.Offset == rhs.Offset;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static bool operator != (DataRef<T> lhs, object rhs) => lhs.Data != rhs;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal readonly bool Equals (DataRef<T> other) => this == other;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public readonly override bool Equals (object other) => this == other;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public readonly override int GetHashCode () {
			// TODO : This isn't right. We need to hash Data _from_ the offset.
			return unchecked((int)Hash.Combine(Data.GetHashCode(), Offset.GetHashCode()));
		}
	}
}
