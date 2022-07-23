using SpriteMaster.Types.Exceptions;
using StardewValley;
using SMHarmonize = SpriteMaster.Harmonize;

namespace SpriteMaster.Configuration.Preview;

internal static class PrecipitationPatches {
	private static PrecipitationType Precipitation => PrecipitationOverride ?? Scene.Current?.Precipitation ?? PrecipitationType.None;
	internal static PrecipitationType? PrecipitationOverride = null;

	[SMHarmonize.Harmonize(
		typeof(Game1),
		"IsSnowingHere",
		SMHarmonize.Harmonize.Fixation.ReversePatched,
		instance: false,
		critical: false
	)]
	public static bool IsSnowingHereReverse(GameLocation? location) {
		try {
			throw new ReversePatchException();
		}
		catch {
			throw new ReversePatchException();
		}
	}

	public static bool IsSnowingHereExt(GameLocation? location = null) {
		if (Precipitation != PrecipitationType.Snow) {
			if (Scene.Current is null) {
				return IsSnowingHereReverse(location);
			}

			return false;
		}

		if (ReferenceEquals(location, Scene.SceneLocation.Value) || location is null) {
			return true;
		}

		return IsSnowingHereReverse(location);
	}

	[SMHarmonize.Harmonize(
		typeof(Game1),
		"IsSnowingHere",
		SMHarmonize.Harmonize.Fixation.Prefix,
		SMHarmonize.Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool IsSnowingHere(ref bool __result, GameLocation? location) {
		return false;
	}

	[SMHarmonize.Harmonize(
		typeof(Game1),
		"IsRainingHere",
		SMHarmonize.Harmonize.Fixation.Prefix,
		SMHarmonize.Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool IsRainingHere(ref bool __result, GameLocation? location) {
		if (Precipitation != PrecipitationType.Rain) {
			if (Scene.Current is null) {
				return true;
			}
			else {
				__result = false;
				return false;
			}
		}

		if (ReferenceEquals(location, Scene.SceneLocation.Value) || location is null) {
			__result = true;
			return false;
		}

		return true;
	}
}
