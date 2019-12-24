using Harmony;
using SpriteMaster.Extensions;
using SpriteMaster.HarmonyExt;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using System;
using System.IO;
using System.Runtime;
using System.Threading;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster {
	public sealed class SpriteMaster : Mod {
		public static SpriteMaster Self { get => _Self; }
		private static SpriteMaster _Self = null;

		// TODO : long for 64-bit?
		private const uint RequiredMemoryBlock = 256U;
		private const uint MemoryPressureLimit = uint.MaxValue - (RequiredMemoryBlock * 1024 * 1024);
		private static readonly string CollectMessage = $"Less than {(RequiredMemoryBlock * 1024 * 1024).AsDataSize(decimals: 0)} available for block allocation, forcing full garbage collection";
		private readonly Thread MemoryPressureThread;

		private void MemoryPressureLoop() {
			while (true) {
				try {
					using var _ = new MemoryFailPoint(unchecked((int)RequiredMemoryBlock));
					Thread.Sleep(128);
				}
				catch (InsufficientMemoryException) {
					Debug.WarningLn(CollectMessage);
					Garbage.Collect(compact: true, blocking: true, background: false);
					Thread.Sleep(1024);
				}
			}
		}
		
		private static readonly string ConfigName = "config.toml";

		private static string CurrentSeason = "";

		public SpriteMaster () {
			_Self = this;

			MemoryPressureThread = new Thread(MemoryPressureLoop);
			MemoryPressureThread.IsBackground = true;
			MemoryPressureThread.Start();
		}

		public override void Entry (IModHelper help) {
			var ConfigPath = Path.Combine(help.DirectoryPath, ConfigName);
			SerializeConfig.Load(ConfigPath);
			SerializeConfig.Save(ConfigPath);
			
			ConfigureHarmony();
			help.Events.Input.ButtonPressed += OnButtonPressed;

			help.ConsoleCommands.Add("spritemaster_stats", "Dump SpriteMaster Statistics", (string a, string[] b) => { ManagedTexture2D.DumpStats(); });
			help.ConsoleCommands.Add("spritemaster_memory", "Dump SpriteMaster Memory", (string a, string[] b) => { Debug.DumpMemory(); });

			help.Events.GameLoop.DayStarted += OnDayStarted;
		}

		// SMAPI/CP won't do this, so we do. Purge the cached textures for the previous season on a season change.
		private static void OnDayStarted(object _, DayStartedEventArgs _1) {
			var season = SDate.Now().Season.ToLower();
			if (season != CurrentSeason) {
				CurrentSeason = season;
				ScaledTexture.SpriteMap.SeasonPurge(season);
			}
		}

		private void ConfigureHarmony() {
			var instance = HarmonyInstance.Create($"DigitalCarbide.${Config.ModuleName}");
			instance.ApplyPatches();
		}

		private static void OnButtonPressed (object sender, ButtonPressedEventArgs args) {
			if (args.Button == Config.ToggleButton)
				Config.Enabled = !Config.Enabled;
		}
	}
}
