using SpriteMaster.Resample;
using SpriteMaster.Types;
using System;

namespace SpriteMaster;

internal class SpriteInfoBase : IDisposable {
	[Flags]
	internal enum SpriteFlags {
		None = 0,
		IsWater = 1 << 0,
		IsFont = 1 << 1,
		BlendEnabled = 1 << 2,
		HashMask = IsWater | IsFont | BlendEnabled,
		WasCached = 1 << 3,
		Preview = 1 << 4,
		Animated = 1 << 5,
		ForcePadding = 1 << 6,
	}

	internal readonly Bounds Bounds;
	internal readonly Vector2B Wrapped;
	internal readonly TextureType TextureType;
	protected internal readonly int RawOffset;
	protected internal readonly int RawStride;
	internal readonly Scaler Scaler;
	internal readonly SpriteFlags Flags;
	protected internal volatile bool Broken = false;

	internal bool BlendEnabled => Flags.HasFlag(SpriteFlags.BlendEnabled);
	internal bool IsWater => Flags.HasFlag(SpriteFlags.IsWater); // TODO : stardew-only
	internal bool IsFont => Flags.HasFlag(SpriteFlags.IsFont);
	internal bool IsAnimated => Flags.HasFlag(SpriteFlags.Animated);
	internal bool IsPreview => Flags.HasFlag(SpriteFlags.Preview);
	internal bool ForcePadding => Flags.HasFlag(SpriteFlags.ForcePadding);

	internal virtual bool Anonymous => true;

	internal SpriteInfoBase(
		Bounds bounds,
		TextureType textureType,
		in (int Offset, int Stride) rawOffsetStride,
		Scaler scaler,
		SpriteFlags flags,
		Vector2B wrapped
	) {
		Bounds = bounds;
		TextureType = textureType;
		RawOffset = rawOffsetStride.Offset;
		RawStride = rawOffsetStride.Stride;

		Scaler = scaler;

		Flags = flags;
		Wrapped = wrapped;
	}

	public virtual void Dispose() {
	}
}
