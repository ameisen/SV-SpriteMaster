using Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Reflection;

namespace SpriteMaster
{
	/// <summary>The mod entry class.</summary>
	public sealed class SpriteMaster : Mod
	{
		public static SpriteMaster Self { get => _Self; }
		private static SpriteMaster _Self = null;

		public SpriteMaster() {
			_Self = this;
		}

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
					case Config.Debug.CacheDump.Button:
						if (Config.Debug.CacheDump.Enabled)
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
