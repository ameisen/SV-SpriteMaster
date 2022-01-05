using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using SpriteMaster.Types.Interlocked;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SpriteMaster;

using SpriteMemorySequence = ReadOnlySequence<byte>;

sealed class SpriteMemorySegment : ReadOnlySequenceSegment<byte> {
	internal SpriteMemorySegment(in ReadOnlyMemory<byte> memory) {
		Memory = memory;
	}

	internal static SpriteMemorySequence Create(in ReadOnlyMemory<byte> memory, int offset, in Vector2I extent, int stride, SurfaceFormat format) {
		if (extent.Width == 0 || extent.Height == 0) {
			throw new ArgumentOutOfRangeException($"SpriteMemorySegment.Create: '{nameof(extent)}' is degenerate");
		}
		if (stride <= 0) {
			throw new ArgumentOutOfRangeException($"SpriteMemorySegment.Create: '{nameof(stride)}' is invalid");
		}

		int spriteStride = checked((int)format.SizeBytes(extent.Width));
		SpriteMemorySegment firstSegment = null;
		SpriteMemorySegment prevSegment = null;
		for (int y = 0; y < extent.Height; ++y) {
			var segment = new SpriteMemorySegment(memory.Slice(offset, spriteStride));
			if (firstSegment is null) {
				firstSegment = segment;
			}
			if (prevSegment is not null) {
				prevSegment.Next = segment;
			}

			prevSegment = segment;
			offset += stride;
		}

		return new SpriteMemorySequence(firstSegment, 0, prevSegment, prevSegment.Memory.Length);
	}
}

/// <summary>
/// A wrapper during the resampling process that encapsulates the properties of the sprite itself
/// <para>Warning: <seealso cref="SpriteInfo">SpriteInfo</seealso> holds a reference to the reference texture's data in its <seealso cref="SpriteInfo._ReferenceData">ReferenceData field</seealso>.</para>
/// </summary>
sealed class SpriteInfo : IDisposable {
	internal readonly Texture2D Reference;
	internal readonly Vector2I ReferenceSize;
	internal readonly Bounds Size;
	internal readonly Vector2B Wrapped;
	internal readonly bool BlendEnabled;
	internal readonly uint ExpectedScale;
	internal readonly bool IsWater;
	internal readonly bool IsFont;
	// For statistics and throttling
	internal readonly bool WasCached;

	private readonly int RawOffset;
	private readonly int RawStride;

	public override string ToString() => $"SpriteInfo[Name: '{Reference.Name}', ReferenceSize: {ReferenceSize}, Size: {Size}]";

	internal SpriteMemorySequence? SpriteData = null;
	private byte[] _ReferenceData = null;
	internal byte[] ReferenceData {
		get => _ReferenceData;
		set {
			if (_ReferenceData == value) {
				return;
			}
			_ReferenceData = value;
			if (_ReferenceData == null) {
				SpriteData = null;
			}
			else {
				SpriteData = SpriteMemorySegment.Create(
					memory: new ReadOnlyMemory<byte>(_ReferenceData),
					offset: RawOffset,
					extent: Size.Extent,
					stride: RawStride,
					format: Reference.Format
				);
			}
		}
	}

	private InterlockedULong _Hash = Hashing.Default;
	internal ulong Hash {
		[MethodImpl(Runtime.MethodImpl.Hot)]
		get {
			ulong hash = _Hash;
			if (hash == Hashing.Default) {
				hash = Hashing.Combine(
					SpriteData?.Hash(),
					Size.Extent.GetLongHashCode(),
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
	internal SpriteInfo(Texture2D reference, in Bounds dimensions, uint expectedScale) {
		ReferenceSize = new(reference);
		ExpectedScale = expectedScale;
		Size = dimensions;
		if (Size.Bottom > ReferenceSize.Height) {
			Size.Height -= (Size.Bottom - ReferenceSize.Height);
		}
		if (Size.Right > ReferenceSize.Width) {
			Size.Width -= (Size.Right - ReferenceSize.Width);
		}
		Reference = reference;

		RawStride = checked((int)reference.Format.SizeBytes(ReferenceSize.Width));
		RawOffset = (RawStride * dimensions.Top) + checked((int)reference.Format.SizeBytes(dimensions.Left));

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
		else if (ReferenceData == MTexture2D.BlockedSentinel) {
			refData = null;
			WasCached = false;
		}
		else {
			WasCached = true;
		}

		ReferenceData = refData;

		BlendEnabled = DrawState.CurrentBlendSourceMode != Blend.One;
		Wrapped = new(
			DrawState.CurrentAddressModeU == TextureAddressMode.Wrap,
			DrawState.CurrentAddressModeV == TextureAddressMode.Wrap
		);

		IsWater = SpriteOverrides.IsWater(Size, Reference);
		IsFont = SpriteOverrides.IsFont(Reference, Size.Extent, ReferenceSize);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public void Dispose() {
		ReferenceData = null;
		_Hash = Hashing.Default;
	}
}
