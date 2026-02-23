using JetBrains.Annotations;
using LinqFasterer;
using Microsoft.Xna.Framework.Input;
using SpriteMaster.Caching;
using SpriteMaster.Experimental;
using SpriteMaster.Extensions;
using SpriteMaster.Harmonize.Patches.Game;
using SpriteMaster.Metadata;
using SpriteMaster.Tasking;
using SpriteMaster.Types;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster;

public sealed partial class SpriteMaster : Mod {
	private bool TryAddConsoleCommand(string name, string documentation, Action<string, string[]> callback) {
		try {
			Helper.ConsoleCommands.Add(name, documentation, callback);
			return true;
		}
		catch (Exception ex)
		{
			Debug.Warning($"Could not register '{name}' for console commands", ex);
			return false;
		}
	}

	private void InitializeEvents() {
		var gameLoop = Helper.Events.GameLoop;

		Helper.Events.Input.ButtonPressed += OnButtonPressed;

		gameLoop.DayEnding += OnDayEnded;
		gameLoop.DayStarted += OnDayStarted;
		// GC after major events
		gameLoop.SaveLoaded += (_, _) => {
			ForceGarbageCollect();
			Garbage.EnterInteractive();
		};
		gameLoop.DayEnding += (_, _) => ForceGarbageCollect();
		gameLoop.ReturnedToTitle += (_, _) => OnTitle();
		gameLoop.GameLaunched += (_, _) => OnGameLaunched();
		gameLoop.SaveCreating += (_, _) => OnSaveStart();
		gameLoop.Saving += (_, _) => OnSaveStart();
		gameLoop.SaveCreated += (_, _) => OnSaveFinish();
		gameLoop.Saved += (_, _) => OnSaveFinish();
		Helper.Events.Display.WindowResized += (_, args) => OnWindowResized(args);
		Helper.Events.Player.Warped += OnWarp;
		Helper.Events.Specialized.LoadStageChanged += (_, args) => {
			switch (args.NewStage) {
				case LoadStage.SaveLoadedBasicInfo:
				case LoadStage.SaveLoadedLocations:
				case LoadStage.Preloaded:
				case LoadStage.ReturningToTitle:
					Garbage.EnterNonInteractive();
					break;
			}
		};
		Helper.Events.Display.MenuChanged += OnMenuChanged;
	}

	[UsedImplicitly]
	public override void Entry(IModHelper help) {
#if !SHIPPING
		ModManifest.UniqueID.AssertEqual(UniqueId);
#endif

		Initialize();

		if (SMConfig.ShowIntroMessage && !SMConfig.SkipIntro) {
			Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			SMConfig.ShowIntroMessage = false;
		}

		Configuration.Serialize.Save(SMConfig.Path);

		foreach (var prefix in new[] { "spritemaster", "sm" }) {
			_ = TryAddConsoleCommand(prefix, "SpriteMaster Commands", ConsoleSupport.Invoke);
		}

		InitializeEvents();

		PostInitialize();

		static void SetSystemTarget(XGraphics.RenderTarget2D? target) {
			if (target is null) {
				return;
			}

			target.Meta().IsSystemRenderTarget = true;
		}

		SetSystemTarget(Game1.lightmap);
		SetSystemTarget(Game1.game1.screen);
		SetSystemTarget(Game1.game1.uiScreen);

		// TODO : Iterate deeply with reflection over 'StardewValley' namespace to find any XTexture2D objects sitting around
	}

	private static class ModUid {
		internal const string DynamicGameAssets = "spacechase0.DynamicGameAssets";
		internal const string ContentPatcher = "Pathoschild.ContentPatcher";
		internal const string ContentPatcherAnimations = "spacechase0.ContentPatcherAnimations";
	}

	private void OnUpdateTicked(object? sender, UpdateTickedEventArgs args) {
		if (!SMConfig.ShowIntroMessage) {
			return;
		}

		if (Game1.ticks <= 1) {
			return;
		}

		Configuration.ConfigMenu.Setup.ForceOpen();

		Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
	}

	private void OnWindowResized(WindowResizedEventArgs args) {
		if (args.NewSize == args.OldSize) {
			return;
		}
		Snow.OnWindowResized(args.NewSize);
	}

	private void OnWarp(object? _, WarpedEventArgs args) {
		if (SMConfig.AsyncScaling.FlushSynchronizedTasksOnWarp) {
			SynchronizedTaskScheduler.Instance.FlushPendingTasks();
		}

		ForceGarbageCollectConcurrent();
	}

	private void OnMenuChanged(object? _, MenuChangedEventArgs args) {
		//_ = _;
	}

	private void OnSaveStart() {
		SynchronizedTaskScheduler.Instance.FlushPendingTasks();
		Garbage.EnterNonInteractive();
	}

	private void OnSaveFinish() {
		SynchronizedTaskScheduler.Instance.FlushPendingTasks();
		ForceGarbageCollect();
		Garbage.EnterInteractive();
	}

	private void OnTitle() {
		ForceGarbageCollect();
		Garbage.EnterInteractive();
	}

	internal void OnFirstDraw() {
		Garbage.EnterInteractive();
	}

	private const string UnderTestingMessage = "which is still in testing under SpriteMaster - results may vary";
	private static readonly (string UID, string Name, string Message)[] WarnFrameworks = new (string UID, string Name, string Message)[] {
		(ModUid.ContentPatcherAnimations, "Content Patcher Animations", UnderTestingMessage),
		(ModUid.DynamicGameAssets, "Dynamic Game Assets", UnderTestingMessage),
	};

	[MethodImpl(Runtime.MethodImpl.RunOnce)]
	private void CheckMods() {
		var frameworkedMods = new Dictionary<string, List<IModInfo>>();

		foreach (var mod in Helper.ModRegistry.GetAll()) {
			var manifest = mod.Manifest;

			foreach (var framework in WarnFrameworks) {
				if (
					!manifest.Dependencies.AnyF(d => d.UniqueID == framework.UID) &&
					manifest.ContentPackFor?.UniqueID != framework.UID
				) {
					continue;
				}

				if (!frameworkedMods.TryGetValue(framework.UID, out var list)) {
					list = new List<IModInfo>();
					frameworkedMods.Add(framework.UID, list);
				}
				list.Add(mod);
				break;
			}
		}

		foreach (var modsPair in frameworkedMods) {
			if (modsPair.Value.Count == 0) {
				continue;
			}

			var framework = WarnFrameworks.FirstF(framework => framework.UID == modsPair.Key);

			var sb = new StringBuilder();
			sb.AppendLine($"The following mods have a dependency on {framework.Name} ({framework.UID}), {framework.Message}:");

			foreach (var mod in modsPair.Value) {
				sb.AppendLine($"\t{mod.Manifest.Name} ({mod.Manifest.UniqueID})");
			}

			Debug.Info(sb.ToString());
		}
	}

	[StructLayout(LayoutKind.Auto)]
	private readonly struct WaitWrapper : IDisposable {
		private readonly object Waiter;

		internal WaitWrapper(object waiter) => Waiter = waiter;

		public void Dispose() {
			if (Waiter is IDisposable disposable) {
				disposable.Dispose();
			}
		}

		internal void Wait() {
			switch (Waiter) {
				case Task task:
					task.Wait();
					break;
				case ManualCondition condition:
					condition.Wait();
					break;
				default:
					ThrowHelper.ThrowInvalidOperationException(Waiter.GetType().Name);
					break;
			}
		}
	}

	private void OnGameLaunched() {
		var waiters = new WaitWrapper[] {
			new(Task.Run(CheckMods)),
			new(FileCache.Initialized),
			new(Task.Run(Configuration.ConfigMenu.Setup.Initialize))
		};

		foreach (var waiter in waiters) {
			waiter.Wait();
			waiter.Dispose();
		}

		ForceGarbageCollect();
		ManagedSpriteInstance.ClearTimers();
	}

	private static void OnDayEnded(object? _, DayEndingEventArgs _1) {
		SynchronizedTaskScheduler.Instance.FlushPendingTasks();
	}

	// SMAPI/CP won't do this, so we do. Purge the cached textures for the previous season on a season change.
	private static void OnDayStarted(object? _, DayStartedEventArgs _1) {
		Snow.PopulateDebrisWeatherArray();

		SynchronizedTaskScheduler.Instance.FlushPendingTasks();

		// Do a full GC at the start of each day
		Garbage.Collect(compact: true, blocking: true, background: false);

		var season = Game1.currentSeason;
		if (!season.EqualsInvariantInsensitive(GameState.CurrentSeason)) {
			GameState.CurrentSeason = season;
			SpriteMap.SeasonPurge(season.ToLowerInvariant());

			// And again after purge
			Garbage.Collect(compact: true, blocking: true, background: false);
		}
	}

	private static void OnButtonPressed(object? _, ButtonPressedEventArgs args) {

		if (args.Button == SMConfig.ToggleButton) {
			var keyboardState = Game1.GetKeyboardState();
			var control = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
			if (control) {
				SMConfig.ToggledEnable = !SMConfig.ToggledEnable;
			}
			else {
				SMConfig.Resample.ToggledEnable = !SMConfig.Resample.ToggledEnable;
			}
		}
	}
}
