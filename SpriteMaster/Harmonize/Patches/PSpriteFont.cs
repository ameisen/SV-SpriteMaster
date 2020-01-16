using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	internal static class PSpriteFont {
		/*
		[Harmonize(".ctor", HarmonizeAttribute.Fixation.Postfix, PriorityLevel.Last)]
		private static void SpriteFontCtor(
			SpriteFont __instance,
			Texture2D texture,
			List<Rectangle> glyphs,
			List<Rectangle> cropping,
			List<char> charMap,
			int lineSpacing,
			float spacing,
			List<Vector3> kerning,
			char? defaultCharacter
		) {
			texture = texture;
		}
		*/
	}
}
