using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
static class Draw {
	private const bool Continue = true;
	private const bool Stop = false;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool Cleanup(this ref Bounds sourceRectangle, Texture2D reference) {
		if (Config.ClampInvalidBounds) {
			sourceRectangle = sourceRectangle.ClampTo(new(reference.Width, reference.Height));
		}

		// Let's just skip potentially invalid draws since I have no idea what to do with them.
		return !sourceRectangle.Degenerate;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool FetchScaledTexture(
		this Texture2D reference,
		uint expectedScale,
		ref Bounds source,
		out ScaledTexture scaledTexture,
		bool create = false
	) {
		var invert = source.Invert;
		scaledTexture = reference.FetchScaledTexture(
			expectedScale: expectedScale,
			source: ref source,
			create: create
		);
		source.Invert = invert;
		return scaledTexture != null;
	}

	private static ScaledTexture FetchScaledTexture(
		this Texture2D reference,
		uint expectedScale,
		ref Bounds source,
		bool create = false
	) {
		var newSource = source;

		try {
			if (!newSource.Cleanup(reference))
				return null;

			if (reference.Width < 1 || reference.Height < 1)
				return null;

			if (reference.Extent().MaxOf <= Config.Resample.MinimumTextureDimensions)
				return null;

			var scaledTexture = create ?
				ScaledTexture.Get(texture: reference, source: newSource, expectedScale: expectedScale) :
				ScaledTexture.Fetch(texture: reference, source: newSource, expectedScale: expectedScale);
			if (scaledTexture != null && scaledTexture.IsReady) {
				var t = scaledTexture.Texture;

				if (!t.Validate())
					return null;

				source = t.Dimensions;

				return scaledTexture;
			}
		}
		catch (Exception ex) {
			ex.PrintError();
			ex.InnerException?.PrintError();
		}

		return null;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool Validate(this ManagedTexture2D @this) {
		return @this?.IsDisposed == false;
	}

	[Conditional("DEBUG"), MethodImpl(Runtime.MethodImpl.Hot)]
	private static void Validate(this in XNA.Rectangle sourceRect, Texture2D reference) {
		Bounds source = sourceRect;
		if (source.Left < 0 || source.Top < 0 || source.Right >= reference.Width || source.Bottom >= reference.Height) {
			if (source.Right - reference.Width > 1 || source.Bottom - reference.Height > 1)
				Debug.WarningLn($"Out of range source '{source}' for texture '{reference.SafeName()}' ({reference.Width}, {reference.Height})");
		}
		if (source.Right < source.Left || source.Bottom < source.Top) {
			Debug.WarningLn($"Inverted range source '{source}' for texture '{reference.SafeName()}'");
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void GetDrawParameters(Texture2D texture, in XNA.Rectangle? source, out Bounds bounds, out float scaleFactor) {
		texture.Meta().UpdateLastAccess();
		var sourceRectangle = source.GetValueOrDefault(new(0, 0, texture.Width, texture.Height));

		scaleFactor = 1.0f;
		if (SpriteOverrides.IsWater(sourceRectangle, texture)) {
			if (Config.Resample.TrimWater) {
				scaleFactor = 4.0f;
			}
		}

		sourceRectangle.Validate(reference: texture);
		bounds = sourceRectangle;
	}

	// Takes the arguments, and checks to see if the texture is padded. If it is, it is forwarded to the correct draw call, avoiding
	// intervening mods altering the arguments first.
	internal static bool OnDrawFirst(
		this SpriteBatch @this,
		ref Texture2D texture,
		ref XNA.Rectangle destination,
		ref XNA.Rectangle? source,
		XNA.Color color,
		float rotation,
		ref XNA.Vector2 origin,
		ref SpriteEffects effects,
		float layerDepth,
		ref ManagedTexture2D __state
	) {
		using var _ = Performance.Track();

		if (destination.Width < 0 || destination.Height < 0) {
			Debug.Trace("destination invert");
		}
		if (source.HasValue && (source.Value.Width < 0 || source.Value.Height < 0)) {
			Debug.Trace("source invert");
		}

		GetDrawParameters(
			texture: texture,
			source: source,
			bounds: out var sourceRectangle,
			scaleFactor: out var scaleFactor
		);

		var referenceRectangle = sourceRectangle;

		Bounds destinationBounds = destination;

		var expectedScale2D = new Vector2F(destinationBounds.Extent) / new Vector2F(sourceRectangle.Extent);
		var expectedScale = EstimateScale(expectedScale2D, scaleFactor);

		if (!texture.FetchScaledTexture(
			expectedScale: expectedScale,
			source: ref sourceRectangle,
			scaledTexture: out var scaledTexture,
			create: true
		)) {
			return Continue;
		}
		scaledTexture.UpdateReferenceFrame();

		var resampledTexture = scaledTexture.Texture;
		if (!resampledTexture.Validate()) {
			return Continue;
		}

		if (!scaledTexture.Padding.IsZero) {
			// Convert the draw into the other draw style. This has to be done because the padding potentially has
			// subpixel accuracy when scaled to the destination rectangle.

			var originalSize = new Vector2F(referenceRectangle.Extent);
			var destinationSize = new Vector2F(destinationBounds.Extent);
			var newScale = destinationSize / originalSize;
			var newPosition = new Vector2F(destinationBounds.X, destinationBounds.Y);

			if (destinationBounds.Invert.X) {
				effects ^= SpriteEffects.FlipHorizontally;
			}
			if (destinationBounds.Invert.Y) {
				effects ^= SpriteEffects.FlipVertically;
			}

			// TODO handle culling here for inverted sprites?

			@this.Draw(
				texture: resampledTexture,
				position: newPosition,
				sourceRectangle: sourceRectangle,
				color: color,
				rotation: rotation,
				origin: origin,
				scale: newScale,
				effects: effects,
				layerDepth: layerDepth
			);
			return Stop;
		}
		__state = resampledTexture;
		return Continue;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool OnDraw(
		this SpriteBatch @this,
		ref Texture2D texture,
		ref XNA.Rectangle destination,
		ref XNA.Rectangle? source,
		XNA.Color color,
		float rotation,
		ref XNA.Vector2 origin,
		ref SpriteEffects effects,
		ref float layerDepth,
		ref ManagedTexture2D __state
	) {
		using var _ = Performance.Track("OnDraw0");

		Bounds sourceRectangle;
		ScaledTexture scaledTexture;
		ManagedTexture2D resampledTexture;

		Bounds destinationBounds = destination;

		if (__state == null) {
			GetDrawParameters(
				texture: texture,
				source: source,
				bounds: out sourceRectangle,
				scaleFactor: out var scaleFactor
			);

			var expectedScale2D = new Vector2F(destinationBounds.Extent) / new Vector2F(sourceRectangle.Extent);
			var expectedScale = EstimateScale(expectedScale2D, scaleFactor);

			if (!texture.FetchScaledTexture(
				expectedScale: expectedScale,
				source: ref sourceRectangle,
				scaledTexture: out scaledTexture
			)) {
				return Continue;
			}
			scaledTexture.UpdateReferenceFrame();

			resampledTexture = scaledTexture.Texture;
			if (!resampledTexture.Validate()) {
				return Continue;
			}
		}
		else {
			resampledTexture = __state;
			scaledTexture = resampledTexture.Texture;
			sourceRectangle = resampledTexture.Dimensions;
		}

		var scaledOrigin = origin / scaledTexture.Scale;

		if (source.HasValue) {
			sourceRectangle.Invert.X = (source.Value.Width < 0);
			sourceRectangle.Invert.Y = (source.Value.Height < 0);
		}

		source = sourceRectangle;
		origin = scaledOrigin;
		texture = resampledTexture;

		return Continue;
	}

	internal static uint EstimateScale(Vector2F scale, float scaleFactor) {
		float factoredScale = scale.MaxOf * scaleFactor;
		factoredScale += Config.Resample.ScaleBias;
		factoredScale = factoredScale.Clamp(2.0f, (float)Config.Resample.MaxScale);
		uint factoredScaleN = (uint)factoredScale.NextInt();
		return Resample.Scalers.xBRZ.Scaler.ClampScale(factoredScaleN);
	}

	internal static bool OnDraw(
		this SpriteBatch @this,
		ref Texture2D texture,
		ref XNA.Vector2 position,
		ref XNA.Rectangle? source,
		XNA.Color color,
		float rotation,
		ref XNA.Vector2 origin,
		ref XNA.Vector2 scale,
		SpriteEffects effects,
		ref float layerDepth
	) {
		using var _ = Performance.Track("OnDraw1");

		GetDrawParameters(
			texture: texture,
			source: source,
			bounds: out var sourceRectangle,
			scaleFactor: out var scaleFactor
		);

		ScaledTexture scaledTexture;
		if (texture is ManagedTexture2D resampledTexture) {
			scaledTexture = resampledTexture.Texture;
			sourceRectangle = resampledTexture.Dimensions;
		}
		else if (texture.FetchScaledTexture(
			expectedScale: EstimateScale(scale, scaleFactor),
			source: ref sourceRectangle,
			scaledTexture: out scaledTexture,
			create: true
		)) {
			scaledTexture.UpdateReferenceFrame();
			resampledTexture = scaledTexture.Texture;

			if (!resampledTexture.Validate()) {
				return Continue;
			}
		}
		else {
			resampledTexture = null;
		}

		if (scaledTexture == null) {
			return Continue;
		}

		var adjustedScale = (Vector2F)scale / (Vector2F)scaledTexture.Scale;
		var adjustedPosition = position;
		var adjustedOrigin = (Vector2F)origin;

		if (!scaledTexture.Padding.IsZero) {
			var textureSize = new Vector2F(sourceRectangle.Extent);
			var innerSize = (Vector2F)scaledTexture.UnpaddedSize;

			// This is the scale factor to bring the inner size to the draw size.
			var innerRatio = textureSize / innerSize;

			// Scale the... scale by the scale factor.
			adjustedScale *= innerRatio;

			adjustedOrigin *= (Vector2F)scaledTexture.Scale;
			adjustedOrigin /= innerRatio;
			adjustedOrigin += (textureSize - innerSize) * 0.5f;
		}
		else {
			adjustedOrigin *= (Vector2F)scaledTexture.Scale;
		}

		if (source.HasValue) {
			sourceRectangle.Invert.X = (source.Value.Width < 0);
			sourceRectangle.Invert.Y = (source.Value.Height < 0);
		}

		texture = resampledTexture;
		source = sourceRectangle;
		origin = adjustedOrigin;
		scale = adjustedScale;
		position = adjustedPosition;
		return Continue;
	}
}
