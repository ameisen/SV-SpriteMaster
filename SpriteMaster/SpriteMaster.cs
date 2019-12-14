/// **********
/// HDSpritesMod is a mod for Stardew Valley using SMAPI and Harmony.
/// It loads all *.png/*.xnb from the mod's local assets folder into 
/// ScaledTexture2D objects and replaces their game loaded counterparts.
/// 
/// Harmony is used to patch the XNA drawMethod (which the game uses to render its
/// textures) to check if the texture being drawn is of the replaced type ScaledTexture2D, 
/// and if it is, then draw the larger version using its scale adjusted parameters.
/// 
/// Credit goes to Platonymous for the ScaledTexture2D and SpriteBatchFix Harmony 
/// patch classes from his Portraiture mod that makes this whole mod possible.
/// 
/// Author: NinthWorld
/// Date: 5/31/19
/// **********

using Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Reflection;

namespace SpriteMaster
{
	/// <summary>The mod entry class.</summary>
	public class SpriteMaster : Mod
	{
		/// <summary>The mod entry point, called after the mod is first loaded.</summary>
		/// <param name="help">Provides simplified APIs for writing mods.</param>
		public override void Entry(IModHelper help)
		{
			HarmonyInstance instance = HarmonyInstance.Create($"DigitalCarbide.${Config.ModuleName}");
			Patches.InitializePatch(instance);
			instance.PatchAll(Assembly.GetExecutingAssembly());

			help.Events.Input.ButtonPressed += (object sender, ButtonPressedEventArgs args) =>
			{
				switch (args.Button)
				{
					case Config.Debug.TextureDump.Button:
						if (Config.Debug.TextureDump.Enabled)
						{
							Debug.DumpMemory();
						}
						break;
					case Config.ToggleButton:
						Config.Enabled = !Config.Enabled;
						break;
				}
			};
		}
	}
}
