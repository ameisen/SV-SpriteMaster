using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SpriteMaster;

internal static partial class DrawState {
	private static GraphicsDevice GetCurrentGraphicsDeviceGame() {
		return Game1.graphics.GraphicsDevice;
	}

	private static void OnUpdateDevice(GraphicsDevice device) {
		Harmonize.Patches.Game.HoeDirt.OnNewGraphicsDevice(device);
	}

	private static bool ForceSynchronousOnTarget(Texture renderTarget) {
		return
			renderTarget == Game1.game1.uiScreen ||
			renderTarget == Game1.game1.screen;
	}
}
