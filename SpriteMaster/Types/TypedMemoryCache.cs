using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

internal enum EvictionReason {
	None,
	/// <summary>Manually</summary>
	Removed,
	/// <summary>Overwritten</summary>
	Replaced,
	/// <summary>Timed out</summary>
	Expired,
	/// <summary>Event</summary>
	TokenExpired,
	/// <summary>Overflow</summary>
	Capacity,
}

internal class TypedMemoryCache<TKey, TValue> where TKey : notnull where TValue : class {
	internal delegate void RemovalCallbackDelegate(EvictionReason reason, TValue element);

	[StructLayout(LayoutKind.Auto)]
	private readonly struct CacheEntry {
		internal readonly TValue Value;
		internal readonly ConcurrentLinkedListSlim<TKey>.NodeRef Node;

		internal readonly bool IsValid => Node.IsValid;

		[MethodImpl(Runtime.MethodImpl.Inline)]
		internal CacheEntry(TValue value, ConcurrentLinkedListSlim<TKey>.NodeRef node) {
			Value = value;
			Node = node;
		}

		[MethodImpl(Runtime.MethodImpl.Inline)]
		internal readonly void UpdateValue(TValue value) {
			Unsafe.AsRef(in Value) = value;
		}
	}

	private readonly RemovalCallbackDelegate? RemovalCallback = null;
	private readonly Dictionary<TKey, CacheEntry> Cache = new();
	private readonly ConcurrentLinkedListSlim<TKey> RecentAccessList = new();
	private readonly SharedLock CacheLock = new();

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private void OnEntryRemoved(TKey key, in TValue value, EvictionReason reason) {
		if (RemovalCallback is not null) {
			RemovalCallback!(reason, value);
		}
	}

	internal TypedMemoryCache(string name, long? maxSize, RemovalCallbackDelegate? removalAction = null) {
		RemovalCallback = removalAction;
	}

	internal int Count => Cache.Count;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal TValue? Get(TKey key) {
		using (CacheLock.Read) {
			var entry = Cache.GetValueOrDefault(key);
			if (entry.IsValid) {
				RecentAccessList.MoveToFront(entry.Node);
			}

			return default;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal bool TryGet(TKey key, [NotNullWhen(true)] out TValue? value) {
		using (CacheLock.Read) {
			if (Cache.TryGetValue(key, out var entry)) {
				RecentAccessList.MoveToFront(entry.Node);
				value = entry.Value;
				return true;
			}
		}

		value = default;
		return false;
	}

	internal TValue Set(TKey key, TValue value, long? size) {
		TValue? original = null;

		using (CacheLock.Write) {
			if (Cache.TryGetValue(key, out var entry)) {
				original = entry.Value;
				entry.UpdateValue(value);
				Cache[key] = entry;
			}
			else {
				Cache.Add(key, new(value, RecentAccessList.AddFront(key)));
			}
		}

		if (original is not null) {
			OnEntryRemoved(key, original, EvictionReason.Replaced);
		}

		return value;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal TValue? Update(TKey key, TValue value, long? size) {
		TValue? original = null;

		using (CacheLock.ReadWrite) {
			var entry = Cache.GetValueOrDefault(key, default);
			using (CacheLock.Write) {
				if (entry.IsValid) {
					original = entry.Value;
					entry.UpdateValue(value);
					Cache[key] = entry;
				}
				else {
					Cache.Add(key, new(value, RecentAccessList.AddFront(key)));
				}
			}
		}

		if (original is not null) {
			OnEntryRemoved(key, original, EvictionReason.Replaced);
		}

		return original;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal TValue? Remove(TKey key) {
		TValue? result = null;

		using (CacheLock.Write) {
			if (Cache.Remove(key, out var entry)) {
				result = entry.Value;
				RecentAccessList.Release(entry.Node);
			}
		}

		if (result is not null) {
			OnEntryRemoved(key, result, EvictionReason.Removed);
		}

		return result;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal void Trim(int count) {
		count.AssertPositiveOrZero();

		if (count == 0) {
			return;
		}

		var trimArray = GC.AllocateUninitializedArray<KeyValuePair<TKey, TValue>>(count);

		using (CacheLock.Write) {
			if (count > Cache.Count) {
				count = Cache.Count;
			}

			if (count == 0) {
				return;
			}

			for (int i = 0; i < count; i++) {
				var removeKey = RecentAccessList.RemoveLast();
				bool result = Cache.Remove(removeKey, out var entry);
				trimArray[i] = new(removeKey, entry.Value);
				result.AssertTrue();
			}
		}

		for (int i = 0; i < count; ++i) {
			var pair = trimArray[i];
			OnEntryRemoved(pair.Key, pair.Value, EvictionReason.Expired);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal void TrimTo(int count) {
		count.AssertPositiveOrZero();

		using (CacheLock.Write) {
			Trim(Cache.Count - count);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal void Clear() {
		int removedCount;
		var removedPairs = GC.AllocateUninitializedArray<KeyValuePair<TKey, TValue>>(Count);

		using (CacheLock.Write) {
			if (Count > removedPairs.Length) {
				removedPairs = GC.AllocateUninitializedArray<KeyValuePair<TKey, TValue>>(Count);
			}

			removedCount = Count;

			int removedIndex = 0;
			foreach (var (key, entry) in Cache) {
				removedPairs[removedIndex++] = new(key, entry.Value);
			}

			Cache.Clear();
			RecentAccessList.Clear();
		}

		for (int i = 0; i < removedCount; i++) {
			var pair = removedPairs[i];
			OnEntryRemoved(pair.Key, pair.Value, EvictionReason.Removed);
		}
	}
}
