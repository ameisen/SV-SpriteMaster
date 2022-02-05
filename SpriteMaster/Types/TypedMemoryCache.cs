using System.Runtime.Caching;

namespace SpriteMaster.Types;

class TypedMemoryCache<T> where T : class {
	private MemoryCache Cache;

	internal TypedMemoryCache(string name) {
		Cache = new(name);
	}

	internal long Count => Cache.GetCount();

	internal T? Get(string key) => Cache.Get(key) as T;

	internal T Set(string key, T value) => ((Cache[key] = value) as T)!;

	internal T? Remove(string key) => Cache.Remove(key) as T;

	internal void Clear() {
		var name = Cache.Name;
		Cache.Dispose();
		Cache = new(name);
	}
}
