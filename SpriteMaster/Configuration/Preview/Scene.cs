using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using StardewValley;
using StardewValley.Locations;
using System;

using SMHarmonize = SpriteMaster.Harmonize;

namespace SpriteMaster.Configuration.Preview;

abstract class Scene : IDisposable {
	private static readonly Lazy<StardewValley.GameLocation> SceneLocation = new(() => new StardewValley.Locations.Beach(@"Maps\Beach", "SMSettingsLocation"));

	private static volatile int IsDrawing = 0;
	private static volatile int IsUpdating = 0;
	private static volatile PrecipitationType CurrentPrecipitation = PrecipitationType.None;
	protected abstract PrecipitationType Precipitation { get; }

	protected enum PrecipitationType {
		None = 0,
		Rain,
		Snow
	}

	private bool IsWeatherStateSet = false;
	private WeatherState InternalWeatherState;

	private ref WeatherState CurrentWeatherState {
		get {
			if (!IsWeatherStateSet) {
				Harmonize.Patches.Game.Snow.PopulateWeather(Region.Extent);
				InternalWeatherState = WeatherState.Backup();
				IsWeatherStateSet = true;
			}

			return ref InternalWeatherState;
		}
	}

	[SMHarmonize.Harmonize(
	typeof(Game1),
	"IsSnowingHere",
	SMHarmonize.Harmonize.Fixation.Prefix,
	SMHarmonize.Harmonize.PriorityLevel.Last,
	instance: false,
	critical: false
)]
	public static bool IsSnowingHere(ref bool __result, GameLocation location) {
		if (IsDrawing == 0 && IsUpdating == 0) {
			return true;
		}

		if (CurrentPrecipitation != PrecipitationType.Snow) {
			return true;
		}

		if (location == SceneLocation.Value || location is null) {
			__result = true;
			return false;
		}

		return true;
	}

	[SMHarmonize.Harmonize(
		typeof(Game1),
		"IsRainingHere",
		SMHarmonize.Harmonize.Fixation.Prefix,
		SMHarmonize.Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool IsRainingHere(ref bool __result, GameLocation location) {
		if (IsDrawing == 0 && IsUpdating == 0) {
			return true;
		}

		if (CurrentPrecipitation != PrecipitationType.Rain) {
			return true;
		}

		if (location == SceneLocation.Value || location is null) {
			__result = true;
			return false;
		}

		return true;
	}

	protected readonly ref struct TempValue<T> {
		private readonly T? OriginalValue;
		private readonly Ref<T?> ReferenceValue;

		internal TempValue(ref T? value, in T? newValue) {
			OriginalValue = value;
			value = newValue;
			ReferenceValue = new(ref value);
		}

		public void Dispose() {
			ReferenceValue.Value = OriginalValue;
		}
	}

	internal Bounds Region { get; private set; }
	private RasterizerState? State = null;
	protected Vector2I Size => Region.Extent;

	protected Scene(in Bounds region) {
		Region = region;
	}

	internal void DrawAt(
		XNA.Graphics.SpriteBatch batch,
		XTexture2D texture,
		in Bounds destination,
		in Bounds? source = null,
		Color8? color = null,
		float rotation = 0.0f,
		SpriteEffects effects = SpriteEffects.None,
		float layerDepth = 0.0f
	) {
		var offset = destination.Extent >> 3;

		batch.Draw(
			texture: texture,
			destinationRectangle: destination.OffsetBy(Region.Offset),
			sourceRectangle: source,
			color: color ?? XNA.Color.White,
			rotation: rotation,
			origin: offset,
			effects: effects,
			layerDepth: layerDepth
		);
	}

	internal void DrawAt(
		XNA.Graphics.SpriteBatch batch,
		XTexture2D texture,
		Vector2I destination,
		in Bounds? source = null,
		Color8? color = null,
		float rotation = 0.0f,
		Vector2F? origin = null,
		SpriteEffects effects = SpriteEffects.None,
		float layerDepth = 0.0f
	) {
		var size = new Vector2I(source?.Width ?? texture.Width, source?.Height ?? texture.Height) << 2;
		var offset = size >> 3;

		var bounds = new Bounds(destination + Region.Offset, size);

		batch.Draw(
			texture: texture,
			destinationRectangle: bounds,
			sourceRectangle: source,
			color: color ?? XNA.Color.White,
			rotation: 0.0f,
			origin: offset,
			effects: effects,
			layerDepth: layerDepth
		);
	}

	internal void DrawAt(
		XNA.Graphics.SpriteBatch batch,
		AnimatedTexture texture,
		Vector2I destination,
		Color8? color = null,
		float rotation = 0.0f,
		SpriteEffects effects = SpriteEffects.None,
		float layerDepth = 0.0f
	) {
		DrawAt(
			batch: batch,
			texture: texture.Texture,
			destination: new Bounds(destination, texture.Size << 2),
			source: texture.Current,
			color: color,
			rotation: rotation,
			effects: effects,
			layerDepth: layerDepth
		);
	}

	protected abstract void OnDraw(XNA.Graphics.SpriteBatch batch, in Preview.Override overrideState);
	protected abstract void OnDrawOverlay(XNA.Graphics.SpriteBatch batch, in Preview.Override overrideState);

	internal void Draw(XNA.Graphics.SpriteBatch batch, in Preview.Override overrideState) {
		using var savedWeatherState = WeatherState.Backup();
		CurrentWeatherState.Restore();

		++IsDrawing;
		CurrentPrecipitation = Precipitation;
		var originalSpriteBatch = StardewValley.Game1.spriteBatch;
		StardewValley.Game1.spriteBatch = batch;
		try {

			var originalLocation = StardewValley.Game1.currentLocation;
			StardewValley.Game1.currentLocation = SceneLocation.Value;
			try {

				{
					using var tempBatch = new TempValue<XNA.Graphics.SpriteBatch>(ref StardewValley.Game1.spriteBatch, batch);
					StardewValley.Game1.DrawBox(Region.X, Region.Y, Region.Width, Region.Height);
				}

				batch.End();
				var originalViewport = batch.GraphicsDevice.Viewport;
				var originalSamplerState = batch.GraphicsDevice.SamplerStates[0];
				var originalDepthStencilState = batch.GraphicsDevice.DepthStencilState;

				var originalRasterizerState = batch.GraphicsDevice.RasterizerState;
				State ??= new RasterizerState {
					CullMode = originalRasterizerState.CullMode,
					DepthBias = originalRasterizerState.DepthBias,
					FillMode = originalRasterizerState.FillMode,
					MultiSampleAntiAlias = originalRasterizerState.MultiSampleAntiAlias,
					ScissorTestEnable = true,
					SlopeScaleDepthBias = originalRasterizerState.SlopeScaleDepthBias,
					DepthClipEnable = originalRasterizerState.DepthClipEnable,
				};

				//batch.GraphicsDevice.Viewport = new(Region);
				var originalScissor = batch.GraphicsDevice.ScissorRectangle;
				batch.GraphicsDevice.ScissorRectangle = Region;
				batch.Begin(sortMode: SpriteSortMode.FrontToBack, rasterizerState: State, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
				try {
					using var tempOverrideState = new TempValue<Preview.Override>(ref Preview.Override.Instance, overrideState);
					OnDraw(batch, overrideState);
					batch.End();
					if (true) { // precipitation
						batch.GraphicsDevice.Viewport = new(Region);
						batch.Begin(sortMode: SpriteSortMode.FrontToBack, rasterizerState: State, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
						StardewValley.Game1.game1.drawWeather(StardewValley.Game1.currentGameTime, null);
						batch.End();
						batch.GraphicsDevice.Viewport = originalViewport;
					}
					batch.Begin(sortMode: SpriteSortMode.FrontToBack, rasterizerState: State, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
					OnDrawOverlay(batch, overrideState);
				}
				finally {
					batch.End();
					batch.GraphicsDevice.ScissorRectangle = originalScissor;
					batch.Begin(rasterizerState: originalRasterizerState, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
				}
			}
			finally {
				StardewValley.Game1.currentLocation = originalLocation;
			}
		}
		finally {
			--IsDrawing;
			CurrentPrecipitation = PrecipitationType.None;
			StardewValley.Game1.spriteBatch = originalSpriteBatch;
		}

		CurrentWeatherState = WeatherState.Backup();
	}

	protected abstract void OnTick();

	internal void Tick() {
		using var savedWeatherState = WeatherState.Backup();
		CurrentWeatherState.Restore();

		++IsUpdating;
		CurrentPrecipitation = Precipitation;
		try {
			var originalLocation = StardewValley.Game1.currentLocation;
			StardewValley.Game1.currentLocation = SceneLocation.Value;
			try {
				if (true) { // precipitation
					var originalDeviceViewport = Game1.graphics.GraphicsDevice.Viewport;
					var originalGameViewport = Game1.viewport;
					var originalFadeToBlackAlpha = Game1.fadeToBlackAlpha;
					try {
						Game1.graphics.GraphicsDevice.Viewport = new(Region);
						Game1.viewport = Region;
						Game1.fadeToBlackAlpha = 0.0f;
						StardewValley.Game1.updateWeather(StardewValley.Game1.currentGameTime);
					}
					finally {
						Game1.graphics.GraphicsDevice.Viewport = originalDeviceViewport;
						Game1.viewport = originalGameViewport;
						Game1.fadeToBlackAlpha = originalFadeToBlackAlpha;
					}
				}

				OnTick();
			}
			finally {
				StardewValley.Game1.currentLocation = originalLocation;
			}
		}
		finally {
			CurrentPrecipitation = PrecipitationType.None;
			--IsUpdating;
		}

		CurrentWeatherState = WeatherState.Backup();
	}
	protected abstract void OnResize(Vector2I Size, Vector2I OldSize);

	internal void Resize(in Bounds newRegion) {
		using var savedWeatherState = WeatherState.Backup();
		CurrentWeatherState.Restore();

		var oldSize = Size;
		Region = newRegion;

		OnResize(Size, oldSize);

		CurrentWeatherState = WeatherState.Backup();
	}

	public abstract void Dispose();
}
