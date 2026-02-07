using StardewModdingAPI;
using System;

namespace SpriteMaster.Configuration;

internal static partial class Config {
	[Attributes.Comment("Button to toggle SpriteMaster")]
	internal static SButton ToggleButton = SButton.F11;

	[Attributes.Ignore]
	internal static readonly string LocalRootDefault = System.IO.Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"StardewValley",
		"Mods",
		ModuleName
	);

	internal static partial class Debug {
		internal static partial class Logging {
			internal static LogLevel LogLevel = LogLevel.Trace;
#if (!SHIPPING && !RELEASE) || LOG_MONITOR
			internal static string[] SilencedMods = new[] {
				"Farm Type Manager",
				"Quest Framework",
				"AntiSocial NPCs",
				"SMAPI",
				"Json Assets",
				"Content Patcher",
				"Free Love",
				"Mail Framework Mod",
				"Shop Tile Framework",
				"Custom Companions",
				"Farmer Helper",
				"Wind Effects",
				"Multiple Spouse Dialogs"
			};
#endif
		}
	}
}
