using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Caching;
using System.Threading;

namespace SpriteMaster;

[SMAPIConsole.Stats("suspended-sprite-cache")]
static class SuspendedSpriteCache {
	private static long TotalCachedSize = 0L;
	private static readonly TypedMemoryCache<ManagedSpriteInstance> Cache = new("SuspendedSpriteCache", OnEntryRemoved);

	private static void OnEntryRemoved(CacheEntryRemovedReason reason, ManagedSpriteInstance element) {
		Interlocked.Add(ref TotalCachedSize, -element.MemorySize);
		element.Dispose();
	}

	internal static void Add(ManagedSpriteInstance instance) {
		if (!Config.SuspendedCache.Enabled) {
			instance.Dispose();
			return;
		}

		var key = instance.Hash.ToString64();
		Cache.Set(key, instance);
		Interlocked.Add(ref TotalCachedSize, instance.MemorySize);
		Debug.Trace($"SuspendedSpriteCache Size: {Cache.Count.ToString(System.Drawing.Color.LightCoral)}");

		if (Interlocked.Read(ref TotalCachedSize) > Config.SuspendedCache.MaxCacheSize) {
			Cache.Trim(10);
		}
	}

	internal static ManagedSpriteInstance? Fetch(ulong hash) {
		if (!Config.SuspendedCache.Enabled) {
			return null;
		}

		var key = hash.ToString64();
		return Cache.Get(key);
	}

	internal static bool TryFetch(ulong hash, [NotNullWhen(true)] out ManagedSpriteInstance? instance) {
		instance = Fetch(hash);
		return instance is not null;
	}

	internal static bool Remove(ulong hash) {
		var element = Cache.Remove(hash.ToString64());
		if (element is not null) {
			Interlocked.Add(ref TotalCachedSize, -element.MemorySize);
			return true;
		}
		return false;
	}

	internal static bool Remove(ManagedSpriteInstance instance) => Remove(instance.Hash);

	[SMAPIConsole.StatsMethod]
	internal static string[] DumpStats() {
		var statsLines = new List<string>();
		statsLines.Add($"\tTotal Suspended Elements: {Cache.Count}");
		statsLines.Add($"\tTotal Memory Size       : {Interlocked.Read(ref TotalCachedSize).AsDataSize()}");
		return statsLines.ToArray();
	}
}
