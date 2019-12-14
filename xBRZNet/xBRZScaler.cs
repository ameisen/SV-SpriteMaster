using System;
using System.Drawing;
using xBRZNet.Blend;
using xBRZNet.Color;
using xBRZNet.Common;
using xBRZNet.Scalers;

namespace xBRZNet
{
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
	public class xBRZScaler
	{
		// scaleSize = 2 to 5
		
		public xBRZScaler(
			in int scaleMultiplier,
			in Span<int> sourceData,
			in int sourceWidth,
			in int sourceHeight,
			in Rectangle? sourceTarget,
			int[] targetData,
			in ScalerConfiguration configuration
		)
		{
			if (scaleMultiplier < 2 || scaleMultiplier > 5)
			{
				throw new ArgumentOutOfRangeException(nameof(scaleMultiplier));
			}
			if (sourceData == null)
			{
				throw new ArgumentNullException(nameof(sourceData));
			}
			if (targetData == null)
			{
				throw new ArgumentNullException(nameof(targetData));
			}
			if (sourceWidth <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceWidth));
			}
			if (sourceHeight <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceHeight));
			}
			if (sourceWidth * sourceHeight > sourceData.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceData));
			}
			if (sourceTarget == null || !sourceTarget.HasValue)
			{
				this.sourceTarget = new Rectangle(0, 0, sourceWidth, sourceHeight);
			}
			else
			{
				this.sourceTarget = sourceTarget.Value;
			}
			if (this.sourceTarget.Right > sourceWidth || this.sourceTarget.Bottom > sourceHeight)
			{
				throw new ArgumentOutOfRangeException(nameof(sourceTarget));
			}
			this.targetWidth = this.sourceTarget.Width * scaleMultiplier;
			this.targetHeight = this.sourceTarget.Height * scaleMultiplier;
			if (targetWidth * targetHeight > targetData.Length)
			{
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

		private readonly ScalerConfiguration configuration;
		private readonly IScaler scaler;
		private OutputMatrix outputMatrix;
		private readonly BlendResult blendResult = new BlendResult();

		private readonly ColorDist ColorDistance;
		private readonly ColorEq ColorEqualizer;

		private readonly int sourceWidth;
		private readonly int sourceHeight;
		private readonly Rectangle sourceTarget;
		private readonly int targetWidth;
		private readonly int targetHeight;

		//fill block with the given color
		private static void FillBlock(int[] trg, int trgi, int pitch, int col, int blockSize)
		{
			for (var y = 0; y < blockSize; ++y, trgi += pitch)
			{
				for (var x = 0; x < blockSize; ++x)
				{
					trg[trgi + x] = col;
				}
			}
		}

		//detect blend direction
		private void PreProcessCorners(Kernel4x4 ker)
		{
			blendResult.Reset();

			if ((ker.F == ker.G && ker.J == ker.K) || (ker.F == ker.J && ker.G == ker.K)) return;

			var dist = ColorDistance;

			const int weight = 4;
			var jg = dist.DistYCbCr(ker.I, ker.F) + dist.DistYCbCr(ker.F, ker.C) + dist.DistYCbCr(ker.N, ker.K) + dist.DistYCbCr(ker.K, ker.H) + weight * dist.DistYCbCr(ker.J, ker.G);
			var fk = dist.DistYCbCr(ker.E, ker.J) + dist.DistYCbCr(ker.J, ker.O) + dist.DistYCbCr(ker.B, ker.G) + dist.DistYCbCr(ker.G, ker.L) + weight * dist.DistYCbCr(ker.F, ker.K);

			if (jg < fk)
			{
				var dominantGradient = (char)((configuration.DominantDirectionThreshold * jg < fk) ? BlendType.Dominant : BlendType.Normal);
				if (ker.F != ker.G && ker.F != ker.J)
				{
					blendResult.F = dominantGradient;
				}
				if (ker.K != ker.J && ker.K != ker.G)
				{
					blendResult.K = dominantGradient;
				}
			}
			else if (fk < jg)
			{
				var dominantGradient = (char)((configuration.DominantDirectionThreshold * fk < jg) ? BlendType.Dominant : BlendType.Normal);
				if (ker.J != ker.F && ker.J != ker.K)
				{
					blendResult.J = dominantGradient;
				}
				if (ker.G != ker.F && ker.G != ker.K)
				{
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
		private void ScalePixel(IScaler scaler, int rotDeg, Kernel3x3 ker, int trgi, char blendInfo)
		{
			var blend = blendInfo.Rotate((RotationDegree)rotDeg);

			if ((BlendType)blend.GetBottomR() == BlendType.None) return;

			// int a = ker._[Rot._[(0 << 2) + rotDeg]];
			var b = ker._[Rot._[(1 << 2) + rotDeg]];
			var c = ker._[Rot._[(2 << 2) + rotDeg]];
			var d = ker._[Rot._[(3 << 2) + rotDeg]];
			var e = ker._[Rot._[(4 << 2) + rotDeg]];
			var f = ker._[Rot._[(5 << 2) + rotDeg]];
			var g = ker._[Rot._[(6 << 2) + rotDeg]];
			var h = ker._[Rot._[(7 << 2) + rotDeg]];
			var i = ker._[Rot._[(8 << 2) + rotDeg]];

			var eq = ColorEqualizer;
			var dist = ColorDistance;

			bool doLineBlend;

			if (blend.GetBottomR() >= (char)BlendType.Dominant)
			{
				doLineBlend = true;
			}
			//make sure there is no second blending in an adjacent
			//rotation for this pixel: handles insular pixels, mario eyes
			//but support double-blending for 90� corners
			else if (blend.GetTopR() != (char)BlendType.None && !eq.IsColorEqual(e, g))
			{
				doLineBlend = false;
			}
			else if (blend.GetBottomL() != (char)BlendType.None && !eq.IsColorEqual(e, c))
			{
				doLineBlend = false;
			}
			//no full blending for L-shapes; blend corner only (handles "mario mushroom eyes")
			else if (eq.IsColorEqual(g, h) && eq.IsColorEqual(h, i) && eq.IsColorEqual(i, f) && eq.IsColorEqual(f, c) && !eq.IsColorEqual(e, i))
			{
				doLineBlend = false;
			}
			else
			{
				doLineBlend = true;
			}

			//choose most similar color
			var px = dist.DistYCbCr(e, f) <= dist.DistYCbCr(e, h) ? f : h;

			var out_ = outputMatrix;
			out_.Move(rotDeg, trgi);

			if (!doLineBlend)
			{
				scaler.BlendCorner(px, out_);
				return;
			}

			//test sample: 70% of values max(fg, hc) / min(fg, hc)
			//are between 1.1 and 3.7 with median being 1.9
			var fg = dist.DistYCbCr(f, g);
			var hc = dist.DistYCbCr(h, c);

			var haveShallowLine = configuration.SteepDirectionThreshold * fg <= hc && e != g && d != g;
			var haveSteepLine = configuration.SteepDirectionThreshold * hc <= fg && e != c && b != c;

			if (haveShallowLine)
			{
				if (haveSteepLine)
				{
					scaler.BlendLineSteepAndShallow(px, out_);
				}
				else
				{
					scaler.BlendLineShallow(px, out_);
				}
			}
			else
			{
				if (haveSteepLine)
				{
					scaler.BlendLineSteep(px, out_);
				}
				else
				{
					scaler.BlendLineDiagonal(px, out_);
				}
			}
		}

		private static int clamp(int value, int reference, bool wrap)
		{
			if (wrap)
			{
				return (value + reference) % reference;
			}
			else
			{
				return Math.Min(Math.Max(value, 0), reference - 1);
			}
		}

		private int clampX(int x)
		{
			x -= sourceTarget.Left;
			if (configuration.WrappedX)
			{
				x = (x + sourceTarget.Width) % sourceTarget.Width;
			}
			else
			{
				x = Math.Min(Math.Max(x, 0), sourceTarget.Width - 1);
			}
			return x + sourceTarget.Left;
		}

		private int clampY(int y)
		{
			y -= sourceTarget.Top;
			if (configuration.WrappedY)
			{
				y = (y + sourceTarget.Height) % sourceTarget.Height;
			}
			else
			{
				y = Math.Min(Math.Max(y, 0), sourceTarget.Height - 1);
			}
			return y + sourceTarget.Top;
		}

		//scaler policy: see "Scaler2x" reference implementation
		private void Scale(in Span<int> src, int[] trg)
		{
			int yFirst = sourceTarget.Top;
			int yLast = sourceTarget.Bottom;

			if (yFirst >= yLast) return;

			var trgWidth = targetWidth;

			//temporary buffer for "on the fly preprocessing"
			var preProcBuffer = new char[sourceTarget.Width];

			var ker4 = new Kernel4x4();

			//initialize preprocessing buffer for first row:
			//detect upper left and right corner blending
			//this cannot be optimized for adjacent processing
			//stripes; we must not allow for a memory race condition!
			if (yFirst > 0)
			{
				var y = yFirst - 1;

				var sM1 = sourceWidth * clampY(y - 1);
				var s0 = sourceWidth * y; //center line
				var sP1 = sourceWidth * clampY(y + 1);
				var sP2 = sourceWidth * clampY(y + 2);

				for (var x = sourceTarget.Left; x < sourceTarget.Right; ++x)
				{
					var xM1 = clampX(x - 1);
					var xP1 = clampX(x + 1);
					var xP2 = clampX(x + 2);

					//read sequentially from memory as far as possible
					ker4.A = src[sM1 + xM1];
					ker4.B = src[sM1 + x];
					ker4.C = src[sM1 + xP1];
					ker4.D = src[sM1 + xP2];

					ker4.E = src[s0 + xM1];
					ker4.F = src[s0 + x];
					ker4.G = src[s0 + xP1];
					ker4.H = src[s0 + xP2];

					ker4.I = src[sP1 + xM1];
					ker4.J = src[sP1 + x];
					ker4.K = src[sP1 + xP1];
					ker4.L = src[sP1 + xP2];

					ker4.M = src[sP2 + xM1];
					ker4.N = src[sP2 + x];
					ker4.O = src[sP2 + xP1];
					ker4.P = src[sP2 + xP2];

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

					if (x + 1 < sourceTarget.Right)
					{
						preProcBuffer[adjustedX + 1] = preProcBuffer[adjustedX + 1].SetTopL(blendResult.K);
					}
					else if (configuration.WrappedX)
					{
						preProcBuffer[0] = preProcBuffer[0].SetTopL(blendResult.K);
					}
				}
			}

			outputMatrix = new OutputMatrix(scaler.Scale, trg, trgWidth);

			var ker3 = new Kernel3x3();

			for (var y = yFirst; y < yLast; ++y)
			{
				//consider MT "striped" access
				var trgi = scaler.Scale * (y - yFirst) * trgWidth;

				var sM1 = sourceWidth * clampY(y - 1);
				var s0 = sourceWidth * y; //center line
				var sP1 = sourceWidth * clampY(y + 1);
				var sP2 = sourceWidth * clampY(y + 2);

				var blendXy1 = (char)0;

				for (var x = sourceTarget.Left; x < sourceTarget.Right; ++x, trgi += scaler.Scale)
				{
					var xM1 = clampX(x - 1);
					var xP1 = clampX(x + 1);
					var xP2 = clampX(x + 2);

					//evaluate the four corners on bottom-right of current pixel
					//blend_xy for current (x, y) position

					//read sequentially from memory as far as possible
					ker4.A = src[sM1 + xM1];
					ker4.B = src[sM1 + x];
					ker4.C = src[sM1 + xP1];
					ker4.D = src[sM1 + xP2];

					ker4.E = src[s0 + xM1];
					ker4.F = src[s0 + x];
					ker4.G = src[s0 + xP1];
					ker4.H = src[s0 + xP2];

					ker4.I = src[sP1 + xM1];
					ker4.J = src[sP1 + x];
					ker4.K = src[sP1 + xP1];
					ker4.L = src[sP1 + xP2];

					ker4.M = src[sP2 + xM1];
					ker4.N = src[sP2 + x];
					ker4.O = src[sP2 + xP1];
					ker4.P = src[sP2 + xP2];

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

					if (x + 1 < sourceTarget.Right)
					{
						//set 3rd known corner for (x + 1, y)
						preProcBuffer[adjustedX + 1] = preProcBuffer[adjustedX + 1].SetBottomL(blendResult.G);
					}
					else if (configuration.WrappedX)
					{
						preProcBuffer[0] = preProcBuffer[0].SetBottomL(blendResult.G);
					}

					//fill block of size scale * scale with the given color
					//  //place *after* preprocessing step, to not overwrite the
					//  //results while processing the the last pixel!
					FillBlock(trg, trgi, trgWidth, src[s0 + x], scaler.Scale);

					//blend four corners of current pixel
					if (blendXy == 0) continue;

					const int a = 0, b = 1, c = 2, d = 3, e = 4, f = 5, g = 6, h = 7, i = 8;

					//read sequentially from memory as far as possible
					ker3._[a] = src[sM1 + xM1];
					ker3._[b] = src[sM1 + x];
					ker3._[c] = src[sM1 + xP1];

					ker3._[d] = src[s0 + xM1];
					ker3._[e] = src[s0 + x];
					ker3._[f] = src[s0 + xP1];

					ker3._[g] = src[sP1 + xM1];
					ker3._[h] = src[sP1 + x];
					ker3._[i] = src[sP1 + xP1];

					ScalePixel(scaler, (int)RotationDegree.R0, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R90, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R180, ker3, trgi, blendXy);
					ScalePixel(scaler, (int)RotationDegree.R270, ker3, trgi, blendXy);
				}
			}
		}
	}
}
