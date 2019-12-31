using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	static class Draw {
		private const bool Continue = true;
		private const bool Stop = false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Cleanup (this ref Rectangle sourceRectangle, Texture2D reference) {
			if (Config.ClampInvalidBounds) {
				sourceRectangle = sourceRectangle.ClampTo(new Rectangle(0, 0, reference.Width, reference.Height));
			}

			// Let's just skip potentially invalid draws since I have no idea what to do with them.
			return (sourceRectangle.Height > 0 && sourceRectangle.Width > 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool FetchScaledTexture (
			this Texture2D reference,
			int expectedScale,
			ref Rectangle source,
			out ScaledTexture scaledTexture,
			bool create = false
		) {
			scaledTexture = reference.FetchScaledTexture(
				expectedScale: expectedScale,
				source: ref source,
				create: create
			);
			return scaledTexture != null;
		}

		private static ScaledTexture FetchScaledTexture (
			this Texture2D reference,
			int expectedScale,
			ref Rectangle source,
			bool create = false
		) {
			var newSource = source;

			try {
				if (!newSource.Cleanup(reference))
					return null;

				if (reference is RenderTarget2D || reference.Width < 1 || reference.Height < 1)
					return null;

				if (Math.Max(source.Width, source.Height) <= Config.Resample.MinimumTextureDimensions)
					return null;

				var scaledTexture = create ?
					ScaledTexture.Get(texture: reference, source: newSource, expectedScale: expectedScale) :
					ScaledTexture.Fetch(texture: reference, source: newSource, expectedScale: expectedScale);
				if (scaledTexture != null && scaledTexture.IsReady) {
					var t = scaledTexture.Texture;

					if (!t.Validate())
						return null;

					source = (Bounds)t.Dimensions;

					return scaledTexture;
				}
			}
			catch (Exception ex) {
				ex.PrintError();
			}

			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Validate(this ManagedTexture2D @this) {
			return @this != null && !@this.IsDisposed;
		}

		[Conditional("DEBUG"), MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Validate (this in Rectangle source, Texture2D reference) {
			if (source.Left < 0 || source.Top < 0 || source.Right >= reference.Width || source.Bottom >= reference.Height) {
				if (source.Right - reference.Width > 1 || source.Bottom - reference.Height > 1)
					Debug.WarningLn($"Out of range source '{source}' for texture '{reference.SafeName()}' ({reference.Width}, {reference.Height})");
			}
			if (source.Right < source.Left || source.Bottom < source.Top) {
				Debug.WarningLn($"Inverted range source '{source}' for texture '{reference.SafeName()}'");
			}
		}

		internal static bool OnDraw (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Vector2 position,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			ref Vector2 scale,
			SpriteEffects effects,
			ref float layerDepth
		) {
			texture.Meta().UpdateLastAccess();

			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));

			sourceRectangle.Validate(reference: texture);

			var expectedScale = Config.Resample.EnableDynamicScale ? (Math.Max(scale.X, scale.Y) + Config.Resample.ScaleBias).Clamp(2.0f, (float)Config.Resample.MaxScale).NextInt() : Config.Resample.MaxScale;

			ManagedTexture2D resampledTexture;
			if (texture.FetchScaledTexture(
				expectedScale: expectedScale,
				source: ref sourceRectangle,
				scaledTexture: out var scaledTexture,
				create: true
			)) {
				resampledTexture = scaledTexture.Texture;
			}
			else {
				resampledTexture = null;
			}

			if (scaledTexture == null) {
				return Continue;
			}

			scaledTexture.UpdateReferenceFrame();

			if (!resampledTexture.Validate()) {
				return Continue;
			}

			var adjustedScale = scale / scaledTexture.Scale;
			var adjustedPosition = position;
			var adjustedOrigin = origin;

			if (!scaledTexture.Padding.IsZero) {
				var textureSize = new Vector2(sourceRectangle.Width, sourceRectangle.Height);
				var innerSize = (Vector2)scaledTexture.UnpaddedSize;

				// This is the scale factor to bring the inner size to the draw size.
				var innerRatio = textureSize / innerSize;

				// Scale the... scale by the scale factor.
				adjustedScale *= innerRatio;

				adjustedOrigin *= scaledTexture.Scale;
				adjustedOrigin /= innerRatio;
				adjustedOrigin += (textureSize - innerSize) * 0.5f;
			}
			else {
				adjustedOrigin *= scaledTexture.Scale;
			}

			texture = resampledTexture;
			source = sourceRectangle;
			origin = adjustedOrigin;
			scale = adjustedScale;
			position = adjustedPosition;
			return Continue;
		}
	}
}
