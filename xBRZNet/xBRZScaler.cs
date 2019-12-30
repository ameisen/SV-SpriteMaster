﻿using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using xBRZNet2.Blend;
using xBRZNet2.Color;
using xBRZNet2.Common;
using xBRZNet2.Scalers;

namespace xBRZNet2 {
	//http://intrepidis.blogspot.com/2014/02/xbrz-in-java.html
	/*
			-------------------------------------------------------------------------
			| xBRZ: "Scale by rules" - high quality image upscaling filter by Zenju |
			-------------------------------------------------------------------------
			using a modified approach of xBR:
			http://board.byuu.org/viewtopic.php?f=10&t=2248
			- new rule set preserving small image features
			- support multithreading
			- support 64 bit architectures
			- support processing image slices
	*/

	/*
			-> map source (srcWidth * srcHeight) to target (scale * width x scale * height)
			image, optionally processing a half-open slice of rows [yFirst, yLast) only
			-> color format: ARGB (BGRA char order), alpha channel unused
			-> support for source/target pitch in chars!
			-> if your emulator changes only a few image slices during each cycle
			(e.g. Dosbox) then there's no need to run xBRZ on the complete image:
			Just make sure you enlarge the source image slice by 2 rows on top and
			2 on bottom (this is the additional range the xBRZ algorithm is using
			during analysis)
			Caveat: If there are multiple changed slices, make sure they do not overlap
			after adding these additional rows in order to avoid a memory race condition 
			if you are using multiple threads for processing each enlarged slice!

			THREAD-SAFETY: - parts of the same image may be scaled by multiple threads
			as long as the [yFirst, yLast) ranges do not overlap!
			- there is a minor inefficiency for the first row of a slice, so avoid
			processing single rows only
			*/

	/*
			Converted to Java 7 by intrepidis. It would have been nice to use
			Java 8 lambdas, but Java 7 is more ubiquitous at the time of writing,
			so this code uses anonymous local classes instead.
			Regarding multithreading, each thread should have its own instance
			of the xBRZ class.
	*/

	// ReSharper disable once InconsistentNaming
	public sealed class xBRZScaler {
		// scaleSize = 2 to 6

		public xBRZScaler (
			int scaleMultiplier,
			Span<int> sourceData,
			int sourceWidth,
			int sourceHeight,
			in Rectangle? sourceTarget,
			int[] targetData,
			in Config configuration
		) {
			if (scaleMultiplier < 2 || scaleMultiplier > 6) {
				throw new ArgumentOutOfRangeException(nameof(scaleMultiplier));
			}
			if (sourceData == null) {
				throw new ArgumentNullException(nameof(sourceData));
			}
			if (targetData == null) {
				throw new ArgumentNullException(nameof(targetData));
			}
			if (sourceWidth <= 0) {
				throw new ArgumentOutOfRangeException(nameof(sourceWidth));
			}
			if (sourceHeight <= 0) {
				throw new ArgumentOutOfRangeException(nameof(sourceHeight));
			}
			if (sourceWidth * sourceHeight > sourceData.Length) {
				throw new ArgumentOutOfRangeException(nameof(sourceData));
			}
			this.sourceTarget = sourceTarget.GetValueOrDefault(new Rectangle(0, 0, sourceWidth, sourceHeight));
			if (this.sourceTarget.Right > sourceWidth || this.sourceTarget.Bottom > sourceHeight) {
				throw new ArgumentOutOfRangeException(nameof(sourceTarget));
			}
			this.targetWidth = this.sourceTarget.Width * scaleMultiplier;
			this.targetHeight = this.sourceTarget.Height * scaleMultiplier;
			if (targetWidth * targetHeight > targetData.Length) {
				throw new ArgumentOutOfRangeException(nameof(targetData));
			}

			this.scaler = scaleMultiplier.ToIScaler();
			this.configuration = configuration;
			this.ColorDistance = new ColorDist(this.configuration);
			this.ColorEqualizer = new ColorEq(this.configuration);
			this.sourceWidth = sourceWidth;
			this.sourceHeight = sourceHeight;
			Scale(sourceData, targetData);
		}

		private readonly Config configuration;
		private readonly IScaler scaler;
		private OutputMatrix outputMatrix;
		private BlendResult blendResult = new BlendResult();

		private readonly ColorDist ColorDistance;
		private readonly ColorEq ColorEqualizer;

		private readonly int sourceWidth;
		private readonly int sourceHeight;
		private readonly Rectangle sourceTarget;
		private readonly int targetWidth;
		private readonly int targetHeight;

		//fill block with the given color
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void FillBlock (int[] trg, int trgi, int pitch, int col, int blockSize) {
			for (var y = 0; y < blockSize; ++y, trgi += pitch) {
				for (var x = 0; x < blockSize; ++x) {
					trg[trgi + x] = col;
				}
			}
		}

		//detect blend direction
		private void PreProcessCorners (in Kernel4x4 ker) {
			blendResult.Reset();

			if ((ker.F == ker.G && ker.J == ker.K) || (ker.F == ker.J && ker.G == ker.K))
				return;

			var dist = ColorDistance;

			const int weight = 4;
			var jg = dist.DistYCbCr(ker.I, ker.F) + dist.DistYCbCr(ker.F, ker.C) + dist.DistYCbCr(ker.N, ker.K) + dist.DistYCbCr(ker.K, ker.H) + weight * dist.DistYCbCr(ker.J, ker.G);
			var fk = dist.DistYCbCr(ker.E, ker.J) + dist.DistYCbCr(ker.J, ker.O) + dist.DistYCbCr(ker.B, ker.G) + dist.DistYCbCr(ker.G, ker.L) + weight * dist.DistYCbCr(ker.F, ker.K);

			if (jg < fk) {
				var dominantGradient = (char)((configuration.DominantDirectionThreshold * jg < fk) ? BlendType.Dominant : BlendType.Normal);
				if (ker.F != ker.G && ker.F != ker.J) {
					blendResult.F = dominantGradient;
				}
				if (ker.K != ker.J && ker.K != ker.G) {
					blendResult.K = dominantGradient;
				}
			}
			else if (fk < jg) {
				var dominantGradient = (char)((configuration.DominantDirectionThreshold * fk < jg) ? BlendType.Dominant : BlendType.Normal);
				if (ker.J != ker.F && ker.J != ker.K) {
					blendResult.J = dominantGradient;
				}
				if (ker.G != ker.F && ker.G != ker.K) {
					blendResult.G = dominantGradient;
				}
			}
		}

		/*
				input kernel area naming convention:
				-------------
				| A | B | C |
				----|---|---|
				| D | E | F | //input pixel is at position E
				----|---|---|
				| G | H | I |
				-------------
				blendInfo: result of preprocessing all four corners of pixel "e"
		*/
		private unsafe void ScalePixel (IScaler scaler, int rotDeg, in Kernel3x3 ker, int trgi, char blendInfo) {
			var blend = blendInfo.Rotate((RotationDegree)rotDeg);

			if ((BlendType)blend.GetBottomR() == BlendType.None)
				return;

			// int a = ker._[Rot._[(0 << 2) + rotDeg]];
			var b = ker._[Rotator._[(1 << 2) + rotDeg]];
			var c = ker._[Rotator._[(2 << 2) + rotDeg]];
			var d = ker._[Rotator._[(3 << 2) + rotDeg]];
			var e = ker._[Rotator._[(4 << 2) + rotDeg]];
			var f = ker._[Rotator._[(5 << 2) + rotDeg]];
			var g = ker._[Rotator._[(6 << 2) + rotDeg]];
			var h = ker._[Rotator._[(7 << 2) + rotDeg]];
			var i = ker._[Rotator._[(8 << 2) + rotDeg]];

			var eq = ColorEqualizer;
			var dist = ColorDistance;

			bool doLineBlend;

			if (blend.GetBottomR() >= (char)BlendType.Dominant) {
				doLineBlend = true;
			}
			//make sure there is no second blending in an adjacent
			//rotation for this pixel: handles insular pixels, mario eyes
			//but support double-blending for 90� corners
			else if (blend.GetTopR() != (char)BlendType.None && !eq.IsColorEqual(e, g)) {
				doLineBlend = false;
			}
			else if (blend.GetBottomL() != (char)BlendType.None && !eq.IsColorEqual(e, c)) {
				doLineBlend = false;
			}
			//no full blending for L-shapes; blend corner only (handles "mario mushroom eyes")
			else if (eq.IsColorEqual(g, h) && eq.IsColorEqual(h, i) && eq.IsColorEqual(i, f) && eq.IsColorEqual(f, c) && !eq.IsColorEqual(e, i)) {
				doLineBlend = false;
			}
			else {
				doLineBlend = true;
			}

			//choose most similar color
			var px = dist.DistYCbCr(e, f) <= dist.DistYCbCr(e, h) ? f : h;

			var out_ = outputMatrix;
			out_.Move(rotDeg, trgi);

			if (!doLineBlend) {
				scaler.BlendCorner(px, out_);
				return;
			}

			//test sample: 70% of values max(fg, hc) / min(fg, hc)
			//are between 1.1 and 3.7 with median being 1.9
			var fg = dist.DistYCbCr(f, g);
			var hc = dist.DistYCbCr(h, c);

			var haveShallowLine = configuration.SteepDirectionThreshold * fg <= hc && e != g && d != g;
			var haveSteepLine = configuration.SteepDirectionThreshold * hc <= fg && e != c && b != c;

			if (haveShallowLine) {
				if (haveSteepLine) {
					scaler.BlendLineSteepAndShallow(px, out_);
				}
				else {
					scaler.BlendLineShallow(px, out_);
				}
			}
			else {
				if (haveSteepLine) {
					scaler.BlendLineSteep(px, out_);
				}
				else {
					scaler.BlendLineDiagonal(px, out_);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int clampX (int x) {
			x -= sourceTarget.Left;
			if (configuration.WrappedX) {
				x = (x + sourceTarget.Width) % sourceTarget.Width;
			}
			else {
				x = Math.Min(Math.Max(x, 0), sourceTarget.Width - 1);
			}
			return x + sourceTarget.Left;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int clampY (int y) {
			y -= sourceTarget.Top;
			if (configuration.WrappedY) {
				y = (y + sourceTarget.Height) % sourceTarget.Height;
			}
			else {
				y = Math.Min(Math.Max(y, 0), sourceTarget.Height - 1);
			}
			return y + sourceTarget.Top;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool legalX (int x) {
			return true;
			if (configuration.WrappedX) {
				return true;
			}
			return x >= sourceTarget.Left && x < sourceTarget.Right;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool legalY (int y) {
			return true;
			if (configuration.WrappedY) {
				return true;
			}
			return y >= sourceTarget.Top && y < sourceTarget.Bottom;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int getX (int x) {
			return legalX(x) ? clampX(x) : -clampX(x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int getY (int y) {
			return legalY(y) ? clampY(y) : -clampY(y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Mask (int value, uint mask) {
			return unchecked((int)((uint)value & mask));
		}

		//scaler policy: see "Scaler2x" reference implementation
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe void Scale (Span<int> src, int[] trg) {
			int yFirst = sourceTarget.Top;
			int yLast = sourceTarget.Bottom;

			if (yFirst >= yLast)
				return;

			var trgWidth = targetWidth;

			//temporary buffer for "on the fly preprocessing"
			var preProcBuffer = stackalloc char[sourceTarget.Width];

			var ker4 = new Kernel4x4();

			int GetPixel (in Span<int> src, int stride, int offset) {
				// We can try embedded a distance calculation as well. Perhaps instead of a negative stride/offset, we provide a 
				// negative distance from the edge and just recalculate the stride/offset in that case.
				// We can scale the alpha reduction by the distance to hopefully correct the edges.

				// Alternatively, for non-wrapping textures (or for wrapping ones that only have one wrapped axis) we embed them in a new target
				// which is padded with alpha, and after resampling we manually clamp the colors on it. This will give a normal-ish effect for drawing, and will make it so we might get a more correct edge since it can overdraw.
				// If we do this, we draw the entire texture, with the padding, but we slightly enlarge the target area for _drawing_ to account for the extra padding.
				// This will effectively cause a filtering effect and hopefully prevent the hard edge problems

				if (stride >= 0 && offset >= 0)
					return src[stride + offset];
				stride = (stride < 0) ? -stride : stride;
				offset = (offset < 0) ? -offset : offset;
				int sample = src[stride + offset];
				const uint mask = 0x00_FF_FF_FFU;
				return Mask(sample, mask);
			}

			//initialize preprocessing buffer for first row:
			//detect upper left and right corner blending
			//this cannot be optimized for adjacent processing
			//stripes; we must not allow for a memory race condition!
			if (yFirst > 0) {
				var y = yFirst - 1;

				var sM1 = sourceWidth * getY(y - 1);
				var s0 = sourceWidth * y; //center line
				var sP1 = sourceWidth * getY(y + 1);
				var sP2 = sourceWidth * getY(y + 2);

				for (var x = sourceTarget.Left; x < sourceTarget.Right; ++x) {
					var xM1 = getX(x - 1);
					var xP1 = getX(x + 1);
					var xP2 = getX(x + 2);

					//read sequentially from memory as far as possible
					ker4.A = GetPixel(src, sM1, xM1);
					ker4.B = GetPixel(src, sM1, x);
					ker4.C = GetPixel(src, sM1, xP1);
					ker4.D = GetPixel(src, sM1, xP2);

					ker4.E = GetPixel(src, s0, xM1);
					ker4.F = src[s0 + x];
					ker4.G = GetPixel(src, s0, xP1);
					ker4.H = GetPixel(src, s0, xP2);

					ker4.I = GetPixel(src, sP1, xM1);
					ker4.J = GetPixel(src, sP1, x);
					ker4.K = GetPixel(src, sP1, xP1);
					ker4.L = GetPixel(src, sP1, xP2);

					ker4.M = GetPixel(src, sP2, xM1);
					ker4.N = GetPixel(src, sP2, x);
					ker4.O = GetPixel(src, sP2, xP1);
					ker4.P = GetPixel(src, sP2, xP2);

					PreProcessCorners(ker4); // writes to blendResult
					/*
					preprocessing blend result:
					---------
					| F | G | //evalute corner between F, G, J, K
					----|---| //input pixel is at position F
					| J | K |
					---------
					*/

					int adjustedX = x - sourceTarget.Left;

					preProcBuffer[adjustedX] = preProcBuffer[adjustedX].SetTopR(blendResult.J);

					if (x + 1 < sourceTarget.Right) {
						preProcBuffer[adjustedX + 1] = preProcBuffer[adjustedX + 1].SetTopL(blendResult.K);
					}
					else if (configuration.WrappedX) {
						preProcBuffer[0] = preProcBuffer[0].SetTopL(blendResult.K);
					}
				}
			}

			outputMatrix = new OutputMatrix(scaler.Scale, trg, trgWidth);

			var ker3 = new Kernel3x3();

			for (var y = yFirst; y < yLast; ++y) {
				//consider MT "striped" access
				var trgi = scaler.Scale * (y - yFirst) * trgWidth;

				var sM1 = sourceWidth * getY(y - 1);
				var s0 = sourceWidth * y; //center line
				var sP1 = sourceWidth * getY(y + 1);
				var sP2 = sourceWidth * getY(y + 2);

				var blendXy1 = (char)0;

				for (var x = sourceTarget.Left; x < sourceTarget.Right; ++x, trgi += scaler.Scale) {
					var xM1 = getX(x - 1);
					var xP1 = getX(x + 1);
					var xP2 = getX(x + 2);

					//evaluate the four corners on bottom-right of current pixel
					//blend_xy for current (x, y) position

					//read sequentially from memory as far as possible
					ker4.A = GetPixel(src, sM1, xM1);
					ker4.B = GetPixel(src, sM1, x);
					ker4.C = GetPixel(src, sM1, xP1);
					ker4.D = GetPixel(src, sM1, xP2);

					ker4.E = GetPixel(src, s0, xM1);
					ker4.F = src[s0 + x];
					ker4.G = GetPixel(src, s0, xP1);
					ker4.H = GetPixel(src, s0, xP2);

					ker4.I = GetPixel(src, sP1, xM1);
					ker4.J = GetPixel(src, sP1, x);
					ker4.K = GetPixel(src, sP1, xP1);
					ker4.L = GetPixel(src, sP1, xP2);

					ker4.M = GetPixel(src, sP2, xM1);
					ker4.N = GetPixel(src, sP2, x);
					ker4.O = GetPixel(src, sP2, xP1);
					ker4.P = GetPixel(src, sP2, xP2);

					PreProcessCorners(ker4); // writes to blendResult

					int adjustedX = x - sourceTarget.Left;

					/*
							preprocessing blend result:
							---------
							| F | G | //evaluate corner between F, G, J, K
							----|---| //current input pixel is at position F
							| J | K |
							---------
					*/

					//all four corners of (x, y) have been determined at
					//this point due to processing sequence!
					var blendXy = preProcBuffer[adjustedX].SetBottomR(blendResult.F);

					//set 2nd known corner for (x, y + 1)
					blendXy1 = blendXy1.SetTopR(blendResult.J);
					//store on current buffer position for use on next row
					preProcBuffer[adjustedX] = blendXy1;

					//set 1st known corner for (x + 1, y + 1) and
					//buffer for use on next column
					blendXy1 = ((char)0).SetTopL(blendResult.K);

					if (x + 1 < sourceTarget.Right) {
						//set 3rd known corner for (x + 1, y)
						preProcBuffer[adjustedX + 1] = preProcBuffer[adjustedX + 1].SetBottomL(blendResult.G);
					}
					else if (configuration.WrappedX) {
						preProcBuffer[0] = preProcBuffer[0].SetBottomL(blendResult.G);
					}

					//fill block of size scale * scale with the given color
					//  //place *after* preprocessing step, to not overwrite the
					//  //results while processing the the last pixel!
					FillBlock(trg, trgi, trgWidth, src[s0 + x], scaler.Scale);

					//blend four corners of current pixel
					if (blendXy == 0)
						continue;

					const int a = 0, b = 1, c = 2, d = 3, e = 4, f = 5, g = 6, h = 7, i = 8;

					//read sequentially from memory as far as possible
					ker3._[a] = GetPixel(src, sM1, xM1);
					ker3._[b] = GetPixel(src, sM1, x);
					ker3._[c] = GetPixel(src, sM1, xP1);

					ker3._[d] = GetPixel(src, s0, xM1);
					ker3._[e] = src[s0 + x];
					ker3._[f] = GetPixel(src, s0, xP1);

					ker3._[g] = GetPixel(src, sP1, xM1);
					ker3._[h] = GetPixel(src, sP1, x);
					ker3._[i] = GetPixel(src, sP1, xP1);

					ScalePixel(scaler, (int)RotationDegree.R0, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R90, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R180, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R270, ker3, trgi, blendXy);
				}
			}
		}
	}
}
