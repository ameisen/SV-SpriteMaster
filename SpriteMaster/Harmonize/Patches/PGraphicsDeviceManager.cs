using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Extensions.Reflection;
using SpriteMaster.GL;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
internal static partial class PGraphicsDeviceManager {
	private struct DeviceState {
		private bool Initialized = false;
		private Vector2I Size = new(int.MinValue);
		private bool IsFullscreen = false;
		private DisplayOrientation Orientation = unchecked((DisplayOrientation)0xFFFF_FFFF);
		private bool HardwareModeSwitch = false;
		private bool PreferHalfPixelOffset = false;
		private GraphicsProfile Profile = (GraphicsProfile)(-1);

		public DeviceState() { }

		internal bool Dirty(GraphicsDeviceManager instance) {
			bool isFullscreen = instance.IsFullScreen;
			Vector2I size = (
				instance.PreferredBackBufferWidth,
				instance.PreferredBackBufferHeight
			);

			if (
				instance.GraphicsDevice is not null &&
				Initialized &&
				IsFullscreen == isFullscreen &&
				Size == size &&
				Orientation == instance.SupportedOrientations &&
				HardwareModeSwitch == instance.HardwareModeSwitch &&
				PreferHalfPixelOffset == instance.PreferHalfPixelOffset &&
				Profile == instance.GraphicsProfile
			) {
				return false;
			}

			Initialized = true;
			IsFullscreen = isFullscreen;
			Size = size;
			Orientation = instance.SupportedOrientations;
			HardwareModeSwitch = instance.HardwareModeSwitch;
			PreferHalfPixelOffset = instance.PreferHalfPixelOffset;
			Profile = instance.GraphicsProfile;
			return true;
		}
	}
	private static DeviceState LastState = new();

	[Harmonize(
		typeof(RenderTarget2D),
		Constructor,
		Fixation.Prefix,
		PriorityLevel.Last
	)]
	public static void OnRenderTarget2DConstruct(
		GraphicsDevice graphicsDevice,
		ref int width,
		ref int height,
		bool mipMap,
		ref SurfaceFormat preferredFormat,
		ref DepthFormat preferredDepthFormat,
		ref int preferredMultiSampleCount,
		RenderTargetUsage usage,
		bool shared,
		int arraySize,
		out bool __state
	) {
		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

		__state = ShouldManageRenderTarget(
			ref preferredFormat,
			ref preferredDepthFormat,
			ref preferredMultiSampleCount
		);
	}

	[Harmonize(
		typeof(RenderTarget2D),
		Constructor,
		Fixation.Postfix,
		PriorityLevel.Last
	)]
	public static void OnRenderTarget2DConstructPost(
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
		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;
		if (__state) {
			__instance.Meta().IsSystemRenderTarget = true;
		}
	}

	[Harmonize(
		"ApplyChanges",
		Fixation.Prefix,
		PriorityLevel.First
	)]
	public static bool OnApplyChanges(GraphicsDeviceManager __instance) {
		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;
		var @this = __instance;

		if (!LastState.Dirty(@this)) {
			return false;
		}

		DrawState.UpdateDeviceManager(@this);

		@this.GraphicsProfile = GraphicsProfile.HiDef;
		//@this.PreferMultiSampling = Config.DrawState.MSAASamples > 1;
		@this.SynchronizeWithVerticalRetrace = true;
		@this.PreferredBackBufferFormat = (SMConfig.DrawState.HonorHDRSettings && Runtime.IsHDR) ? SMConfig.DrawState.BackbufferHDRFormat : SMConfig.DrawState.BackbufferFormat;
		if (SMConfig.DrawState.DisableDepthBuffer) {
			@this.PreferredDepthStencilFormat = DepthFormat.None;
		}

		return true;
	}

	private static bool DumpedSystemInfo = false;
	private static WeakReference<GraphicsDevice>? LastGraphicsDevice = null;

	[Harmonize(
		"ApplyChanges",
		Fixation.Postfix,
		PriorityLevel.Last
	)]
	public static void OnApplyChangesPost(GraphicsDeviceManager __instance) {
		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;
		var @this = __instance;

		var device = @this.GraphicsDevice;
		DrawState.UpdateDevice();

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
				GLExt.Dump();
			}
			catch {
				// ignored
			}

			DumpedSystemInfo = true;
		}
		else {
			try {
				SystemInfo.Update(__instance, device);
			}
			catch {
				// ignored
			}
		}

		try {
			static FieldInfo? GetPrivateField(object obj, string name, bool instance = true) {
				return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static));
			}

			var capabilitiesProperty = GetPrivateField(device, "_profileCapabilities");

			if (capabilitiesProperty is null) {
				// Probably monogame?
				var maxTextureSizeProperty = GetPrivateField(device, "_maxTextureSize");
				int? maxTextureSize = maxTextureSizeProperty?.GetValue<int>(device);
				if (maxTextureSize.HasValue) {
					SMConfig.ClampDimension = maxTextureSize.Value;
				}
			}
			else {
				var capabilitiesMember = capabilitiesProperty.GetValue(device);

				if (capabilitiesMember is null) {
					throw new NullReferenceException(nameof(capabilitiesMember));
				}

				var capabilitiesList = new[] {
					GetPrivateField(capabilitiesMember, "HiDef", instance: false)?.GetValue(capabilitiesMember),
					capabilitiesMember
				};

				foreach (var capabilities in capabilitiesList) {
					if (capabilities is null) {
						continue;
					}
					var maxTextureSizeProperty = GetPrivateField(capabilities, "MaxTextureSize");

					if (maxTextureSizeProperty is null) {
						throw new NullReferenceException(nameof(maxTextureSizeProperty));
					}

					for (var currentDimension = SMConfig.AbsoluteMaxTextureDimension; currentDimension >= SMConfig.BaseMaxTextureDimension; currentDimension >>= 1) {
						maxTextureSizeProperty.SetValue(capabilities, currentDimension);
						var maxTextureAspectRatioField = GetPrivateField(capabilities, "MaxTextureAspectRatio");
						if (maxTextureAspectRatioField is null) {
							throw new NullReferenceException(nameof(maxTextureAspectRatioField));
						}
						maxTextureAspectRatioField.SetValue(capabilities, currentDimension / 2);
						try {
							SMConfig.ClampDimension = currentDimension;
							//Math.Min(i, Config.PreferredMaxTextureDimension);
							using (new DumpTexture2D(@this.GraphicsDevice, currentDimension, currentDimension) { Name = "Resolution Test Texture" })
							{
								/* do nothing. We want to dispose of it immediately. */
							}
							Garbage.Collect(compact: true, blocking: true, background: false);
							break;
						}
						catch {
							SMConfig.ClampDimension = SMConfig.BaseMaxTextureDimension;
							maxTextureSizeProperty.SetValue(capabilities, SMConfig.BaseMaxTextureDimension);
							maxTextureAspectRatioField.SetValue(capabilities, SMConfig.BaseMaxTextureDimension / 2);
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
