using JetBrains.Annotations;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types.MemoryCache;

internal interface IMemoryCache<TKey, TValue> : IObjectCache<TKey, TValue[]> where TKey : notnull where TValue : unmanaged {
	[Pure, MustUseReturnValue]
	ReadOnlySpan<TValue> GetSpan(TKey key);

	[Pure, MustUseReturnValue]
	bool TryGetSpan(TKey key, out ReadOnlySpan<TValue> value);

	[MustUseReturnValue]
	ReadOnlySpan<TValue> UpdateSpan(TKey key, TValue[] value);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	ReadOnlySpan<TValue> RemoveSpan(TKey key);
}
