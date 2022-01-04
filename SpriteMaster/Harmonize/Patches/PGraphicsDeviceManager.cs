using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
static class PGraphicsDeviceManager {

	// D3DCREATE_MULTITHREADED
	/*
	[HarmonyPatch(typeof(GraphicsDevice), "CreateDevice", HarmonyPatch.Fixation.Transpile, PriorityLevel.First)]
	internal static IEnumerable<Harmony.CodeInstruction> CreateDeviceTranspiler (IEnumerable<Harmony.CodeInstruction> instructions) {
		return new List<Harmony.CodeInstruction>(instructions);
	}
	*/

	private struct DeviceState {
		internal Vector2I Size = new(int.MinValue);
		internal bool Initialized = false;
		internal bool IsFullscreen = false;

		internal bool Dirty(GraphicsDeviceManager instance) {
			bool isFullscreen = instance.IsFullScreen;
			Vector2I size = (
				instance.PreferredBackBufferWidth,
				instance.PreferredBackBufferHeight
			);

			if (Initialized && IsFullscreen == isFullscreen && Size == size) {
				return false;
			}

			Initialized = true;
			IsFullscreen = isFullscreen;
			Size = size;
			return true;
		}
	}
	private static DeviceState LastState = new();

	private struct GameWindowState {
		internal Vector2I Size = new(int.MinValue);
		internal int GameInstanceCount = 0;
		internal int GameInstanceIndex = 0;
		internal float ZoomModifier = 0.0f;
		internal bool Initialized = false;
		internal bool IsFullscreen = false;
		internal bool OverrideGameMenuReset = false;

		// Windows
		internal StardewValley.Menus.TextEntryMenu TextEntry = null;
		internal StardewValley.Minigames.IMinigame CurrentMinigame = null;
		internal IClickableMenu ActiveClickableMenu = null;
		internal int ACMCurrentTab = int.MinValue;
		internal IClickableMenu ACMCurrentPage = null;
		internal WeakReference<IClickableMenu>[] OnScreenMenus = null;

		private bool CheckWindowsDirty(StardewValley.Game1 instance) {
			return
				Game1.textEntry != TextEntry ||
				Game1.currentMinigame != CurrentMinigame ||
				Game1.activeClickableMenu != ActiveClickableMenu ||
				(Game1.activeClickableMenu as GameMenu)?.GetCurrentPage() != ACMCurrentPage;
		}

		private bool CheckOnScreenMenusDirty() {
			var onscreenMenus = Game1.onScreenMenus;
			if (onscreenMenus is null != OnScreenMenus is null) {
				return true;
			}
			if (onscreenMenus is null) {
				return true;
			}
			if (onscreenMenus.Count != OnScreenMenus.Length) {
				return true;
			}
			for (int i = 0; i < onscreenMenus.Count; ++i) {
				if (OnScreenMenus[i] is null) {
					return onscreenMenus[i] is not null;
				}
				if (!(OnScreenMenus[i].TryGetTarget(out var menu))) {
					return true;
				}
				if (menu != onscreenMenus[i]) {
					return true;
				}
			}
			return false;
		}

		private void SetOnScreenMenusDirty() {
			if (Game1.onScreenMenus is null) {
				OnScreenMenus = null;
			}
			OnScreenMenus = new WeakReference<IClickableMenu>[Game1.onScreenMenus.Count];
			for (int i = 0; i < Game1.onScreenMenus.Count; ++i) {
				if (Game1.onScreenMenus[i] is null) {
					OnScreenMenus[i] = null;
					continue;
				}
				OnScreenMenus[i] = Game1.onScreenMenus[i].MakeWeak();
			}
		}

		private void SetWindowsDirty(StardewValley.Game1 instance) {
			TextEntry = Game1.textEntry;
			CurrentMinigame = Game1.currentMinigame;
			ActiveClickableMenu = Game1.activeClickableMenu;
			ACMCurrentPage = (Game1.activeClickableMenu as GameMenu)?.GetCurrentPage();
		}

		internal bool Dirty(StardewValley.Game1 instance, Vector2I size) {
			bool isFullscreen = Game1.graphics.IsFullScreen;
			int gameInstanceCount = GameRunner.instance.gameInstances.Count;

			if (
				Initialized &&
				IsFullscreen == isFullscreen &&
				Size == size &&
				GameInstanceCount == gameInstanceCount &&
				GameInstanceIndex == instance.instanceIndex &&
				ZoomModifier == instance.zoomModifier && // TODO : should we use approximate equality?
				OverrideGameMenuReset == Game1.overrideGameMenuReset &&
				!CheckWindowsDirty(instance) &&
				!CheckOnScreenMenusDirty()
			) {
				return false;
			}

			Initialized = true;
			IsFullscreen = isFullscreen;
			Size = size;
			GameInstanceCount = gameInstanceCount;
			GameInstanceIndex = instance.instanceIndex;
			ZoomModifier = instance.zoomModifier;
			OverrideGameMenuReset = Game1.overrideGameMenuReset;
			SetWindowsDirty(instance);
			SetOnScreenMenusDirty();
			return true;
		}
	}
	private static GameWindowState LastGameWindowState = new();

	[Harmonize(typeof(Microsoft.Xna.Framework.Graphics.RenderTarget2D), Harmonize.Constructor, Harmonize.Fixation.Prefix, PriorityLevel.Last)]
	internal static bool OnRenderTarget2DConstruct(
		GraphicsDevice graphicsDevice,
		int width,
		int height,
		bool mipMap,
		ref SurfaceFormat preferredFormat,
		ref DepthFormat preferredDepthFormat,
		ref int preferredMultiSampleCount,
		RenderTargetUsage usage,
		bool shared,
		int arraySize,
		out bool __state
	) {
		var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: false);

		if (stackTrace.GetFrame(0)?.GetMethod()?.DeclaringType == typeof(StardewValley.Game1)) {
			__state = true;
		}

		foreach (var frame in stackTrace.GetFrames()) {
			var method = frame.GetMethod();
			if (method?.DeclaringType != typeof(StardewValley.Game1)) {
				continue;
			}

			switch (method?.Name) {
				case "SetWindowSize": {
					__state = true;
					GraphicsDevice device = null;
					if (!LastGraphicsDevice?.TryGetTarget(out device) ?? false || device is null) {
						return true;
					}

					preferredMultiSampleCount = Config.DrawState.EnableMSAA ? 16 : 0;
					preferredDepthFormat = device.PresentationParameters.DepthStencilFormat;
					preferredFormat = device.PresentationParameters.BackBufferFormat;
				} return true;

				case "Initialize":
				case "allocateLightmap":
				case "takeMapScreenshot": {
					__state = true;
				} return true;
			}
		}

		__state = false;
		return true;
	}

	[Harmonize(typeof(Microsoft.Xna.Framework.Graphics.RenderTarget2D), Harmonize.Constructor, Harmonize.Fixation.Postfix, PriorityLevel.Last)]
	internal static void OnRenderTarget2DConstructPost(
		RenderTarget2D __instance,
		GraphicsDevice graphicsDevice,
		int width,
		int height,
		bool mipMap,
		SurfaceFormat preferredFormat,
		DepthFormat preferredDepthFormat,
		int preferredMultiSampleCount,
		RenderTargetUsage usage,
		bool shared,
		int arraySize,
		bool __state
	) {
		if (__state) {
			__instance.Meta().IsSystemRenderTarget = true;
		}
	}

	// This is an additional patch to try to reduce the number of superfluous state changes
	/*
	[Harmonize(typeof(StardewValley.Game1), "SetWindowSize", Fixation.Prefix, PriorityLevel.Last, critical: false)]
	internal static bool OnSetWindowSize(StardewValley.Game1 __instance, int w, int h) {
		bool dirty = false;

		dirty = LastGameWindowState.Dirty(__instance, (w, h)) || dirty;
		dirty = Game1.graphics.SynchronizeWithVerticalRetrace != Game1.options.vsyncEnabled || dirty;

		dirty = __instance.screen is null || dirty;
		dirty = __instance.uiScreen is null || dirty;

		return dirty;
	}
	*/

	[Harmonize("ApplyChanges", Harmonize.Fixation.Prefix, PriorityLevel.First)]
	internal static bool OnApplyChanges(GraphicsDeviceManager __instance) {
		var @this = __instance;

		if (!LastState.Dirty(@this)) {
			return false;
		}

		DrawState.UpdateDeviceManager(@this);

		@this.GraphicsProfile = GraphicsProfile.HiDef;
		@this.PreferMultiSampling = Config.DrawState.EnableMSAA;
		@this.SynchronizeWithVerticalRetrace = true;
		@this.PreferredBackBufferFormat = Config.DrawState.BackbufferFormat;
		if (Config.DrawState.DisableDepthBuffer) {
			@this.PreferredDepthStencilFormat = DepthFormat.None;
		}

		return true;
	}

	private static bool DumpedSystemInfo = false;
	private static WeakReference<GraphicsDevice> LastGraphicsDevice = null;

	[Harmonize("ApplyChanges", Harmonize.Fixation.Postfix, PriorityLevel.Last)]
	internal static void OnApplyChangesPost(GraphicsDeviceManager __instance) {
		var @this = __instance;

		var device = @this.GraphicsDevice;

		if (LastGraphicsDevice is null) {
			LastGraphicsDevice = device.MakeWeak();
		}
		else if (!LastGraphicsDevice.TryGetTarget(out var lastDevice) || lastDevice != device) {
			LastGraphicsDevice.SetTarget(device);
		}
		else {
			return;
		}

		if (!DumpedSystemInfo) {
			try {
				SystemInfo.Dump(__instance, device);
			}
			catch { }
			DumpedSystemInfo = true;
		}

		try {
			static FieldInfo getPrivateField(object obj, string name, bool instance = true) {
				return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static));
			}

			var capabilitiesProperty = getPrivateField(device, "_profileCapabilities");

			if (capabilitiesProperty is null) {
				// Probably monogame?
				var maxTextureSizeProperty = getPrivateField(device, "_maxTextureSize");
				int? maxTextureSize = maxTextureSizeProperty?.GetValue<int>(device);
				if (maxTextureSize.HasValue) {
					Config.ClampDimension = maxTextureSize.Value;
				}
			}
			else {
				var capabilitiesMember = capabilitiesProperty.GetValue(device);

				var capabilitiesList = new[] {
					getPrivateField(capabilitiesMember, "HiDef", instance: false).GetValue(capabilitiesMember),
					capabilitiesMember
				};

				foreach (var capabilities in capabilitiesList) {
					if (capabilities is null) {
						continue;
					}
					var maxTextureSizeProperty = getPrivateField(capabilities, "MaxTextureSize");
					for (var currentDimension = Config.AbsoluteMaxTextureDimension; currentDimension >= Config.BaseMaxTextureDimension; currentDimension >>= 1) {
						maxTextureSizeProperty.SetValue(capabilities, currentDimension);
						getPrivateField(capabilities, "MaxTextureAspectRatio").SetValue(capabilities, currentDimension / 2);
						try {
							Config.ClampDimension = currentDimension;
							//Math.Min(i, Config.PreferredMaxTextureDimension);
							using (var testTexture = new Texture2D(@this.GraphicsDevice, currentDimension, currentDimension)) {
								/* do nothing. We want to dispose of it immediately. */
							}
							Garbage.Collect(compact: true, blocking: true, background: false);
							break;
						}
						catch {
							Config.ClampDimension = Config.BaseMaxTextureDimension;
							maxTextureSizeProperty.SetValue(capabilities, Config.BaseMaxTextureDimension);
							getPrivateField(capabilities, "MaxTextureAspectRatio").SetValue(capabilities, Config.BaseMaxTextureDimension / 2);
						}
						Garbage.Collect(compact: true, blocking: true, background: false);
					}
				}
			}
		}
		catch (Exception ex) {
			ex.PrintWarning();
		}
	}
}
