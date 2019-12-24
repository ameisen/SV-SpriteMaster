﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading;

using System.Linq;
using System.Collections.Generic;

using WeakTexture = System.WeakReference<Microsoft.Xna.Framework.Graphics.Texture2D>;
using WeakScaledTexture = System.WeakReference<SpriteMaster.ScaledTexture>;
using WeakTextureMap = System.Runtime.CompilerServices.ConditionalWeakTable<Microsoft.Xna.Framework.Graphics.Texture2D, SpriteMaster.ScaledTexture>;
using WeakSpriteMap = System.Runtime.CompilerServices.ConditionalWeakTable<Microsoft.Xna.Framework.Graphics.Texture2D, System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>>;
using SpriteMaster.Types;
using SpriteMaster.Extensions;
using TeximpNet.Compression;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal abstract class ITextureMap {
		protected readonly SharedLock Lock = new SharedLock();
		protected readonly List<WeakScaledTexture> ScaledTextureReferences = new List<WeakScaledTexture>();

		internal static ITextureMap Create () {
			return Config.Resample.DeSprite ? (ITextureMap)new SpriteMap() : new TextureMap();
		}

		internal abstract void Add (Texture2D reference, ScaledTexture texture, Bounds sourceRectangle);

		internal abstract bool TryGet (Texture2D texture, Bounds sourceRectangle, out ScaledTexture result);

		internal abstract void Remove (ScaledTexture scaledTexture, Texture2D texture);

		internal abstract void Purge (Texture2D reference, Bounds? sourceRectangle = null);

		internal abstract void SeasonPurge (string season);

		protected void OnAdd (Texture2D reference, ScaledTexture texture, Bounds sourceRectangle) {
			ScaledTextureReferences.Add(texture.MakeWeak());
		}

		protected void OnRemove (in ScaledTexture scaledTexture, in Texture2D texture) {
			try {
				List<WeakScaledTexture> removeElements = new List<WeakScaledTexture>();
				foreach (var element in ScaledTextureReferences) {
					if (element.TryGetTarget(out var elementTexture)) {
						if (elementTexture == scaledTexture) {
							removeElements.Add(element);
						}
					}
					else {
						removeElements.Add(element);
					}
				}

				foreach (var element in removeElements) {
					ScaledTextureReferences.Remove(element);
				}
			}
			catch { }
		}

		internal Dictionary<Texture2D, List<ScaledTexture>> GetDump () {
			var result = new Dictionary<Texture2D, List<ScaledTexture>>();

			foreach (var element in ScaledTextureReferences) {
				if (element.TryGetTarget(out var scaledTexture)) {
					if (scaledTexture.Reference.TryGetTarget(out var referenceTexture)) {
						List<ScaledTexture> resultList;
						if (!result.TryGetValue(referenceTexture, out resultList)) {
							resultList = new List<ScaledTexture>();
							result.Add(referenceTexture, resultList);
						}
						resultList.Add(scaledTexture);
					}
				}
			}

			return result;
		}
	}

	sealed class TextureMap : ITextureMap {
		private readonly WeakTextureMap Map = new WeakTextureMap();

		internal override void Add (Texture2D reference, ScaledTexture texture, Bounds sourceRectangle) {
			using (Lock.Exclusive) {
				Map.Add(reference, texture);
				OnAdd(reference, texture, sourceRectangle);
			}
		}

		internal override bool TryGet (Texture2D texture, Bounds sourceRectangle, out ScaledTexture result) {
			result = null;

			using (Lock.Shared) {
				if (Map.TryGetValue(texture, out var scaledTexture)) {
					if (scaledTexture.Texture != null && scaledTexture.Texture.IsDisposed) {
						using (Lock.Promote) {
							Map.Remove(texture);
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

		internal override void Remove (ScaledTexture scaledTexture, Texture2D texture) {
			try {
				using (Lock.Exclusive) {
					OnRemove(scaledTexture, texture);
					Map.Remove(texture);
				}
			}
			finally {
				if (scaledTexture.Texture != null && !scaledTexture.Texture.IsDisposed) {
					Debug.InfoLn($"Disposing Active HD Texture: {scaledTexture.SafeName()}");

					//scaledTexture.Texture.Dispose();
				}
			}
		}

		internal override void Purge (Texture2D reference, Bounds? sourceRectangle = null) {
			try {
				using (Lock.Shared) {
					if (Map.TryGetValue(reference, out var scaledTexture)) {
						using (Lock.Promote) {
							Debug.InfoLn($"Purging Texture {reference.SafeName()}");
							if (scaledTexture.Texture != null) {
								lock (scaledTexture) {
									scaledTexture.Texture.Purge();
									scaledTexture.Texture = null;
								}
							}
							Map.Remove(reference);
							// TODO dispose sprite?
						}
					}
				}
			}
			catch { }
		}

		internal override void SeasonPurge (string season) {
			try {
				var purgeList = new List<ScaledTexture>();
				using (Lock.Shared) {
					foreach (var weakRef in ScaledTextureReferences) {
						if (weakRef.TryGetTarget(out var scaledTexture)) {
							if (
								(scaledTexture.Name.ToLower().Contains("spring") ||
								scaledTexture.Name.ToLower().Contains("summer") ||
								scaledTexture.Name.ToLower().Contains("fall") ||
								scaledTexture.Name.ToLower().Contains("winter")) &&
								!scaledTexture.Name.ToLower().Contains(season.ToLower())
							) {
								purgeList.Add(scaledTexture);
							}
						}
					}
				}
				foreach (var purgable in purgeList) {
					if (purgable.Reference.TryGetTarget(out var reference)) {
						purgable.Destroy(reference);
						Map.Remove(reference);
					}
				}
			}
			catch { }
		}
	}

	sealed class SpriteMap : ITextureMap {
		private readonly WeakSpriteMap Map = new WeakSpriteMap();

		static private ulong SpriteHash (Texture2D texture, Bounds sourceRectangle) {
			return ScaledTexture.ExcludeSprite(texture) ? 0UL : sourceRectangle.Hash();
		}

		internal override void Add (Texture2D reference, ScaledTexture texture, Bounds sourceRectangle) {
			using (Lock.Exclusive) {
				OnAdd(reference, texture, sourceRectangle);

				var rectangleHash = SpriteHash(reference, sourceRectangle);

				var spriteMap = Map.GetOrCreateValue(reference);
				spriteMap.Add(rectangleHash, texture);

				ScaledTextureReferences.Add(texture.MakeWeak());
			}
		}

		internal override bool TryGet (Texture2D texture, Bounds sourceRectangle, out ScaledTexture result) {
			result = null;

			using (Lock.Shared) {
				if (Map.TryGetValue(texture, out var spriteMap)) {
					var rectangleHash = SpriteHash(texture, sourceRectangle);
					if (spriteMap.TryGetValue(rectangleHash, out var scaledTexture)) {
						if (scaledTexture.Texture != null && scaledTexture.Texture.IsDisposed) {
							using (Lock.Promote) {
								Map.Remove(texture);
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
			}

			return false;
		}

		internal override void Remove (ScaledTexture scaledTexture, Texture2D texture) {
			try {
				using (Lock.Exclusive) {
					OnRemove(scaledTexture, texture);
					Map.Remove(texture);
				}
			}
			finally {
				if (scaledTexture.Texture != null && !scaledTexture.Texture.IsDisposed) {
					Debug.InfoLn($"Disposing Active HD Texture: {scaledTexture.SafeName()}");

					//scaledTexture.Texture.Dispose();
				}
			}
		}

		internal override void Purge (Texture2D reference, Bounds? sourceRectangle = null) {
			try {
				using (Lock.Shared) {
					if (Map.TryGetValue(reference, out var scaledTextureMap)) {
						// TODO handle sourceRectangle meaningfully.
						using (Lock.Promote) {
							Debug.InfoLn($"Purging Texture {reference.SafeName()}");

							foreach (var scaledTexture in scaledTextureMap.Values) {
								if (scaledTexture.Texture != null) {
									lock (scaledTexture) {
										scaledTexture.Texture.Purge();
										scaledTexture.Texture = null;
									}
								}
							}

							Map.Remove(reference);
							// TODO dispose sprites?
						}
					}
				}
			}
			catch { }
		}

		internal override void SeasonPurge (string season) {
			try {
				var purgeList = new List<ScaledTexture>();
				using (Lock.Shared) {
					foreach (var weakRef in ScaledTextureReferences) {
						if (weakRef.TryGetTarget(out var scaledTexture)) {
							if (
								(scaledTexture.Name.ToLower().Contains("spring") ||
								scaledTexture.Name.ToLower().Contains("summer") ||
								scaledTexture.Name.ToLower().Contains("fall") ||
								scaledTexture.Name.ToLower().Contains("winter")) &&
								!scaledTexture.Name.ToLower().Contains(season.ToLower())
							) {
								purgeList.Add(scaledTexture);
							}
						}
					}
				}
				foreach (var purgable in purgeList) {
					if (purgable.Reference.TryGetTarget(out var reference)) {
						purgable.Destroy(reference);
						Map.Remove(reference);
					}
				}
			}
			catch { }
		}
	}

	internal sealed class ScaledTexture {
		// TODO : This can grow unbounded. Should fix.
		public static readonly ITextureMap TextureMap = ITextureMap.Create();

		private static readonly List<Action> PendingActions = Config.AsyncScaling.Enabled ? new List<Action>() : null;

		private static readonly Dictionary<string, WeakTexture> DuplicateTable = Config.DiscardDuplicates ? new Dictionary<string, WeakTexture>() : null;

		static internal bool ExcludeSprite (Texture2D texture) {
			return false;// && (texture.Name == "LooseSprites\\Cursors");
		}

		static internal bool HasPendingActions () {
			if (!Config.AsyncScaling.Enabled) {
				return false;
			}
			lock (PendingActions) {
				return PendingActions.Count != 0;
			}
		}

		static internal void AddPendingAction (Action action) {
			lock (PendingActions) {
				PendingActions.Add(action);
			}
		}

		static internal void ProcessPendingActions (int processCount = -1) {
			if (!Config.AsyncScaling.Enabled) {
				return;
			}

			if (processCount < 0) {
				processCount = Config.AsyncScaling.MaxLoadsPerFrame;
				if (processCount < 0) {
					processCount = int.MaxValue;
				}
			}

			// TODO : use GetUpdateToken

			lock (PendingActions) {
				if (processCount >= PendingActions.Count) {
					foreach (var action in PendingActions) {
						action.Invoke();
					}
					PendingActions.Clear();
				}
				else {
					while (processCount-- > 0) {
						PendingActions.Last().Invoke();
						PendingActions.RemoveAt(PendingActions.Count - 1);
					}
				}
			}
		}

		private static bool LegalFormat(Texture2D texture) {
			return texture.Format == SurfaceFormat.Color;
		}

		private static bool Validate(Texture2D texture) {
			int textureArea = texture.Width * texture.Height;

			if (textureArea == 0 || texture.IsDisposed) {
				return false;
			}

			if (Config.IgnoreUnknownTextures && (texture.Name == null || texture.Name == "")) {
				return false;
			}

			if (!LegalFormat(texture)) {
				return false;
			}

			return true;
		}

		static internal ScaledTexture Fetch (Texture2D texture, Bounds sourceRectangle, Bounds indexRectangle, bool allowPadding = true) {
			if (!Validate(texture)) {
				return null;
			}

			if (TextureMap.TryGet(texture, indexRectangle, out var scaleTexture)) {
				return scaleTexture;
			}

			return null;
		}

		static internal ScaledTexture Get (Texture2D texture, Bounds sourceRectangle, Bounds indexRectangle, bool allowPadding = true) {
			if (!Validate(texture)) {
				return null;
			}

			if (TextureMap.TryGet(texture, indexRectangle, out var scaleTexture)) {
				return scaleTexture;
			}

			bool useAsync = (Config.AsyncScaling.EnabledForUnknownTextures || !texture.Name.IsBlank()) && (texture.Area() >= Config.AsyncScaling.MinimumSizeTexels);

			if (useAsync && Config.AsyncScaling.Enabled && !DrawState.GetUpdateToken(texture.Width * texture.Height)) {
				return null;
			}

			if (useAsync && Config.AsyncScaling.Enabled && RemainingAsyncTasks <= 0) {
				return null;
			}

			if (Config.DiscardDuplicates) {
				// Check for duplicates with the same name.
				// TODO : We do have a synchronity issue here. We could purge before an asynchronous task adds the texture.
				// DiscardDuplicatesFrameDelay
				if (texture.Name != null && texture.Name != "" && texture.Name != "LooseSprites\\Cursors" && texture.Name != "Minigames\\TitleButtons") {
					bool insert = false;
					if (DuplicateTable.TryGetValue(texture.Name, out var weakTexture)) {
						if (weakTexture.TryGetTarget(out var strongTexture)) {
							if (strongTexture != texture) {
								DuplicateTable.Remove(texture.Name);
								Debug.WarningLn($"Purging Duplicate Texture '{texture.Name}'");
								Purge(strongTexture);
								insert = true;
							}
						}
						else {
							DuplicateTable.Remove(texture.Name);
							insert = true;
						}
					}
					else {
						insert = true;
					}
					if (insert) {
						DuplicateTable.Add(texture.Name, texture.MakeWeak());
					}
				}
			}

			bool isSprite = Config.Resample.DeSprite && !ExcludeSprite(texture) && ((sourceRectangle.X != 0 || sourceRectangle.Y != 0) || (sourceRectangle.Width != texture.Width || sourceRectangle.Height != texture.Height));
			var textureWrapper = new TextureWrapper(texture, sourceRectangle, indexRectangle);
			ulong hash = Upscaler.GetHash(textureWrapper, isSprite);

			if (Config.EnableCachedHashTextures) {
				lock (LocalTextureCache) {
					if (LocalTextureCache.TryGetValue(hash, out var scaledTextureRef)) {
						if (scaledTextureRef.TryGetTarget(out var cachedTexture)) {
							Debug.InfoLn($"Using Cached Texture for \"{cachedTexture.SafeName()}\"");
							TextureMap.Add(texture, cachedTexture, indexRectangle);
							texture.Disposing += (object sender, EventArgs args) => { cachedTexture.OnParentDispose((Texture2D)sender); };
							if (!cachedTexture.IsReady || cachedTexture.Texture == null) {
								return null;
							}
							return cachedTexture;
						}
						else {
							LocalTextureCache.Remove(hash);
						}
					}
				}
			}

			if (TotalMemoryUsage >= Config.MaxMemoryUsage) {
				Debug.ErrorLn($"Over Max Memory Usage: {TotalMemoryUsage.AsDataSize()}");
			}

			ScaledTexture newTexture = null;
			const int scale = Config.Resample.Scale ? 2 : 1;
			newTexture = new ScaledTexture(
				assetName: texture.Name,
				textureWrapper: textureWrapper,
				sourceRectangle: sourceRectangle,
				indexRectangle: indexRectangle,
				scale: scale,
				isSprite: isSprite,
				hash: hash,
				allowPadding: allowPadding,
				async: useAsync
			);
			if (Config.EnableCachedHashTextures)
				lock (LocalTextureCache) {
					LocalTextureCache.Add(hash, newTexture.MakeWeak());
				}
			if (useAsync && Config.AsyncScaling.Enabled) {
				// It adds itself to the relevant maps.
				if (newTexture.IsReady && newTexture.Texture != null) {
					return newTexture;
				}
				return null;
			}
			else {
				return newTexture;
			}

		}

		internal ManagedTexture2D Texture = null;
		internal readonly string Name;
		internal Vector2 Scale;
		internal readonly bool IsSprite;
		internal volatile bool IsReady = false;

		internal Vector2B Wrapped = new Vector2B(false);

		internal readonly WeakTexture Reference;
		internal readonly Bounds OriginalSourceRectangle;
		internal readonly ulong Hash;

		internal Vector2I Padding = Vector2I.Zero;
		internal Vector2I UnpaddedSize;
		internal Vector2I BlockPadding = Vector2I.Zero;
		private readonly Vector2I originalSize;
		private readonly Bounds sourceRectangle;
		private int refScale;

		internal Vector2 AdjustedScale = Vector2.One;

		~ScaledTexture() {
			if (Texture != null) {
				Texture.Purge();
			}
		}

		internal static volatile uint TotalMemoryUsage = 0;

		internal long MemorySize {
			get {
				if (!IsReady || Texture == null) {
					return 0;
				}
				return Texture.Width * Texture.Height * sizeof(int);
			}
		}

		internal long OriginalMemorySize {
			get {
				return originalSize.Width * originalSize.Height * sizeof(int);
			}
		}

		internal static readonly Dictionary<ulong, WeakScaledTexture> LocalTextureCache = new Dictionary<ulong, WeakScaledTexture>();

		internal static void Purge (Texture2D reference) {
			Purge<byte>(reference, null, DataRef<byte>.Null);
		}

		internal static void Purge<T> (Texture2D reference, Bounds? bounds, DataRef<T> data) where T : struct {
			TextureMap.Purge(reference, bounds);
			Upscaler.PurgeHash(reference);
			TextureWrapper.Purge(reference, bounds, data);
		}

		internal sealed class ManagedTexture2D : Texture2D {
			private static long TotalAllocatedSize = 0;
			private static int TotalManagedTextures = 0;

			public readonly WeakReference<Texture2D> Reference;
			public readonly ScaledTexture Texture;
			public readonly Vector2I Dimensions;

			[Conditional("DEBUG")]
			private static void _DumpStats () {
				DumpStats();
			}

			internal static void DumpStats() {
				var currentProcess = Process.GetCurrentProcess();
				var workingSet = currentProcess.WorkingSet64;
				var vmem = currentProcess.VirtualMemorySize64;
				var gca = GC.GetTotalMemory(false);
				Debug.InfoLn($"Total Managed Textures : {TotalManagedTextures}");
				Debug.InfoLn($"Total Texture Size     : {TotalAllocatedSize.AsDataSize()}");
				Debug.InfoLn($"Process Working Set    : {workingSet.AsDataSize()}");
				Debug.InfoLn($"Process Virtual Memory : {vmem.AsDataSize()}");
				Debug.InfoLn($"GC Allocated Memory    : {gca.AsDataSize()}");
			}

			public ManagedTexture2D (
				ScaledTexture texture,
				Texture2D reference,
				Vector2I dimensions,
				SurfaceFormat format,
				int[] data = null,
				string name = null
			) : base(reference.GraphicsDevice, dimensions.Width, dimensions.Height, false, format) {
				if (name != null) {
					this.Name = name;
				}
				else {
					this.Name = $"{reference.SafeName()} [RESAMPLED {(CompressionFormat)format}]";
				}

				Reference = reference.MakeWeak();
				Texture = texture;
				Dimensions = dimensions - texture.BlockPadding;

				reference.Disposing += (object obj, EventArgs args) => OnParentDispose();

				TotalAllocatedSize += this.SizeBytes();
				++TotalManagedTextures;

				//_DumpStats();

				Garbage.MarkOwned(format, dimensions.Area);
				Disposing += (object obj, EventArgs args) => {
					Garbage.UnmarkOwned(format, dimensions.Area);
					TotalAllocatedSize -= this.SizeBytes();
					--TotalManagedTextures;
				};

				if (data != null) {
					this.SetDataEx(data);
				}
			}

			~ManagedTexture2D() {
				if (!IsDisposed) {
					Dispose(false);
				}
			}

			public void Purge(bool onlySelf = true) {
			}

			private void OnParentDispose() {
				if (!IsDisposed) {
					Dispose();
				}
			}
		}

		internal static volatile int RemainingAsyncTasks = Config.AsyncScaling.MaxInFlightTasks;

		internal ScaledTexture (string assetName, TextureWrapper textureWrapper, Bounds sourceRectangle, Bounds indexRectangle, int scale, ulong hash, bool isSprite, bool allowPadding, bool async) {
			IsSprite = isSprite;
			Hash = hash;
			var source = textureWrapper.Reference;

			this.OriginalSourceRectangle = new Bounds(sourceRectangle);
			this.Reference = source.MakeWeak();
			this.sourceRectangle = sourceRectangle;
			this.refScale = scale;
			TextureMap.Add(source, this, indexRectangle);

			this.Name = source.Name.IsBlank() ? assetName : source.Name;
			originalSize = IsSprite ? sourceRectangle.Extent : new Vector2I(source);

			if (async && Config.AsyncScaling.Enabled) {
				ThreadPool.QueueUserWorkItem((object wrapper) => {
					--RemainingAsyncTasks;
					try {
						Thread.CurrentThread.Priority = ThreadPriority.Lowest;
						Thread.CurrentThread.Name = "Texture Resampling Thread";
						Upscaler.Upscale(
							texture: this,
							scale: ref refScale,
							input: (TextureWrapper)wrapper,
							desprite: IsSprite,
							hash: Hash,
							wrapped: ref Wrapped,
							allowPadding: allowPadding,
							async: true
						);
						// If the upscale fails, the asynchronous action on the render thread automatically cleans up the ScaledTexture.
					}
					finally {
						++RemainingAsyncTasks;
					}
				}, textureWrapper);
			}
			else {
				// TODO store the HD Texture in _this_ object instead. Will confuse things like subtexture updates, though.
				this.Texture = (ManagedTexture2D)Upscaler.Upscale(
					texture: this,
					scale: ref refScale,
					input: textureWrapper,
					desprite: IsSprite,
					hash: Hash,
					wrapped: ref Wrapped,
					allowPadding: allowPadding,
					async: false
				);

				if (this.Texture != null) {
					Finish();
				}
				else {
					Destroy(source);
					return;
				}
			}

			// TODO : I would love to dispose of this texture _now_, but we rely on it disposing to know if we need to dispose of ours.
			// There is a better way to do this using weak references, I just need to analyze it further. Memory leaks have been a pain so far.
			source.Disposing += (object sender, EventArgs args) => { OnParentDispose((Texture2D)sender); };
		}

		// Async Call
		internal void Finish () {
			ManagedTexture2D texture;
			lock (this) {
				texture = Texture;
			}

			if (texture == null || texture.IsDisposed) {
				return;
			}

			TotalMemoryUsage += (uint)texture.SizeBytes();
			texture.Disposing += (object sender, EventArgs args) => { TotalMemoryUsage -= (uint)texture.SizeBytes(); };

			if (IsSprite) {
				Debug.InfoLn($"Creating HD Sprite [{texture.Format} x{refScale}]: {this.SafeName()} {sourceRectangle}");
			}
			else {
				Debug.InfoLn($"Creating HD Spritesheet [{texture.Format} x{refScale}]: {this.SafeName()}");
			}

			this.Scale = (Vector2)texture.Dimensions / new Vector2(originalSize.Width, originalSize.Height);

			if (Config.RestrictSize) {
				var scaledSize = originalSize * refScale;
				if (scaledSize.Height > Config.ClampDimension) {
					Scale.Y = 1f;
				}
				if (scaledSize.Width > Config.ClampDimension) {
					Scale.X = 1f;
				}
			}

			IsReady = true;
		}

		internal void Destroy (Texture2D texture) {
			TextureMap.Remove(this, texture);
		}

		private void OnParentDispose (Texture2D texture) {
			Debug.InfoLn($"Parent Texture Disposing: {texture.SafeName()}");

			Destroy(texture);
		}
	}
}
