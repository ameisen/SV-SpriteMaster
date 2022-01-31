using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;

using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

static class GraphicsResource {
	/*
	[Harmonize("~GraphicsResource", Harmonize.Fixation.Prefix, PriorityLevel.First)]
	private static void Finalize(XNA.Graphics.GraphicsResource __instance) {
		switch (__instance) {
			case InternalTexture2D _:
				return;
			case Texture2D texture:
				if (!texture.IsDisposed) {
					//Debug.Trace($"Another module has leaked the following texture: {texture.GetType().FullName} '{texture.Name}' {texture} {(Bounds)texture.Bounds}");
				}
				break;
		}
	}
	*/
}
