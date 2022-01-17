using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using SpriteMaster.Types.Interlocking;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster;

/// <summary>
/// A wrapper during the resampling process that encapsulates the properties of the sprite itself
/// <para>Warning: <seealso cref="SpriteInfo">SpriteInfo</seealso> holds a reference to the reference texture's data in its <seealso cref="SpriteInfo._ReferenceData">ReferenceData field</seealso>.</para>
/// </summary>
sealed class SpriteInfo : IDisposable {
	internal readonly Texture2D Reference;
	internal readonly Bounds Bounds;
	internal Vector2I ReferenceSize => Reference.Extent();
	internal readonly Vector2B Wrapped;
	internal readonly TextureType TextureType;
	internal readonly uint ExpectedScale;
	private readonly int RawOffset;
	private readonly int RawStride;
	internal readonly XNA.Graphics.BlendState BlendState;
	internal readonly bool BlendEnabled;
	internal readonly bool IsWater;
	internal readonly bool IsFont;
	// For statistics and throttling
	internal readonly bool WasCached;

	public override string ToString() => $"SpriteInfo[Name: '{Reference.Name}', ReferenceSize: {ReferenceSize}, Size: {Bounds}]";

	internal ulong SpriteDataHash = 0;
	private byte[]? _ReferenceData = null;
	internal byte[]? ReferenceData {
		get => _ReferenceData;
		set {
			if (_ReferenceData == value) {
				return;
			}
			_ReferenceData = value;
			if (_ReferenceData == null) {
				SpriteDataHash = 0;
			}
			else {
				int formatSize = Reference.Format.IsCompressed() ? 4 : (int)Reference.Format.SizeBytes(1);

				int actualWidth = Bounds.Extent.X * formatSize;

				var spriteData = new Span2D<byte>(
					array: _ReferenceData,
					offset: RawOffset,
					width: Bounds.Extent.X * formatSize,
					height: Bounds.Extent.Y,
					// 'pitch' is the distance between the end of one row and the start of another
					// whereas 'stride' is the distance between the starts of rows
					// Ergo, 'pitch' is 'stride' - 'width'.
					pitch: RawStride - actualWidth
				);

				SpriteDataHash = spriteData.Hash();
			}
		}
	}

	private InterlockedULong _Hash = 0;
	internal ulong Hash {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			if (_ReferenceData == null) {
				throw new NullReferenceException(nameof(_ReferenceData));
			}

			ulong hash = _Hash;
			if (hash == 0) {
				hash = Hashing.Combine(
					SpriteDataHash,
					Bounds.Extent.GetLongHashCode(),
					BlendEnabled.GetLongHashCode(),
					//ExpectedScale.GetLongHashCode(),
					IsWater.GetLongHashCode(),
					IsFont.GetLongHashCode(),
					Reference.Format.GetLongHashCode()
				);

			}
			_Hash = hash;
			return hash;// ^ (ulong)ExpectedScale.GetHashCode();
		}
	}

	// Attempt to update the bytedata cache for the reference texture, or purge if it that makes more sense or if updating
	// is not plausible.
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Purge(Texture2D reference, in Bounds? bounds, in DataRef<byte> data) => reference.Meta().Purge(reference, bounds, data);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsCached(Texture2D reference) => reference.Meta().CachedDataNonBlocking is not null;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal SpriteInfo(Texture2D reference, in Bounds dimensions, uint expectedScale, TextureType textureType) {
		Reference = reference;
		BlendState = DrawState.CurrentBlendState;
		ExpectedScale = expectedScale;
		Bounds = dimensions;
		TextureType = textureType;
		if (Bounds.Bottom > ReferenceSize.Height) {
			Bounds.Height -= (Bounds.Bottom - ReferenceSize.Height);
		}
		if (Bounds.Right > ReferenceSize.Width) {
			Bounds.Width -= (Bounds.Right - ReferenceSize.Width);
		}

		int formatSize = reference.Format.IsCompressed() ? 4 : (int)reference.Format.SizeBytes(1);

		RawStride = formatSize * ReferenceSize.Width;
		RawOffset = (RawStride * dimensions.Top) + (formatSize * dimensions.Left);

		var refMeta = reference.Meta();
		var refData = refMeta.CachedData;

		if (refData is null) {
			// TODO : Switch this around to use ReadOnlySequence so our hash is specific to the sprite
			refData = new byte[reference.SizeBytes()];
			Debug.TraceLn($"Reloading Texture Data (not in cache): {reference.SafeName(DrawingColor.LightYellow)}");
			reference.GetData(refData);
			reference.Meta().CachedRawData = refData;
			if (refMeta.IsCompressed) {
				refData = null; // we can only use uncompressed data at this stage.
			}
			WasCached = false;
		}
		else if (ReferenceData == Texture2DMeta.BlockedSentinel) {
			refData = null;
			WasCached = false;
		}
		else {
			WasCached = true;
		}

		ReferenceData = refData;

		BlendEnabled = DrawState.CurrentBlendState.AlphaSourceBlend != Blend.One;
		Wrapped = new(
			DrawState.CurrentSamplerState.AddressU == TextureAddressMode.Wrap,
			DrawState.CurrentSamplerState.AddressV == TextureAddressMode.Wrap
		);

		IsWater = TextureType == TextureType.Sprite && SpriteOverrides.IsWater(Bounds, Reference);
		IsFont = !IsWater && TextureType == TextureType.Sprite && SpriteOverrides.IsFont(Reference, Bounds.Extent, ReferenceSize);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public void Dispose() {
		ReferenceData = null;
		_Hash = 0;
		GC.SuppressFinalize(this);
	}
}
