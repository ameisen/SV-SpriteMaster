using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal ref struct DataRef<T> where T : unmanaged {
	internal static DataRef<T> Null => new(null, 0);

	private readonly ReadOnlySpan<T> DataInternal = default;
	private T[]? CopiedData = null;
	private readonly int LengthInternal = 0;
	internal readonly int Length => DataInternal.Length;

	internal readonly ReadOnlySpan<T> Data => CopiedData is null ? DataInternal : CopiedData.AsReadOnlySpan();

	internal readonly T[] DataCopy {
		 get {
			if (DataInternal.IsEmpty) {
				throw new NullReferenceException(nameof(DataCopy));
			}

			if (CopiedData is null) {
				Unsafe.AsRef(in CopiedData) = GC.AllocateUninitializedArray<T>(DataInternal.Length);
				DataInternal.CopyTo(CopiedData.AsSpan());
			}
			return CopiedData!;
		}
	}

	internal readonly bool IsEmpty => Length == 0;

	internal readonly bool IsNull => DataInternal.IsEmpty || Length == 0;

	internal readonly bool HasData => !DataInternal.IsEmpty;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal DataRef(ReadOnlySpan<T> data, int length, T[]? referenceArray = null, bool copied = false) {
#if DEBUG
		if (referenceArray is null && copied) {
			throw new NullReferenceException(nameof(referenceArray));
		}
#endif
		DataInternal = data;
		CopiedData = copied ? referenceArray : null;
		LengthInternal = length;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public override readonly int GetHashCode() {
		return (int)DataInternal.AsBytes().Hash();
	}
}
