using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;

namespace SpriteMaster.Harmonize.Patches;

static class Snow {
	/*
	[Harmonize(
		typeof(Game1),
		"drawWeather",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool DrawWeather(Game1 __instance, GameTime time, RenderTarget2D target_screen) {
		// Is it snow?
		bool drawSnow = Game1.IsSnowingHere() && Game1.currentLocation.isOutdoors && Game1.currentLocation is not Desert;
		if (drawSnow) {
			return false;
		}
		return true;
	}
	*/
}
