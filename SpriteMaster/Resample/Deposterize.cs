﻿using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Colors;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Resample;

// Temporary code lifted from the PPSSPP project, deposterize.h
static class Deposterize {
	private static readonly ColorSpace CurrentColorSpace = ColorSpace.sRGB_Precise;

	private class DeposterizeContext<T> where T : unmanaged {
		private readonly Vector2I Size;
		private readonly Vector2B Wrapped;
		private readonly int Passes;
		private readonly int Threshold;
		private readonly int ThresholdSq;
		private readonly int BlockSize;

		internal DeposterizeContext(
			in Vector2I size,
			in Vector2B wrapped,
			int passes,
			int threshold,
			int blockSize
		) {
			Size = size;
			Wrapped = wrapped;
			Passes = passes;
			Threshold = threshold;
			ThresholdSq = threshold * threshold;
			BlockSize = blockSize;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private static Fixed16 TexelDiff(Fixed16 texel1, Fixed16 texel2) => (Fixed16)Math.Abs(texel1.Value - texel2.Value);

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private static double Square(double value) => value * value;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private unsafe double DistYCbCrImpl(Color16 pix1, Color16 pix2) {
			// See if the colors are the same
			if (pix1.NoAlpha == pix2.NoAlpha) {
				return 0.0;
			}

			//http://en.wikipedia.org/wiki/YCbCr#ITU-R_BT.601_conversion
			//YCbCr conversion is a matrix multiplication => take advantage of linearity by subtracting first!
			var rDiff = TexelDiff(pix1.R, pix2.R); //we may delay division by 255 to after matrix multiplication
			var gDiff = TexelDiff(pix1.G, pix2.G);
			var bDiff = TexelDiff(pix1.B, pix2.B); //subtraction for int is noticeable faster than for double

			var coefficient = CurrentColorSpace.LumaCoefficient;
			var scale = CurrentColorSpace.LumaScale;

			// TODO : integer math?
			var y = (coefficient.R * rDiff.Value) + (coefficient.G * gDiff.Value) + (coefficient.B * bDiff.Value); //[!], analog YCbCr!
			var cB = scale.B * (bDiff.Value - y);
			var cR = scale.R * (rDiff.Value - y);

			// Skip division by 255.
			// Also skip square root here by pre-squaring the config option equalColorTolerance.
			return Square(y) + ((Square(cB) + Square(cR)));
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal double DistYCbCr(Color16 pix1, Color16 pix2) {
			if (pix1 == pix2) {
				return 0.0;
			}

			var distance = DistYCbCrImpl(pix1, pix2);

			return distance;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private bool Compare(Fixed16 reference, Fixed16 lower, Fixed16 higher) {
			return
				(lower != higher) &&
				(
					(lower == reference && Math.Abs(higher.Value - reference.Value) <= Threshold) ||
					(higher == reference && Math.Abs(lower.Value - reference.Value) <= Threshold)
				);
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private Color16 Merge(Color16 reference, Color16 lower, Color16 higher) {
			Color16 result = reference;

			if (reference.A == lower.A && reference.A == higher.A) {
				bool doMerge = false;
				if (Config.Resample.Deposterization.UseYCbCr && lower != higher && (lower == reference || higher == reference)) {
					doMerge =
						(lower == reference && DistYCbCr(higher, reference) <= ThresholdSq) ||
						(higher == reference && DistYCbCr(lower, reference) <= ThresholdSq);
				}
				else {
					doMerge =
						reference.A == lower.A && reference.A == higher.A &&
						Compare(reference.R, lower.R, higher.R) &&
						Compare(reference.G, lower.G, higher.G) &&
						Compare(reference.B, lower.B, higher.B);
				}

				if (doMerge) {
					result.R = (ushort)((lower.R.Value + higher.R.Value) >> 1);
					result.G = (ushort)((lower.G.Value + higher.G.Value) >> 1);
					result.B = (ushort)((lower.B.Value + higher.B.Value) >> 1);
				}
			}

			return result;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private int GetX(int value) {
			if (Wrapped.X) {
				var result = value % Size.X;
				if (result < 0) {
					result += Size.X;
				}
				return result;
			}
			else {
				return Math.Clamp(value, 0, Size.X - 1);
			}
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private int GetY(int value) {
			if (Wrapped.Y) {
				var result = value % Size.Y;
				if (result < 0) {
					result += Size.Y;
				}
				return result;
			}
			else {
				return Math.Clamp(value, 0, Size.Y - 1);
			}
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private void DeposterizeH(ReadOnlySpan<Color16> inData, Span<Color16> outData) {
			int minY = 0;
			int maxY = Size.Height;

			int minX = -1;
			int maxX = Size.Width + 1;

			foreach (int y in minY.RangeTo(maxY)) {
				int modulusY = GetY(y);

				var yIndex = modulusY * Size.X;

				foreach (int x in minX.RangeTo(maxX)) {
					int modulusX = GetX(x);

					var index = yIndex + modulusX;

					var center = inData[index];
					var left = inData[yIndex + GetX(x - 1)];
					var right = inData[yIndex + GetX(x + 1)];

					outData[index] = Merge(center, left, right);
				}
			}
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private void DeposterizeV(ReadOnlySpan<Color16> inData, Span<Color16> outData) {
			int minY = -1;
			int maxY = Size.Height + 1;

			int minX = -(Size.X % BlockSize);
			int maxX = Size.Width + -minX;

			int minXBlock = 0;
			int maxXBlock = (maxX / BlockSize) + 1;

			foreach (int xb in minXBlock.RangeTo(maxXBlock)) {
				var min = (xb + minX) * BlockSize;
				var max = Math.Min(maxX, min + BlockSize);

				foreach (int y in minY.RangeTo(maxY)) {
					var yIndex =			GetY(y)			* Size.X;
					var yIndexPrev =	GetY(y - 1)	* Size.X;
					var yIndexNext =	GetY(y + 1)	* Size.X;

					foreach (int x in min.RangeTo(max)) {
						var modulusX = GetX(x);

						var index = yIndex + modulusX;

						var center = inData[index];
						var upper = inData[yIndexPrev + modulusX];
						var lower = inData[yIndexNext + modulusX];

						outData[index] = Merge(center, upper, lower);
					}
				}
			}
		}

		private void DeposterizeV2(ReadOnlySpan<Color16> inData, Span<Color16> outData) {
			int minY = 1;
			int maxY = Size.Height;

			int minX = -(Size.X % BlockSize);
			int maxX = Size.Width + -minX;

			int minXBlock = 0;
			int maxXBlock = (maxX / BlockSize) + 1;

			for (int xb = 0; xb < Size.X / BlockSize + 1; ++xb) {
				var min = (xb + minX) * BlockSize;
				var max = Math.Min(maxX, min + BlockSize);

				for (int y = 1; y < Size.Height; ++y) {
					var modulusY = GetY(y);
					var yIndex = modulusY * Size.X;
					var yIndexPrev = GetY(y - 1) * Size.X;
					var yIndexNext = GetY(y + 1) * Size.X;

					for (int x = xb * BlockSize; x < (xb + 1) * BlockSize && x < Size.Width; ++x) {
						var modulusX = GetX(x);

						var index = yIndex + modulusX;

						var center = inData[index];
						var upper = inData[yIndexPrev + modulusX];
						var lower = inData[yIndexNext + modulusX];

						outData[index] = Merge(center, upper, lower);
					}
				}
			}
		}

		internal Span<T> Execute(ReadOnlySpan<T> data) {
			var buffer1 = SpanExt.MakeUninitialized<T>(data.Length);
			var buffer2 = SpanExt.MakeUninitialized<T>(data.Length);

			var inData = data.Cast<T, Color16>();
			var buffer1Data = buffer1.Cast<T, Color16>();
			var buffer2Data = buffer2.Cast<T, Color16>();

			DeposterizeH(inData, buffer1Data);
			DeposterizeV(buffer1Data, buffer2Data);
			//buffer1Data.CopyTo(buffer2Data);
			for (int pass = 1; pass < Passes; ++pass) {
				DeposterizeH(buffer2Data, buffer1Data);
				DeposterizeV(buffer1Data, buffer2Data);
				//buffer1Data.CopyTo(buffer2Data);
			}

			return buffer2;
		}
	}

	/*
	[MethodImpl(Runtime.MethodImpl.Optimize)]
	internal static unsafe T[] Enhance<T>(
		T[] data,
		in Vector2I size,
		in Vector2B wrapped) where T : unmanaged {
		return Enhance<T>(data.AsFixedSpan(), size, wrapped);
	}
	*/

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Span<T> Enhance<T>(
		ReadOnlySpan<T> data,
		in Vector2I size,
		in Vector2B wrapped,
		int? passes = null,
		int? threshold = null,
		int? blockSize = null
	) where T : unmanaged {
		var context = new DeposterizeContext<T>(
			size: size,
			wrapped: wrapped,
			passes: passes ?? Config.Resample.Deposterization.Passes,
			threshold: threshold ?? Config.Resample.Deposterization.Threshold,
			blockSize: blockSize ?? Config.Resample.Deposterization.BlockSize
		);
		return context.Execute(data);
	}
}
