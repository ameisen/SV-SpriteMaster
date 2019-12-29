﻿using Harmony;
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
		public static SpriteMaster Self { get; private set; } = default;

		// TODO : long for 64-bit?
		private readonly Thread MemoryPressureThread = null;

		private void MemoryPressureLoop() {
			for (;;) {
				if (DrawState.TriggerGC) {
					Thread.Sleep(128);
					continue;
				}

				try {
					using var _ = new MemoryFailPoint(Config.RequiredFreeMemory);
					Thread.Sleep(128);
				}
				catch (InsufficientMemoryException) {
					Debug.WarningLn($"Less than {(Config.RequiredFreeMemory * 1024 * 1024).AsDataSize(decimals: 0)} available for block allocation, forcing full garbage collection");
					DrawState.TriggerGC = true;
					Thread.Sleep(10000);
				}
			}
		}
		
		private static readonly string ConfigName = "config.toml";

		private static string CurrentSeason = "";

		public SpriteMaster () {
			Contract.AssertNull(Self);
			Self = this;

			MemoryPressureThread = new Thread(MemoryPressureLoop);
			MemoryPressureThread.Priority = ThreadPriority.BelowNormal;
			MemoryPressureThread.IsBackground = true;
		}

		public override void Entry (IModHelper help) {
			var ConfigPath = Path.Combine(help.DirectoryPath, ConfigName);
			SerializeConfig.Load(ConfigPath);
			SerializeConfig.Save(ConfigPath);
			
			ConfigureHarmony();
			help.Events.Input.ButtonPressed += OnButtonPressed;

			help.ConsoleCommands.Add("spritemaster_stats", "Dump SpriteMaster Statistics", (_, _1) => { ManagedTexture2D.DumpStats(); });
			help.ConsoleCommands.Add("spritemaster_memory", "Dump SpriteMaster Memory", (_, _1) => { Debug.DumpMemory(); });

			//help.ConsoleCommands.Add("night", "make it dark", (_, _1) => { help.ConsoleCommands.Trigger("world_settime", new string[] { "2100" }); });

			help.Events.GameLoop.DayStarted += OnDayStarted;
			// GC after major events
			help.Events.GameLoop.SaveLoaded += (_, _1) => Garbage.Collect(compact: true, blocking: true, background: false);

			MemoryPressureThread.Start();
		}

		// SMAPI/CP won't do this, so we do. Purge the cached textures for the previous season on a season change.
		private static void OnDayStarted(object _, DayStartedEventArgs _1) {
			// Do a full GC at the start of each day
			Garbage.Collect(compact: true, blocking: true, background: false);
			
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
