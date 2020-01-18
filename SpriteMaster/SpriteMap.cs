using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal sealed class SpriteMap {
		private readonly SharedLock Lock = new SharedLock();
		private readonly WeakCollection<ScaledTexture> ScaledTextureReferences = new WeakCollection<ScaledTexture>();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static private ulong SpriteHash (Texture2D texture, Bounds source, int expectedScale) {
			return Hash.Combine(source.Hash(), expectedScale.GetHashCode());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Add (Texture2D reference, ScaledTexture texture, Bounds source, int expectedScale) {
			var rectangleHash = SpriteHash(texture: reference, source: source, expectedScale: expectedScale);

			var meta = reference.Meta();
			using (Lock.Exclusive) {
				ScaledTextureReferences.Add(texture);
				using (meta.Lock.Exclusive) {
					meta.SpriteTable.Add(rectangleHash, texture);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool TryGet (Texture2D texture, Bounds source, int expectedScale, out ScaledTexture result) {
			result = null;
			var rectangleHash = SpriteHash(texture: texture, source: source, expectedScale: expectedScale);

			var meta = texture.Meta();
			var Map = meta.SpriteTable;
			using (meta.Lock.Shared) {
				if (Map.TryGetValue(rectangleHash, out var scaledTexture)) {
					if (scaledTexture.Texture?.IsDisposed == true) {
						using (meta.Lock.Promote) {
							Map.Clear();
						}
					}
					else {
						if (scaledTexture.IsReady) {
							result = scaledTexture;
						}
						return true;
					}
				}
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Remove (ScaledTexture scaledTexture, Texture2D texture) {
			try {
				var meta = texture.Meta();

				using (Lock.Exclusive) {
					try {
						ScaledTextureReferences.Purge();
						var removeElements = new List<ScaledTexture>();
						foreach (var element in ScaledTextureReferences) {
							if (element == scaledTexture) {
								removeElements.Add(element);
							}
						}

						foreach (var element in removeElements) {
							ScaledTextureReferences.Remove(element);
						}
					}
					catch { }

					using (meta.Lock.Exclusive) {
						meta.SpriteTable.Clear();
					}
				}
			}
			finally {
				if (scaledTexture.Texture != null && !scaledTexture.Texture.IsDisposed) {
					Debug.TraceLn($"Disposing Active HD Texture: {scaledTexture.SafeName()}");

					//scaledTexture.Texture.Dispose();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Purge (Texture2D reference, Bounds? sourceRectangle = null) {
			try {
				var meta = reference.Meta();
				var Map = meta.SpriteTable;
				using (meta.Lock.Shared) {
					if (!Map.Any()) {
						return;
					}

					// TODO handle sourceRectangle meaningfully.
					using (Lock.Exclusive) {
						Debug.TraceLn($"Purging Texture {reference.SafeName()}");

						foreach (var scaledTexture in Map.Values) {
							lock (scaledTexture) {
								if (scaledTexture.Texture != null) {
									scaledTexture.Texture.Dispose();
								}
								scaledTexture.Texture = null;
							}
						}

						using (meta.Lock.Promote) {
							Map.Clear();
						}
						// TODO dispose sprites?
					}
				}
			}
			catch { }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SeasonPurge (string season) {
			try {
				var purgeList = new List<ScaledTexture>();
				using (Lock.Shared) {
					foreach (var scaledTexture in ScaledTextureReferences) {
						if (scaledTexture.Anonymous())
							continue;
						var textureName = scaledTexture.SafeName().ToLowerInvariant();
						if (
							(
								textureName.Contains("spring") ||
								textureName.Contains("summer") ||
								textureName.Contains("fall") ||
								textureName.Contains("winter")
							) && !textureName.Contains(season.ToLowerInvariant())
						) {
							purgeList.Add(scaledTexture);
						}
					}
					using (Lock.Promote) {
						foreach (var purgable in purgeList) {
							if (purgable.Reference.TryGetTarget(out var reference)) {
								purgable.Dispose();
								var meta = reference.Meta();
								using (meta.Lock.Exclusive) {
									meta.SpriteTable.Clear();
								}
							}
						}
					}
				}
			}
			catch { }
		}

		internal Dictionary<Texture2D, List<ScaledTexture>> GetDump () {
			var result = new Dictionary<Texture2D, List<ScaledTexture>>();

			foreach (var scaledTexture in ScaledTextureReferences) {
				if (scaledTexture.Reference.TryGetTarget(out var referenceTexture)) {
					List<ScaledTexture> resultList;
					if (!result.TryGetValue(referenceTexture, out resultList)) {
						resultList = new List<ScaledTexture>();
						result.Add(referenceTexture, resultList);
					}
					resultList.Add(scaledTexture);
				}
			}

			return result;
		}
	}
}
