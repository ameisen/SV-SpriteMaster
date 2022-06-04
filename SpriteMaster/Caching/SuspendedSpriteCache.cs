using SpriteMaster.Configuration;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpriteMaster.Caching;

[SMAPIConsole.Stats("suspended-sprite-cache")]
internal static class SuspendedSpriteCache {
	private static long TotalCachedSize = 0L;
	private const double MinTrimPercentage = 0.05;
	private static readonly TypedMemoryCache<ulong, ManagedSpriteInstance> Cache = new(
		name: "SuspendedSpriteCache",
		maxSize: Config.SuspendedCache.MaxCacheSize,
		removalAction: OnEntryRemoved
	);
	private static readonly Condition TrimEvent = new();
	private static readonly Thread CacheTrimThread = ThreadExt.Run(CacheTrimLoop, background: true, name: "Cache Trim Thread");

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static int Percent75(int value) {
		return (value >> 1) + (value >> 2);
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static long Percent75(long value) {
		return (value >> 1) + (value >> 2);
	}

	private static void TrimSize() {
		if (Config.SuspendedCache.MaxCacheSize is <= 0 or long.MaxValue) {
			return;
		}

		var totalCachedSize = Interlocked.Read(ref TotalCachedSize);

		// How much is needed to be reduced, with an additional safety of 25% or so?
		var goal = Percent75(Config.SuspendedCache.MaxCacheSize);

		Debug.Trace(
			$"Trimming (Size) SuspendedSpriteCache: from {totalCachedSize.AsDataSize()} to {goal.AsDataSize()}"
		);

		while (totalCachedSize > Config.SuspendedCache.MaxCacheSize) {
			Cache.Trim(1);

			totalCachedSize = Interlocked.Read(ref TotalCachedSize);
		}
	}

	private static void TrimCount() {
		if (Config.SuspendedCache.MaxCacheCount is <= 0 or int.MaxValue) {
			return;
		}

		int currentCacheCount = Cache.Count;

		if (currentCacheCount <= Config.SuspendedCache.MaxCacheCount) {
			return;
		}

		var goal = Percent75(Config.SuspendedCache.MaxCacheCount);

		Debug.Trace($"Trimming (Count) SuspendedSpriteCache: {currentCacheCount} -> {goal}");
		Cache.TrimTo(goal);
	}

	[DoesNotReturn]
	private static void CacheTrimLoop() {
		while (true) {
			TrimEvent.Wait();

			TrimSize();
			TrimCount();
		}
		// ReSharper disable once FunctionNeverReturns
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static void OnEntryRemoved(EvictionReason reason, ManagedSpriteInstance element) {
		Interlocked.Add(ref TotalCachedSize, -element.MemorySize);
		element.Dispose();
	}

	internal static void Add(ManagedSpriteInstance instance) {
		if (!Config.SuspendedCache.Enabled) {
			instance.Dispose();
			return;
		}

		Cache.Set(instance.Hash, instance, size: instance.MemorySize);
		Interlocked.Add(ref TotalCachedSize, instance.MemorySize);
		Debug.Trace($"SuspendedSpriteCache Size: {Cache.Count.ToString(DrawingColor.LightCoral)}");

		if (Interlocked.Read(ref TotalCachedSize) > Config.SuspendedCache.MaxCacheSize) {
			TrimEvent.Set();
		}
	}

	internal static ManagedSpriteInstance? Fetch(ulong hash) {
		if (!Config.SuspendedCache.Enabled) {
			return null;
		}

		return Cache.Get(hash);
	}

	internal static bool TryFetch(ulong hash, [NotNullWhen(true)] out ManagedSpriteInstance? instance) {
		instance = Fetch(hash);
		return instance is not null;
	}

	internal static bool Remove(ulong hash) {
		var element = Cache.Remove(hash);
		return element is not null;
	}

	internal static bool Remove(ManagedSpriteInstance instance) => Remove(instance.Hash);

	internal static void Purge() {
		Cache.Clear();
		TotalCachedSize = 0L;
	}

	[SMAPIConsole.StatsMethod]
	internal static string[] DumpStats() {
		var statsLines = new List<string> {
			$"\tTotal Suspended Elements: {Cache.Count}",
			$"\tTotal Memory Size       : {Interlocked.Read(ref TotalCachedSize).AsDataSize()}"
		};
		return statsLines.ToArray();
	}
}
