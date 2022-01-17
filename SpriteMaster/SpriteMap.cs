using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster;

// TODO : This class, and Texture2DMeta, have a _lot_ of inter-play and it makes it very confusing.
// This needs to be cleaned up badly.
static class SpriteMap {
	private static readonly SharedLock Lock = new();
	private static readonly WeakCollection<ManagedSpriteInstance> SpriteInstanceReferences = new();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong SpriteHash(Texture2D texture, in Bounds source, uint expectedScale) {
		return Hashing.Combine(source.Hash(), expectedScale.GetSafeHash());
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Add(Texture2D reference, ManagedSpriteInstance texture) {
		var meta = reference.Meta();
		using (Lock.Write) {
			SpriteInstanceReferences.Add(texture);  
			using (meta.Lock.Write) {
				return meta.SpriteInstanceTable.TryAdd(texture.SpriteMapHash, texture);
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool TryGetReady(Texture2D texture, in Bounds source, uint expectedScale, [NotNullWhen(true)] out ManagedSpriteInstance? result) {
		if (TryGet(texture, source, expectedScale, out var internalResult) && internalResult.IsReady) {
			result = internalResult;
			return true;
		}
		result = null;
		return false;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool TryGet(Texture2D texture, in Bounds source, uint expectedScale, [NotNullWhen(true)] out ManagedSpriteInstance? result) {
		var rectangleHash = SpriteHash(texture: texture, source: source, expectedScale: expectedScale);

		var meta = texture.Meta();
		var Map = meta.SpriteInstanceTable;
		using (meta.Lock.Read) {
			if (Map.TryGetValue(rectangleHash, out var spriteInstance)) {
				if (spriteInstance.Texture?.IsDisposed == true) {
					var removeList = new List<ulong>();
					using (meta.Lock.Promote) {
						foreach (var skv in meta.SpriteInstanceTable) {
							if (skv.Value?.Texture?.IsDisposed ?? false) {
								removeList.Add(skv.Key);
								skv.Value.Dispose();
							}
						}
						foreach (var key in removeList) {
							meta.SpriteInstanceTable.Remove(key);
						}
					}
				}
				else {
					result = spriteInstance;
					return true;
				}
			}
		}

		result = null;
		return false;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Remove(ManagedSpriteInstance spriteInstance, Texture2D texture) {
		try {
			var meta = texture.Meta();
			var spriteTable = meta.SpriteInstanceTable;

			using (Lock.Write) {
				try {
					SpriteInstanceReferences.Purge();
					var removeElements = new List<ManagedSpriteInstance>();
					foreach (var element in SpriteInstanceReferences) {
						if (element == spriteInstance) {
							removeElements.Add(element);
						}
					}

					foreach (var element in removeElements) {
						SpriteInstanceReferences.Remove(element);
					}
				}
				catch { }
			}
			using (meta.Lock.Write) {
				if (spriteTable.TryGetValue(spriteInstance.SpriteMapHash, out var currentValue) && currentValue == spriteInstance) {
					spriteTable.Remove(spriteInstance.SpriteMapHash);
					currentValue?.Dispose();
				}
			}
		}
		finally {
			if (spriteInstance.Texture != null && !spriteInstance.Texture.IsDisposed) {
				Debug.TraceLn($"Disposing Active HD Texture: {spriteInstance.SafeName()}");

				//spriteInstance.Texture.Dispose();
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Purge(Texture2D reference, in Bounds? sourceRectangle = null) {
		try {
			var meta = reference.Meta();
			var spriteTable = meta.SpriteInstanceTable;
			using (meta.Lock.Read) {
				if (spriteTable.Count == 0) {
					return;
				}

				// TODO : handle sourceRectangle meaningfully.
				Debug.TraceLn($"Purging Texture {reference.SafeName()}");

				bool hasSourceRect = sourceRectangle.HasValue;

				var removeTexture = hasSourceRect ? new List<ulong>() : null!;

				foreach (var pairs in spriteTable) {
					var spriteInstance = pairs.Value;
					lock (spriteInstance) {
						if (sourceRectangle.HasValue && !spriteInstance.OriginalSourceRectangle.Overlaps(sourceRectangle.Value)) {
							continue;
						}
						if (spriteInstance.Texture is not null) {
							// TODO : should this be locked?
							spriteInstance.Texture.Dispose();
						}
						spriteInstance.Texture = null;
						if (hasSourceRect) {
							spriteInstance.Dispose();
							removeTexture.Add(pairs.Key);
						}
					}
				}

				using (meta.Lock.Promote) {
					if (hasSourceRect) {
						foreach (var hash in removeTexture) {
							spriteTable.Remove(hash);
						}
					}
					else {
						meta.ClearSpriteInstanceTable();
					}
				}
				// : TODO dispose sprites?
			}
		}
		catch { }
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void SeasonPurge(string season) {
		try {
			var purgeList = new List<ManagedSpriteInstance>();
			using (Lock.Read) {
				foreach (var spriteInstance in SpriteInstanceReferences) {
					if (spriteInstance is null || spriteInstance.Anonymous()) {
						continue;
					}

					var textureName = spriteInstance.SafeName().ToLowerInvariant();
					if (
						!textureName.Contains(season) &&
						(
							textureName.Contains("spring") ||
							textureName.Contains("summer") ||
							textureName.Contains("fall") ||
							textureName.Contains("winter")
						)
					) {
						purgeList.Add(spriteInstance);
					}
				}
			}
			foreach (var purgable in purgeList) {
				if (purgable.Reference.TryGetTarget(out var reference)) {
					purgable.Dispose();
					var meta = reference.Meta();
					var spriteTable = meta.SpriteInstanceTable;
					using (meta.Lock.Write) {
						meta.ClearSpriteInstanceTable();
					}
				}
			}
		}
		catch { }
	}

	internal static Dictionary<Texture2D, List<ManagedSpriteInstance>> GetDump() {
		var result = new Dictionary<Texture2D, List<ManagedSpriteInstance>>();

		foreach (var spriteInstance in SpriteInstanceReferences) {
			if (spriteInstance is not null && spriteInstance.Reference.TryGetTarget(out var referenceTexture)) {
				if (!result.TryGetValue(referenceTexture, out var resultList)) {
					resultList = new List<ManagedSpriteInstance>();
					result.Add(referenceTexture, resultList);
				}
				resultList.Add(spriteInstance);
			}
		}

		return result;
	}
}
