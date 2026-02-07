namespace SpriteMaster.Configuration;

internal static partial class Command {
	private static void OnResetDisplay() {
		StardewValley.Game1.graphics.ApplyChanges();
	}
}
