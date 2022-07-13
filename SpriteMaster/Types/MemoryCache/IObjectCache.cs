using JetBrains.Annotations;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SpriteMaster.Types.MemoryCache;

internal interface IObjectCache<TKey, TValue> :
	IDisposable, IAsyncDisposable, ICache
	where TKey : notnull where TValue : notnull {

	string Name { get; }

	long TotalSize { get; }

	[Pure]
	int Count { get; }

	[Pure, MustUseReturnValue]
	TValue? Get(TKey key);

	[Pure, MustUseReturnValue]
	bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value);

	[MustUseReturnValue]
	TValue Set(TKey key, TValue value);

	void SetFast(TKey key, TValue value);

	[MustUseReturnValue]
	TValue? Update(TKey key, TValue value);

	[MustUseReturnValue]
	TValue? Remove(TKey key);

	void RemoveFast(TKey key);

	void Trim(int count);

	void TrimTo(int count);

	void Clear();

	[MustUseReturnValue]
	(ulong Count, ulong Size) ClearWithCount();
}
