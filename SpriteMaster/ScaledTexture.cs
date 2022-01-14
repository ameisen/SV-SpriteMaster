using LinqFasterer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pastel;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Resample;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using WeakTexture = System.WeakReference<Microsoft.Xna.Framework.Graphics.Texture2D>;

namespace SpriteMaster;
sealed partial class ScaledTexture : IDisposable {
	// TODO : This can grow unbounded. Should fix.
	internal static readonly SpriteMap SpriteMap = new();

	private static readonly LinkedList<WeakReference<ScaledTexture>> MostRecentList = new();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool LegalFormat(Texture2D texture) => AllowedFormats.ContainsF(texture.Format);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Validate(Texture2D texture) {
		var meta = texture.Meta();
		if (!meta.ScaleValid) {
			return false;
		}

		if (texture is ManagedTexture2D) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', is already scaled");
			}
			return meta.ScaleValid = false;
		}

		if (texture is InternalTexture2D) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', is an internal texture");
			}
			return meta.ScaleValid = false;
		}

		if (texture is RenderTarget2D && texture.Meta().IsSystemRenderTarget) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', system render targets unsupported");
			}
			return meta.ScaleValid = false;
		}

		if (Math.Max(texture.Width, texture.Height) <= Config.Resample.MinimumTextureDimensions) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', texture is too small to qualify ({texture.Extent().ToString(DrawingColor.Orange)})");
			}
			return meta.ScaleValid = false;
		}

		if (texture.Area() == 0) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', zero area");
			}
			return meta.ScaleValid = false;
		}

		// TODO pComPtr check?
		if (texture.IsDisposed || texture.GraphicsDevice.IsDisposed) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', Is Zombie");
			}
			return meta.ScaleValid = false;
		}

		if (Config.IgnoreUnknownTextures && texture.Anonymous()) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', Is Unknown Texture");
			}
			return meta.ScaleValid = false;
		}


		if (texture.LevelCount > 1) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', Multi-Level Textures Unsupported: {texture.LevelCount.ToString(DrawingColor.Orange)} levels");
			}
			return meta.ScaleValid = false;
		}

		if (!LegalFormat(texture)) {
			if (!meta.TracePrinted) {
				meta.TracePrinted = true;
				Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', Format Unsupported: {texture.Format.ToString(DrawingColor.Orange)}");
			}
			return meta.ScaleValid = false;
		}

		if (!texture.Anonymous()) {
			foreach (var blacklisted in Config.Resample.Blacklist) {
				if (texture.SafeName().StartsWith(blacklisted)) {
					if (!meta.TracePrinted) {
						meta.TracePrinted = true;
						Debug.TraceLn($"Not Scaling Texture '{texture.SafeName(DrawingColor.LightYellow)}', Is Blacklisted");
					}
					return meta.ScaleValid = false;
				}
			}
		}

		return true;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	static internal ScaledTexture Fetch(Texture2D texture, in Bounds source, uint expectedScale) {
		if (SpriteMap.TryGet(texture, source, expectedScale, out var scaleTexture)) {
			return scaleTexture;
		}

		return null;
	}

	private static readonly TexelTimer TexelAverage = new();
	private static readonly TexelTimer TexelAverageCached = new();
	private static readonly TexelTimer TexelAverageSync = new();
	private static readonly TexelTimer TexelAverageCachedSync = new();

	internal static void ClearTimers() {
		TexelAverage.Reset();
		TexelAverageCached.Reset();
		TexelAverageSync.Reset();
		TexelAverageCachedSync.Reset();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static TexelTimer GetTimer(bool cached, bool async) {
		if (async) {
			return cached ? TexelAverageCached : TexelAverage;
		}
		else {
			return cached ? TexelAverageCachedSync : TexelAverageSync;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static TexelTimer GetTimer(Texture2D texture, bool async, out bool isCached) {
		var IsCached = SpriteInfo.IsCached(texture);
		isCached = IsCached;
		return GetTimer(IsCached, async);
	}

	static TimeSpan MeanTimeSpan = TimeSpan.Zero;
	static int TimeSpanSamples = 0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	static internal ScaledTexture Get(Texture2D texture, in Bounds source, uint expectedScale) {
		using var _ = Performance.Track();

		if (SpriteMap.TryGet(texture, source, expectedScale, out var scaleTexture)) {
			return scaleTexture;
		}

		if (!Validate(texture)) {
			return null;
		}

		bool useStalling = Config.Resample.UseFrametimeStalling && !GameState.IsLoading;

		bool useAsync = Config.AsyncScaling.Enabled && (Config.AsyncScaling.EnabledForUnknownTextures || !texture.Anonymous()) && (source.Area >= Config.AsyncScaling.MinimumSizeTexels);
		// !texture.Meta().HasCachedData

		TimeSpan? remainingTime = null;
		bool? isCached = null;

		string getMetadataString() {
			if (isCached.HasValue) {
				return $" ({(useAsync ? "async" : "sync".Pastel(DrawingColor.Orange))} {(isCached.Value ? "cached" : "uncached".Pastel(DrawingColor.Orange))})";
			}
			else {
				return $" ({(useAsync ? "async" : "sync".Pastel(DrawingColor.Orange))})";
			}
		}

		string getNameString() {
			return $"'{texture.SafeName(DrawingColor.LightYellow)}'{getMetadataString()}";
		}

		if (useStalling && DrawState.PushedUpdateWithin(0)) {
			remainingTime = DrawState.RemainingFrameTime();
			if (remainingTime <= TimeSpan.Zero) {
				return null;
			}

			var estimatedDuration = GetTimer(texture: texture, async: useAsync, out bool cached).Estimate((int)texture.Format.SizeBytes(source.Area));
			isCached = cached;
			if (estimatedDuration > TimeSpan.Zero && estimatedDuration > remainingTime) {
				Debug.TraceLn($"Not enough frame time left to begin resampling {getNameString()} ({estimatedDuration.TotalMilliseconds.ToString(DrawingColor.LightBlue)} ms >= {remainingTime?.TotalMilliseconds.ToString(DrawingColor.LightBlue)} ms)");
				return null;
			}
		}

		// TODO : We should really only populate the average when we are performing an expensive operation like GetData.
		var watch = System.Diagnostics.Stopwatch.StartNew();

		TextureType textureType;
		if (!source.Offset.IsZero || source.Extent != texture.Extent()) {
			textureType = TextureType.Sprite;
		}
		else {
			textureType = TextureType.Image;
		}

		if (SpriteOverrides.IsWater(source, texture)) {
			//textureType = TextureType.Image;
		}

		SpriteInfo textureWrapper;

		using (Performance.Track("new SpriteInfo")) {
			textureWrapper = new(reference: texture, dimensions: source, expectedScale: expectedScale, textureType: textureType);
		}

		string getRemainingTime() {
			if (!remainingTime.HasValue) {
				return "";
			}
			return $" (remaining time: {remainingTime?.TotalMilliseconds.ToString(DrawingColor.LightYellow)} ms)";
		}

		// If this is null, it can only happen due to something being blocked, so we should try again later.
		if (textureWrapper.ReferenceData is null) {
			Debug.TraceLn($"Texture Data fetch for {getNameString()} was {"blocked".Pastel(DrawingColor.Red)}; retrying later#{getRemainingTime()}");
			return null;
		}

		Debug.TraceLn($"Beginning Rescale Process for {getNameString()} #{getRemainingTime()}");

		DrawState.PushedUpdateThisFrame = true;

		try {
			var resampleTask = ResampleTask.Dispatch(
				spriteInfo: textureWrapper,
				async: useAsync
			);

			var result = resampleTask.IsCompletedSuccessfully ? resampleTask.Result : null;

			if (useAsync) {
				// It adds itself to the relevant maps.
				return (result?.IsReady ?? false) ? result : null;
			}
			else {
				return result;
			}
		}
		finally {
			watch.Stop();
			var duration = watch.Elapsed;
			var averager = GetTimer(cached: textureWrapper.WasCached, async: useAsync);
			TimeSpanSamples++;
			MeanTimeSpan += duration;
			Debug.TraceLn($"Duration {getNameString()}: {(MeanTimeSpan / TimeSpanSamples).TotalMilliseconds.ToString(DrawingColor.LightYellow)} ms");
			averager.Add(source.Area, duration);
		}
	}

	internal static readonly SurfaceFormat[] AllowedFormats = {
		SurfaceFormat.Color,
		SurfaceFormat.Dxt5,
		SurfaceFormat.Dxt5SRgb
		//SurfaceFormat.Dxt3 // fonts
	};

	internal ManagedTexture2D Texture = null;
	internal readonly string Name;
	internal Vector2 Scale;
	internal readonly TextureType TexType;
	private volatile bool _isReady = false;
	internal bool IsReady => _isReady && Texture is not null;

	internal Vector2B Wrapped = Vector2B.False;

	internal readonly WeakTexture Reference;
	internal readonly Bounds OriginalSourceRectangle;
	internal ulong Hash { get; private set; }

	internal Vector2I Padding = Vector2I.Zero;
	internal Vector2I UnpaddedSize;
	internal Vector2I BlockPadding = Vector2I.Zero;
	private readonly Vector2I originalSize;
	private readonly Bounds sourceRectangle;
	private readonly uint ExpectedScale;
	internal readonly ulong SpriteMapHash;
	private uint refScale;

	internal ulong LastReferencedFrame = DrawState.CurrentFrame;

	internal Vector2 AdjustedScale = Vector2.One;

	private LinkedListNode<WeakReference<ScaledTexture>> CurrentRecentNode = null;
	internal volatile bool IsDisposed = false;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	~ScaledTexture() {
		if (!IsDisposed) {
			Dispose();
		}
	}

	internal static volatile uint TotalMemoryUsage = 0;

	internal long MemorySize {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			if (!IsReady) {
				return 0;
			}
			return Texture.SizeBytes();
		}
	}

	internal long OriginalMemorySize {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			return originalSize.Width * originalSize.Height * sizeof(int);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Purge(Texture2D reference) {
		Purge(reference, null, DataRef<byte>.Null);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Purge(Texture2D reference, in Bounds? bounds, in DataRef<byte> data) {
		SpriteInfo.Purge(reference, bounds, data);
		SpriteMap.Purge(reference, bounds);
		Resampler.PurgeHash(reference);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void PurgeTextures(long _purgeTotalBytes) {
		Contracts.AssertPositive(_purgeTotalBytes);

		Debug.TraceLn($"Attempting to purge {_purgeTotalBytes.AsDataSize()} from currently loaded textures");

		// For portability purposes
		if (IntPtr.Size == 8) {
			var purgeTotalBytes = _purgeTotalBytes;
			lock (MostRecentList) {
				long totalPurge = 0;
				while (purgeTotalBytes > 0 && MostRecentList.Count > 0) {
					if (MostRecentList.Last.Value.TryGetTarget(out var target)) {
						var textureSize = (long)target.MemorySize;
						Debug.TraceLn($"Purging {target.SafeName()} ({textureSize.AsDataSize()})");
						purgeTotalBytes -= textureSize;
						totalPurge += textureSize;
						target.CurrentRecentNode = null;
						target.Dispose(true);
					}
					MostRecentList.RemoveLast();
				}
				Debug.TraceLn($"Total Purged: {totalPurge.AsDataSize()}");
			}
		}
		else {
			// For 32-bit, truncate down to an integer so this operation goes a bit faster.
			Contracts.AssertLessEqual(_purgeTotalBytes, (long)uint.MaxValue);
			var purgeTotalBytes = (uint)_purgeTotalBytes;
			lock (MostRecentList) {
				uint totalPurge = 0;
				while (purgeTotalBytes > 0 && MostRecentList.Count > 0) {
					if (MostRecentList.Last.Value.TryGetTarget(out var target)) {
						var textureSize = (uint)target.MemorySize;
						Debug.TraceLn($"Purging {target.SafeName()} ({textureSize.AsDataSize()})");
						purgeTotalBytes -= textureSize;
						totalPurge += textureSize;
						target.CurrentRecentNode = null;
						target.Dispose(true);
					}
					MostRecentList.RemoveLast();
				}
				Debug.TraceLn($"Total Purged: {totalPurge.AsDataSize()}");
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ScaledTexture(string assetName, SpriteInfo textureWrapper, Bounds sourceRectangle, TextureType textureType, bool async, uint expectedScale) {
		using var _ = Performance.Track();

		TexType = textureType;

		ulong GetHash() {
			using (Performance.Track("Upscaler.GetHash")) {
				return Resampler.GetHash(textureWrapper, textureType);
			}
		}

		var source = textureWrapper.Reference;

		this.OriginalSourceRectangle = new(sourceRectangle);
		this.Reference = source.MakeWeak();
		this.sourceRectangle = sourceRectangle;
		this.ExpectedScale = expectedScale;
		this.refScale = expectedScale;
		this.SpriteMapHash = SpriteMap.SpriteHash(source, sourceRectangle, expectedScale);
		// TODO : I believe we need a lock here until when the texture is _fully created_, preventing new instantiations from starting of a texture
		// already in-flight
		if (!SpriteMap.Add(source, this)) {
			// If false, then the sprite already exists in the map (which can be caused by gap between the Resample task being kicked off, and hitting this, and _another_ sprite getting
			// past the earlier try-block, and getting here.
			// TODO : this should be fixed by making sure that all of the resample tasks _at least_ get to this point before the end of the frame.
			// TODO : That might not be sufficient either if the _same_ draw ends up happening again.
			return;
		}

		this.Name = source.Anonymous() ? assetName.SafeName() : source.SafeName();
		switch (TexType) {
			case TextureType.Sprite:
				originalSize = sourceRectangle.Extent;
				break;
			case TextureType.Image:
				originalSize = source.Extent();
				break;
			case TextureType.SlicedImage:
				throw new NotImplementedException("Sliced Images not yet implemented");
		}

		// TODO store the HD Texture in _this_ object instead. Will confuse things like subtexture updates, though.
		Hash = GetHash();
		this.Texture = Resampler.Upscale(
			texture: this,
			scale: ref refScale,
			input: textureWrapper,
			hash: Hash,
			wrapped: ref Wrapped,
			async: false
		);

		// TODO : I would love to dispose of this texture _now_, but we rely on it disposing to know if we need to dispose of ours.
		// There is a better way to do this using weak references, I just need to analyze it further. Memory leaks have been a pain so far.
		source.Disposing += (object sender, EventArgs args) => { OnParentDispose((Texture2D)sender); };

		lock (MostRecentList) {
			CurrentRecentNode = MostRecentList.AddFirst(this.MakeWeak());
		}
	}

	// Async Call
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Finish() {
		ManagedTexture2D texture;
		lock (this) {
			texture = Texture;
		}

		if (texture is null || texture.IsDisposed || IsDisposed) {
			return;
		}

		UpdateReferenceFrame();

		TotalMemoryUsage += (uint)texture.SizeBytes();
		texture.Disposing += (object sender, EventArgs args) => { TotalMemoryUsage -= (uint)texture.SizeBytes(); };

		switch (TexType) {
			case TextureType.Sprite:
				Debug.TraceLn($"Creating Sprite [{texture.Format.ToString(DrawingColor.LightCoral)} x{refScale}]: {this.SafeName(DrawingColor.LightYellow)} {sourceRectangle}");
				break;
			case TextureType.Image:
				Debug.TraceLn($"Creating Image [{texture.Format.ToString(DrawingColor.LightCoral)} x{refScale}]: {this.SafeName(DrawingColor.LightYellow)}");
				break;
			case TextureType.SlicedImage:
				Debug.TraceLn($"Creating Sliced Image [{texture.Format.ToString(DrawingColor.LightCoral)} x{refScale}]: {this.SafeName(DrawingColor.LightYellow)}");
				break;
			default:
				Debug.TraceLn($"Creating UNKNOWN [{texture.Format.ToString(DrawingColor.LightCoral)} x{refScale}]: {this.SafeName(DrawingColor.LightYellow)}");
				break;
		}

		this.Scale = (Vector2)texture.Dimensions / new Vector2(originalSize.Width, originalSize.Height);

		Thread.MemoryBarrier();
		_isReady = true;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void UpdateReferenceFrame() {
		if (IsDisposed) {
			return;
		}

		this.LastReferencedFrame = DrawState.CurrentFrame;

		lock (MostRecentList) {
			if (CurrentRecentNode is not null) {
				MostRecentList.Remove(CurrentRecentNode);
				MostRecentList.AddFirst(CurrentRecentNode);
			}
			else {
				CurrentRecentNode = MostRecentList.AddFirst(this.MakeWeak());
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Dispose(bool disposeChildren) {
		if (disposeChildren && Texture is not null) {
			if (!Texture.IsDisposed) {
				Texture.Dispose();
			}
			Texture = null;
		}
		Dispose();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public void Dispose() {
		if (IsDisposed) {
			return;
		}

		if (Reference.TryGetTarget(out var reference)) {
			SpriteMap.Remove(this, reference);
		}
		if (CurrentRecentNode is not null) {
			lock (MostRecentList) {
				MostRecentList.Remove(CurrentRecentNode);
			}
			CurrentRecentNode = null;
		}
		IsDisposed = true;

		GC.SuppressFinalize(this);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void OnParentDispose(Texture2D texture) {
		Debug.TraceLn($"Parent Texture Disposing: {texture.SafeName()}");

		Dispose();
	}
}
