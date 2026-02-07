using StardewValley;

namespace SpriteMaster;

internal sealed partial class ManagedSpriteInstance {
	private static bool? CheckIsDialogFont(params XTexture2D[] textures) {
		if (Game1.dialogueFont?.Texture is not {} dialogTexture) {
			return null;
		}

		foreach (var texture in textures) {
			if (ReferenceEquals(texture, dialogTexture)) {
				return true;
			}
		}

		return false;
	}
}
