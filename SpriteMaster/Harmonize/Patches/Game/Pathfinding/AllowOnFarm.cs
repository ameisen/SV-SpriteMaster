using StardewValley;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.Game.Pathfinding;

static partial class Pathfinding {
	// Override 'warpCharacter' so that family entities (pets, spouses, children) actually path to destinations rather than warping.
	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, GameLocation targetLocation, XNA.Vector2 position) {
		if (!Config.Enabled || !Config.Extras.AllowNPCsOnFarm || !Config.Extras.OptimizeWarpPoints) {
			return true;
		}

		if (character is null || Game1.player is not Farmer player) {
			return true;
		}

		bool isFamily = false;
		// If the player has friendship data, check if the given character is married or a roommate.
		if (player.friendshipData.TryGetValue(character.Name, out var value)) {
			isFamily = value.IsMarried() || value.IsRoommate();
		}
		// Check if the given character is a child.
		if (!isFamily && (player.getChildren()?.Contains(character) ?? false)) {
			isFamily = true;
		}

		// https://github.com/aedenthorn/StardewValleyMods/blob/master/FreeLove/FarmerPatches.cs
		// If the character is family, a pet, or a spouse, run the logic.
		if (isFamily || character == player.getSpouse() || character == player.getPet()) {
			// Do _not_ execute this logic for Events.
			var trace = new StackTrace();
			foreach (var frame in trace.GetFrames()) {
				if (frame.GetMethod() is MethodBase method) {
					if (method.DeclaringType == typeof(Event)) {
						return true;
					}
				}
			}

			// Try to path. If we fail, revert to default logic.
			character.controller = new PathFindController(c: character, location: targetLocation, endPoint: Utility.Vector2ToPoint(position), finalFacingDirection: -1); // TODO: we often do know the expected final facing direction.
			if (character.controller.pathToEndPoint is null) {
				character.controller = null;
				return true;
			}
			return false;
		}

		return true;
	}

	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, string targetLocationName, XNA.Vector2 position) {
		return WarpCharacter(character, Game1.getLocationFromName(targetLocationName), position);
	}

	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, string targetLocationName, XNA.Point position) {
		return WarpCharacter(character, Game1.getLocationFromName(targetLocationName), Utility.PointToVector2(position));
	}

	// Prevent NPCs from destroying items on the farm.
	[Harmonize(
		typeof(GameLocation),
		"characterDestroyObjectWithinRectangle",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool CharacterDestroyObjectWithinRectangle(GameLocation __instance, ref bool __result, XNA.Rectangle rect, bool showDestroyedObject) {
		if (__instance.IsFarm || __instance.IsGreenhouse) {
			__result = false;
			return false;
		}

		return true;
	}
}
