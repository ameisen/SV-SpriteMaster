using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Caching;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using SpriteMaster.Types.Interlocked;
using System;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using SpriteDictionary = System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>;

namespace SpriteMaster.Metadata;

sealed class MTexture2D {
	internal readonly SpriteDictionary SpriteTable = new();

	/// <summary>The current (static) ID, incremented every time a new <see cref="MTexture2D"/> is created.</summary>
	private static ulong CurrentID = 0U;
	/// <summary>Whenever a new <see cref="MTexture2D"/> is created, <see cref="CurrentID"/> is incremented and <see cref="UniqueIDString"/> is set to a string representation of it.</summary>
	private readonly string UniqueIDString = Interlocked.Increment(ref CurrentID).ToString64();

	internal readonly SharedLock Lock = new();

	internal volatile bool TracePrinted = false;

	internal bool ScaleValid = true;
	internal bool IsSystemRenderTarget = false;

	internal InterlockedULong LastAccessFrame { get; private set; } = (ulong)DrawState.CurrentFrame;
	internal InterlockedULong Hash { get; private set; } = Hashing.Default;

	// TODO : this presently is not threadsafe.
	private readonly WeakReference<byte[]> _CachedData = (Config.MemoryCache.Enabled) ? new(null) : null;

	internal bool HasCachedData {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			if (!Config.MemoryCache.Enabled) {
				return false;
			}

			using (Lock.Shared) {
				return _CachedData.TryGetTarget(out var target);
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal unsafe void Purge(Texture2D reference, in Bounds? bounds, in DataRef<byte> data) {
		using (Lock.Exclusive) {
			bool hasCachedData = CachedData is not null;

			if (data.IsNull) {
				if (!hasCachedData) {
					Debug.TraceLn($"Clearing '{reference.SafeName(DrawingColor.LightYellow)}' Cache");
				}
				CachedData = null;
				return;
			}

			var refSize = (int)reference.SizeBytes();

			bool forcePurge = false;

			try {
				// TODO : lock isn't granular enough.
				if (Config.MemoryCache.AlwaysFlush) {
					forcePurge = true;
				}
				else if (!bounds.HasValue && data.Offset == 0 && data.Length == refSize) {
					Debug.TraceLn($"{(hasCachedData ? "Overriding" : "Setting")} '{reference.SafeName(DrawingColor.LightYellow)}' Cache in Purge: {bounds.HasValue}, {data.Offset}, {data.Length}");
					CachedData = data.Data;
				}
				// TODO : This doesn't update the compressed cache.
				else if (!bounds.HasValue && CachedData is var currentData && currentData is not null) {
					Debug.TraceLn($"{(hasCachedData ? "Updating" : "Setting")} '{reference.SafeName(DrawingColor.LightYellow)}' Cache in Purge: {bounds.HasValue}");
					var byteSpan = data.Data;
					var untilOffset = Math.Min(currentData.Length - data.Offset, data.Length);
					foreach (int i in 0.RangeTo(untilOffset)) {
						currentData[i + data.Offset] = byteSpan[i];
					}
					Hash = Hashing.Default;
					CachedData = currentData; // Force it to update the global cache.
				}
				else {
					if (hasCachedData) {
						Debug.TraceLn($"Forcing full '{reference.SafeName(DrawingColor.LightYellow)}' Purge");
					}
					forcePurge = true;
				}
			}
			catch (Exception ex) {
				ex.PrintInfo();
				forcePurge = true;
			}

			// TODO : maybe we need to purge more often?
			if (forcePurge && hasCachedData) {
				CachedData = null;
			}
		}
	}

	internal static readonly byte[] BlockedSentinel = new byte[1] { 0xFF };

	internal byte[] CachedDataNonBlocking {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			if (!Config.MemoryCache.Enabled) {
				return null;
			}

			using (Lock.TryShared) {
				_CachedData.TryGetTarget(out var target);
				return target;
			}
			return BlockedSentinel;
		}
	}

	internal byte[] CachedData {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			if (!Config.MemoryCache.Enabled) {
				return null;
			}

			using (Lock.Shared) {
				_CachedData.TryGetTarget(out var target);
				return target;
			}
		}
		[MethodImpl(Runtime.MethodImpl.Hot)]
		set {
			try {
				if (!Config.MemoryCache.Enabled) {
					return;
				}

				TracePrinted = false;

				//if (_CachedData.TryGetTarget(out var target) && target == value) {
				//	return;
				//}
				if (value is null) {
					using (Lock.Promote) {
						_CachedData.SetTarget(null);
						ResidentCache.Remove<byte[]>(UniqueIDString);
					}
				}
				else {
					using (Lock.Promote) {
						ResidentCache.Set(UniqueIDString, value);
						_CachedData.SetTarget(value);
					}
				}
			}
			finally {
				Hash = default;
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void UpdateLastAccess() {
		LastAccessFrame = DrawState.CurrentFrame;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ulong GetHash(SpriteInfo info) {
		using (Lock.Shared) {
			ulong hash = Hash;
			if (hash == Hashing.Default) {
				hash = info.Hash;
				using (Lock.Promote) {
					Hash = hash;
				}
			}
			return hash;
		}
	}
}
