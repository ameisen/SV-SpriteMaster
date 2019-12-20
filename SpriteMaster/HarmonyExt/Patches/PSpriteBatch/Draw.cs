using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using System;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	static class Draw {
		private static bool Cleanup (this ref Rectangle sourceRectangle, Texture2D reference) {
			if (Config.ClampInvalidBounds) {
				sourceRectangle = sourceRectangle.ClampTo(new Rectangle(0, 0, reference.Width, reference.Height));
			}

			// Let's just skip potentially invalid draws since I have no idea what to do with them.
			return (sourceRectangle.Height > 0 && sourceRectangle.Width > 0);
		}

		private static ScaledTexture FetchScaledTexture (
			this SpriteBatch @this,
			Texture2D reference,
			ref Rectangle sourceRectangle,
			bool create = false
		) {
			try {
				if (!sourceRectangle.Cleanup(reference))
					return null;

				if (reference is RenderTarget2D || reference.Width < 1 || reference.Height < 1)
					return null;

				var scaledTexture = create ?
					ScaledTexture.Get(reference, sourceRectangle, sourceRectangle) :
					ScaledTexture.Fetch(reference, sourceRectangle, sourceRectangle);
				if (scaledTexture != null && scaledTexture.IsReady) {
					var t = scaledTexture.Texture;

					if (t == null)
						return null;

					if (Config.Resample.DeSprite && scaledTexture.IsSprite) {
						sourceRectangle = new Rectangle(
							0,
							0,
							t.Width,
							t.Height
						);
					}
					else {
						sourceRectangle = new Rectangle(
							(sourceRectangle.X * scaledTexture.Scale.X).ToCoordinate(),
							(sourceRectangle.Y * scaledTexture.Scale.Y).ToCoordinate(),
							(sourceRectangle.Width * scaledTexture.Scale.X).ToCoordinate(),
							(sourceRectangle.Height * scaledTexture.Scale.Y).ToCoordinate()
						);
					}

					return scaledTexture;
				}
			}
			catch (Exception ex) {
				ex.PrintError();
			}

			return null;
		}

		private static ScaledTexture GetScaledTexture (
			this SpriteBatch @this,
			Texture2D reference,
			ref Rectangle sourceRectangle
		) {
			var scaledTexture = @this.FetchScaledTexture(reference, ref sourceRectangle, create: true);
			if (scaledTexture != null) {
				return scaledTexture;
			}

			return null;
		}

		// Takes the arguments, and checks to see if the texture is padded. If it is, it is forwarded to the correct draw call, avoiding
		// intervening mods altering the arguments first.
		internal static bool OnDrawFirst (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		) {
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			var originalSourceRectangle = sourceRectangle;

			var scaledTexture = GetScaledTexture(@this, texture, ref sourceRectangle);
			if (scaledTexture == null) {
				return true;
			}

			if (!scaledTexture.Padding.IsZero) {
				// Convert the draw into the other draw style. This has to be done because the padding potentially has
				// subpixel accuracy when scaled to the destination rectangle.

				var originalSize = new Vector2(originalSourceRectangle.Width, originalSourceRectangle.Height);
				var destinationSize = new Vector2(destination.Width, destination.Height);
				var newScale = destinationSize / originalSize;
				var newPosition = new Vector2(destination.X, destination.Y);

				@this.Draw(
					texture: texture,
					position: newPosition,
					sourceRectangle: source,
					color: color,
					rotation: rotation,
					origin: origin,
					scale: newScale,
					effects: effects,
					layerDepth: layerDepth
				);
				return false;
			}
			return true;
		}

		internal static bool OnDraw (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		) {
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));

			var scaledTexture = FetchScaledTexture(@this, texture, ref sourceRectangle);
			if (scaledTexture == null) {
				return true;
			}

			var t = scaledTexture.Texture;

			var scaledOrigin = origin / scaledTexture.Scale;

			source = sourceRectangle;
			origin = scaledOrigin;
			texture = t;

			return true;
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
			float layerDepth
		) {
			// TODO : We need to intgrate the origin into the bounds so we can properly hash-sprite these calls.

			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			bool allowPadding = true;

			ScaledTexture scaledTexture;
			Texture2D t;
			if (texture is ScaledTexture.ManagedTexture2D managedTexture) {
				scaledTexture = managedTexture.Texture;
				t = texture;
			}
			else {
				scaledTexture = FetchScaledTexture(@this, texture, ref sourceRectangle, allowPadding);
				if (scaledTexture == null) {
					return true;
				}
				t = scaledTexture.Texture;
			}

			var adjustedScale = scale / scaledTexture.Scale;
			var adjustedPosition = position;
			var adjustedOrigin = origin;

			if (!scaledTexture.Padding.IsZero) {
				var textureSize = new Vector2(t.Width, t.Height);
				var innerSize = new Vector2(scaledTexture.UnpaddedSize.Width, scaledTexture.UnpaddedSize.Height);

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

			texture = t;
			source = sourceRectangle;
			origin = adjustedOrigin;
			scale = adjustedScale;
			position = adjustedPosition;
			return true;
		}
	}
}
