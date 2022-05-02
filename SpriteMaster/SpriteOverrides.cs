using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster;

static class SpriteOverrides {
	internal const int WaterBlock = 4; // Water is scaled up 4x for some reason

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsWater(in Bounds bounds, XTexture2D texture) {
		if (bounds.Right <= 640 && bounds.Top >= 2000 && bounds.Extent.MinOf >= WaterBlock && texture.NormalizedName() == @"LooseSprites\Cursors") {
			return true;
		}
		return false;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsFont(XTexture2D texture, in Vector2I spriteSize, in Vector2I sheetSize) {
		switch (texture.Format) {
			case SurfaceFormat.Dxt1:
			case SurfaceFormat.Dxt1SRgb:
			case SurfaceFormat.Dxt1a:
			case SurfaceFormat.Dxt3:
			case SurfaceFormat.Dxt3SRgb:
			case SurfaceFormat.Dxt5:
			case SurfaceFormat.Dxt5SRgb:
				return Math.Min(spriteSize.MinOf, sheetSize.MinOf) >= 1;
			default:
				return false;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsFont(SpriteInfo info) => IsFont(info.Reference, info.Bounds.Extent, info.ReferenceSize);
}
