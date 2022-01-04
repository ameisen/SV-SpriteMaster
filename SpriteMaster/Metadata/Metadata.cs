﻿using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Metadata;

static class Metadata {
	private static readonly ConditionalWeakTable<Texture2D, MTexture2D> Texture2DMetaTable = new();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static MTexture2D Meta(this Texture2D @this) {
		return Texture2DMetaTable.GetValue(@this, key => new(key));
	}
}

