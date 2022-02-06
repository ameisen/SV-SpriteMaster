using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System.Diagnostics.CodeAnalysis;

namespace SpriteMaster;

static class SuspendedSpriteCache {
	private static readonly TypedMemoryCache<ManagedSpriteInstance> Cache = new("SuspendedSpriteCache");

	internal static void Add(ManagedSpriteInstance instance) {
		if (!Config.SuspendedCache.Enabled) {
			instance.Dispose();
			return;
		}

		var key = instance.Hash.ToString64();
		Cache.Set(key, instance);
		Debug.Trace($"SuspendedSpriteCache Size: {Cache.Count.ToString(System.Drawing.Color.LightCoral)}");
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
		var result = Cache.Remove(hash.ToString64()) is not null;
		return result;
	}

	internal static bool Remove(ManagedSpriteInstance instance) => Remove(instance.Hash);
}
