using SpriteMaster.Resample;
using SpriteMaster.Types;

namespace SpriteMaster;

internal sealed class BasicSpriteInfo : SpriteInfoBase, IDisposable {
	internal byte[]? ReferenceData { get; private set; }

	internal Bounds ReferenceSize => Bounds;

	internal BasicSpriteInfo(
		byte[] referenceData,
		Bounds bounds,
		TextureType textureType,
		in (int Offset, int Stride) rawOffsetStride,
		Scaler scaler,
		SpriteFlags flags,
		Vector2B wrapped
	) : base(bounds, textureType, in rawOffsetStride, scaler, flags, wrapped) {
		ReferenceData = referenceData;
	}

	public override void Dispose() {
		base.Dispose();
		ReferenceData = null;
	}
}
