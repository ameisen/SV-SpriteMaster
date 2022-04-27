using StardewValley;

namespace SpriteMaster;

static class GameState {
	internal static bool IsLoading => Game1.currentLoader is not null || Game1.gameMode == Game1.loadingMode;
	internal static volatile string CurrentSeason = "";
}
