using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;

namespace SpriteMaster.Configuration.Preview;

abstract class Scene : IDisposable {
	internal static Scene? Current = null;
	protected const int TileSize = 16;
	protected const int TileSizeRendered = TileSize * 4;

	private struct CurrentScope : IDisposable {
		private readonly Scene? PreviousScene;

		internal CurrentScope(Scene scene) {
			PreviousScene = Current;
			Current = scene;
		}

		public void Dispose() {
			Current = PreviousScene;
		}
	}

	internal static readonly Lazy<StardewValley.GameLocation> SceneLocation = new(() => new StardewValley.Locations.Beach(@"Maps\Beach", "SMSettingsLocation"));

	internal abstract PrecipitationType Precipitation { get; }

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

	protected static Vector2I GetSizeInTiles(Vector2I size) => size / TileSize;

	protected static Vector2I GetSizeInRenderedTiles(Vector2I size) => size / TileSizeRendered;

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
		Vector2I shift = Vector2I.Zero;
		if (destination.Extent.MaxOf > TileSizeRendered) {
			var centroid = destination.Center;
			var end = destination.Offset + destination.Extent;
			var difference = end - centroid;
			difference /= TileSizeRendered;
			difference *= TileSizeRendered;

			Vector2I hasValue = ((destination.Extent.X > TileSizeRendered).ToInt(), (destination.Extent.Y > TileSizeRendered).ToInt());
			difference -= hasValue * (TileSizeRendered / 2);

			var odd = (destination.Extent / TileSizeRendered) & 1;
			odd &= hasValue;
			difference -= odd * (TileSizeRendered / 2);

			shift = -difference;
		}

		var offset = (destination.Extent >> 3);

		batch.Draw(
			texture: texture,
			destinationRectangle: destination.OffsetBy(shift),
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

		Vector2I shift = Vector2I.Zero;
		if (size.MaxOf > TileSizeRendered) {
			var centroid = size / 2;
			var end = size;
			var difference = end - centroid;
			difference /= TileSizeRendered;
			difference *= TileSizeRendered;

			Vector2I hasValue = ((size.X > TileSizeRendered).ToInt(), (size.Y > TileSizeRendered).ToInt());
			difference -= hasValue * (TileSizeRendered / 2);

			var odd = (size / TileSizeRendered) & 1;
			odd &= hasValue;
			difference -= odd * (TileSizeRendered / 2);

			shift = -difference;
		}

		var offset = (size >> 3);

		var bounds = new Bounds(destination, size);

		batch.Draw(
			texture: texture,
			destinationRectangle: bounds.OffsetBy(shift),
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
		using var currentScope = new CurrentScope(this);

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
					batch.GraphicsDevice.Viewport = new(Region);
					OnDraw(batch, overrideState);
					batch.End();
					if (true) { // precipitation
						batch.Begin(sortMode: SpriteSortMode.FrontToBack, rasterizerState: State, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
						StardewValley.Game1.game1.drawWeather(StardewValley.Game1.currentGameTime, null);
						batch.End();
					}
					batch.Begin(sortMode: SpriteSortMode.FrontToBack, rasterizerState: State, samplerState: originalSamplerState, depthStencilState: originalDepthStencilState);
					OnDrawOverlay(batch, overrideState);
					batch.GraphicsDevice.Viewport = originalViewport;
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
			StardewValley.Game1.spriteBatch = originalSpriteBatch;
		}

		CurrentWeatherState = WeatherState.Backup();
	}

	protected abstract void OnTick();

	internal void Tick() {
		using var savedWeatherState = WeatherState.Backup();
		CurrentWeatherState.Restore();
		using var currentScope = new CurrentScope(this);

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

		CurrentWeatherState = WeatherState.Backup();
	}
	protected abstract void OnResize(Vector2I Size, Vector2I OldSize);

	internal void Resize(in Bounds newRegion) {
		using var savedWeatherState = WeatherState.Backup();
		CurrentWeatherState.Restore();
		using var currentScope = new CurrentScope(this);

		var oldSize = Size;
		Region = newRegion;

		OnResize(Size, oldSize);

		CurrentWeatherState = WeatherState.Backup();
	}

	public abstract void Dispose();
}
