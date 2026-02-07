namespace SpriteMaster;

internal static class GameSupport {
	internal static XGraphics.GraphicsDevice GraphicsDevice =>
		StardewValley.Game1.graphics.GraphicsDevice;

	internal static XNA.Game GameRunnerInstance =>
		StardewValley.GameRunner.instance;
}
