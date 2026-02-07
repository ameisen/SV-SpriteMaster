using StardewValley;

namespace SpriteMaster;

internal static partial class GameState {
	internal static bool IsLoading => Game1.currentLoader is not null || Game1.gameMode == Game1.loadingMode;
	internal static volatile string CurrentSeason = "";
}
