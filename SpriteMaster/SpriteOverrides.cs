using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster;

static class SpriteOverrides {
	internal const int WaterBlock = 4; // Water is scaled up 4x for some reason

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsWater(in Bounds bounds, Texture2D texture) {
		if (bounds.Right <= 640 && bounds.Top >= 2000 && bounds.Extent.MinOf >= WaterBlock && texture.SafeName() == "LooseSprites/Cursors") {
			return true;
		}
		return false;
	}
}
