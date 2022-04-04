using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers.EPX;

sealed partial class Scaler {
	private const uint MinScale = 2;
	private const uint MaxScale = Config.MaxScale;

	private static uint ClampScale(uint scale) => Math.Clamp(scale, MinScale, MaxScale);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static Span<Color16> Apply(
		Config? config,
		uint scaleMultiplier,
		ReadOnlySpan<Color16> sourceData,
		Vector2I sourceSize,
		Span<Color16> targetData,
		Vector2I targetSize
	) {
		if (config is null) {
			throw new ArgumentNullException(nameof(config));
		}

		if (sourceSize.X * sourceSize.Y > sourceData.Length) {
			throw new ArgumentOutOfRangeException(nameof(sourceData));
		}

		var targetSizeCalculated = sourceSize * scaleMultiplier;
		if (targetSize != targetSizeCalculated) {
			throw new ArgumentOutOfRangeException(nameof(targetSize));
		}

		if (targetData == Span<Color16>.Empty) {
			targetData = SpanExt.MakeUninitialized<Color16>(targetSize.Area);
		}
		else {
			if (targetSize.Area > targetData.Length) {
				throw new ArgumentOutOfRangeException(nameof(targetData));
			}
		}

		var scalerInstance = new Scaler(
			configuration: in config,
			scaleMultiplier: scaleMultiplier,
			sourceSize: sourceSize,
			targetSize: targetSize
		);

		scalerInstance.Scale(sourceData, targetData);
		return targetData;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private Scaler(
		in Config configuration,
		uint scaleMultiplier,
		Vector2I sourceSize,
		Vector2I targetSize
	) {
		if (scaleMultiplier < MinScale || scaleMultiplier > MaxScale) {
			throw new ArgumentOutOfRangeException(nameof(scaleMultiplier));
		}
		/*
		if (sourceData is null) {
			throw new ArgumentNullException(nameof(sourceData));
		}
		if (targetData is null) {
			throw new ArgumentNullException(nameof(targetData));
		}
		*/
		if (sourceSize.X <= 0 || sourceSize.Y <= 0) {
			throw new ArgumentOutOfRangeException(nameof(sourceSize));
		}

		ScaleMultiplier = scaleMultiplier;
		Configuration = configuration;
		SourceSize = sourceSize;
		TargetSize = targetSize;
	}

	private readonly uint ScaleMultiplier;
	private readonly Config Configuration;

	private readonly Vector2I SourceSize;
	private readonly Vector2I TargetSize;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private int GetX(int x) {
		if (Configuration.Wrapped.X) {
			x = (x + SourceSize.Width) % SourceSize.Width;
		}
		else {
			x = Math.Clamp(x, 0, SourceSize.Width - 1);
		}
		return x;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private int GetY(int y) {
		if (Configuration.Wrapped.Y) {
			y = (y + SourceSize.Height) % SourceSize.Height;
		}
		else {
			y = Math.Clamp(y, 0, SourceSize.Height - 1);
		}
		return y;
	}

	// https://en.wikipedia.org/wiki/Pixel-art_scaling_algorithms#EPX/Scale2%C3%97/AdvMAME2%C3%97
	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void Scale(ReadOnlySpan<Color16> source, Span<Color16> destination) {
		var last = SourceSize;

		if (last.Y <= 0 || last.X <= 0) {
			return;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		static Color16 GetPixel(ReadOnlySpan<Color16> src, int stride, int offset) {
			// We can try embedded a distance calculation as well. Perhaps instead of a negative stride/offset, we provide a 
			// negative distance from the edge and just recalculate the stride/offset in that case.
			// We can scale the alpha reduction by the distance to hopefully correct the edges.

			// Alternatively, for non-wrapping textures (or for wrapping ones that only have one wrapped axis) we embed them in a new target
			// which is padded with alpha, and after resampling we manually clamp the colors on it. This will give a normal-ish effect for drawing, and will make it so we might get a more correct edge since it can overdraw.
			// If we do this, we draw the entire texture, with the padding, but we slightly enlarge the target area for _drawing_ to account for the extra padding.
			// This will effectively cause a filtering effect and hopefully prevent the hard edge problems

			if (stride < 0) {
				Debug.Warning($"EPX GetPixel out of range: stride: {stride}, value clamped");
				stride = Math.Max(0, stride);
			}

			if (offset < 0) {
				Debug.Warning($"EPX GetPixel out of range: offset: {offset}, value clamped");
				offset = Math.Max(0, offset);
			}

			return src[stride + offset];
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		static void SetPixel(Span<Color16> dst, int stride, int offset, in Color16 color) {
			// We can try embedded a distance calculation as well. Perhaps instead of a negative stride/offset, we provide a 
			// negative distance from the edge and just recalculate the stride/offset in that case.
			// We can scale the alpha reduction by the distance to hopefully correct the edges.

			// Alternatively, for non-wrapping textures (or for wrapping ones that only have one wrapped axis) we embed them in a new target
			// which is padded with alpha, and after resampling we manually clamp the colors on it. This will give a normal-ish effect for drawing, and will make it so we might get a more correct edge since it can overdraw.
			// If we do this, we draw the entire texture, with the padding, but we slightly enlarge the target area for _drawing_ to account for the extra padding.
			// This will effectively cause a filtering effect and hopefully prevent the hard edge problems

			if (stride < 0) {
				Debug.Warning($"EPX SetPixel out of range: stride: {stride}, value clamped");
				stride = Math.Max(0, stride);
			}

			if (offset < 0) {
				Debug.Warning($"EPX SetPixel out of range: offset: {offset}, value clamped");
				offset = Math.Max(0, offset);
			}

			dst[stride + offset] = color;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		static bool ColorEq(in Color16 a, in Color16 b) {
			if (a.A != b.A) {
				return false;
			}

			if (a.NoAlpha != b.NoAlpha) {
				return false;
			}

			return true;
		}

		for (int y = 0; y < last.Y; ++y) {
			var yM1 = SourceSize.X * GetY(y - 1);
			var y0 =  SourceSize.X * GetY(y);
			var yP1 = SourceSize.X * GetY(y + 1);

			var yo12 = TargetSize.X * (y * 2);
			var yo34 = TargetSize.X * ((y * 2) + 1);

			for (int x = 0; x < last.X; ++x) {
				var xM1 = GetX(x - 1);
				var x0 =  GetX(x);
				var xP1 = GetX(x + 1);

				var A = GetPixel(source, yM1, x0);
				var C = GetPixel(source, y0,  xM1);
				var P = GetPixel(source, y0,  x0);
				var B = GetPixel(source, y0,  xP1);
				var D = GetPixel(source, yP1, x0);

				var o1 = P;
				var o2 = P;
				var o3 = P;
				var o4 = P;

				if (C == A && C != D && A != B) {
					//if (A.A <= P.A) {
						o1 = A;
					//}
				}
				if (A == B && A != C && B != D) {
					//if (B.A <= P.A) {
						o2 = B;
					//}
				}
				if (D == C && D != B && C != A) {
					//if (C.A <= P.A) {
						o3 = C;
					//}
				}
				if (B == D && B != A && D != C) {
					//if (D.A <= P.A) {
						o4 = D;
					//}
				}

				var x13 = x * 2;
				var x24 = x * 2 + 1;

				SetPixel(destination, yo12, x13, in o1);
				SetPixel(destination, yo12, x24, in o2);
				SetPixel(destination, yo34, x13, in o3);
				SetPixel(destination, yo34, x24, in o4);
			}
		}
	}
}
