using SpriteMaster.Configuration;
using SpriteMaster.Types;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security;

namespace SpriteMaster.Caching;

/// <summary>
/// Used to cache original texture data so it doesn't need to perform blocking fetches as often
/// </summary>
[SuppressUnmanagedCodeSecurity]
internal static class ResidentCache {
	internal static bool Enabled => Config.ResidentCache.Enabled;

	private static readonly TypedMemoryCache<ulong, byte[]> Cache = CreateCache();

	private static TypedMemoryCache<ulong, byte[]> CreateCache() => new(
		name: "ResidentCache",
		removalAction: null,
		maxSize: Config.ResidentCache.MaxSize
	);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static byte[]? Get(ulong key) =>
		Cache.Get(key);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static bool TryGet(ulong key, [NotNullWhen(true)] out byte[]? value) =>
		Cache.TryGet(key, out value);

	internal static byte[] Set(ulong key, byte[] value) =>
		Cache.Set(key, value, size: value.Length * sizeof(byte));

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static byte[]? Remove(ulong key) =>
		Cache.Remove(key);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static void Purge() {
		Cache.Clear();
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static void OnSettingsChanged() {
		if (!Enabled) {
			Purge();
		}
	}
}
