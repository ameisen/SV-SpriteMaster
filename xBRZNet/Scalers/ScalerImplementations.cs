using xBRZNet2.Common;

namespace xBRZNet2.Scalers
{
	internal interface IScaler
	{
		int Scale { get; }
		void BlendLineSteep(int col, OutputMatrix out_);
		void BlendLineSteepAndShallow(int col, OutputMatrix out_);
		void BlendLineShallow(int col, OutputMatrix out_);
		void BlendLineDiagonal(int col, OutputMatrix out_);
		void BlendCorner(int col, OutputMatrix out_);
	}

	internal abstract class ScalerBase
	{
		protected static void AlphaBlend(int n, int m, ref int dstRef, int col)
		{
			//assert n < 256 : "possible overflow of (col & redMask) * N";
			//assert m < 256 : "possible overflow of (col & redMask) * N + (dst & redMask) * (M - N)";
			//assert 0 < n && n < m : "0 < N && N < M";

			//this works because 8 upper bits are free
			var dst = dstRef;
			var alphaComponent = BlendComponent(Mask.Alpha, n, m, dst, col);
			var redComponent = BlendComponent(Mask.Red, n, m, dst, col);
			var greenComponent = BlendComponent(Mask.Green, n, m, dst, col);
			var blueComponent = BlendComponent(Mask.Blue, n, m, dst, col);
			var blend = (alphaComponent | redComponent | greenComponent | blueComponent);
			dstRef = (unchecked((int)blend)); // MJY: Added required cast but will throw an exception if the asserts at the top are not checked.
		}

		private static uint BlendComponent(uint mask, int n, int m, int inPixel, int setPixel)
		{
			var inChan = (uint)inPixel & mask;
			var setChan = (uint)setPixel & mask;
			var blend = setChan * n + inChan * (m - n);
			var component = (((uint)(blend / m)) & mask);
			return component;
		}
	}

	internal sealed class Scaler2X : ScalerBase, IScaler
	{
		public int Scale { get; } = 2;

		public void BlendLineShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
		}

		public void BlendLineSteep(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
		}

		public void BlendLineSteepAndShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(0, 1), col);
			AlphaBlend(5, 6, ref out_.Ref(1, 1), col); //[!] fixes 7/8 used in xBR
		}

		public void BlendLineDiagonal(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 2, ref out_.Ref(1, 1), col);
		}

		public void BlendCorner(int col, OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(21, 100, ref out_.Ref(1, 1), col); //exact: 1 - pi/4 = 0.2146018366
		}
	}

	internal sealed class Scaler3X : ScalerBase, IScaler
	{
		public int Scale { get; } = 3;

		public void BlendLineShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			out_.Set(Scale - 1, 2, col);
		}

		public void BlendLineSteep(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			out_.Set(2, Scale - 1, col);
		}

		public void BlendLineSteepAndShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(2, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(0, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(2, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(1, 2), col);
			out_.Set(2, 2, col);
		}

		public void BlendLineDiagonal(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 8, ref out_.Ref(1, 2), col);
			AlphaBlend(1, 8, ref out_.Ref(2, 1), col);
			AlphaBlend(7, 8, ref out_.Ref(2, 2), col);
		}

		public void BlendCorner(int col, OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(45, 100, ref out_.Ref(2, 2), col); //exact: 0.4545939598
																										//alphaBlend(14, 1000, out.ref(2, 1), col); //0.01413008627 -> negligable
																										//alphaBlend(14, 1000, out.ref(1, 2), col); //0.01413008627
		}
	}

	internal sealed class Scaler4X : ScalerBase, IScaler
	{
		public int Scale { get; } = 4;

		public void BlendLineShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 2, 3), col);
			out_.Set(Scale - 1, 2, col);
			out_.Set(Scale - 1, 3, col);
		}

		public void BlendLineSteep(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			AlphaBlend(3, 4, ref out_.Ref(3, Scale - 2), col);
			out_.Set(2, Scale - 1, col);
			out_.Set(3, Scale - 1, col);
		}

		public void BlendLineSteepAndShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(3, 4, ref out_.Ref(3, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(1, 3), col);
			AlphaBlend(1, 4, ref out_.Ref(3, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(0, 3), col);
			AlphaBlend(1, 3, ref out_.Ref(2, 2), col); //[!] fixes 1/4 used in xBR
			out_.Set(3, 3, col);
			out_.Set(3, 2, col);
			out_.Set(2, 3, col);
		}

		public void BlendLineDiagonal(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 2, ref out_.Ref(Scale - 1, Scale / 2), col);
			AlphaBlend(1, 2, ref out_.Ref(Scale - 2, Scale / 2 + 1), col);
			out_.Set(Scale - 1, Scale - 1, col);
		}

		public void BlendCorner(int col, OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(68, 100, ref out_.Ref(3, 3), col); //exact: 0.6848532563
			AlphaBlend(9, 100, ref out_.Ref(3, 2), col); //0.08677704501
			AlphaBlend(9, 100, ref out_.Ref(2, 3), col); //0.08677704501
		}
	}

	internal sealed class Scaler5X : ScalerBase, IScaler
	{
		public int Scale { get; } = 5;

		public void BlendLineShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 3, 4), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 2, 3), col);
			out_.Set(Scale - 1, 2, col);
			out_.Set(Scale - 1, 3, col);
			out_.Set(Scale - 1, 4, col);
			out_.Set(Scale - 2, 4, col);
		}

		public void BlendLineSteep(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(1, 4, ref out_.Ref(4, Scale - 3), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			AlphaBlend(3, 4, ref out_.Ref(3, Scale - 2), col);
			out_.Set(2, Scale - 1, col);
			out_.Set(3, Scale - 1, col);
			out_.Set(4, Scale - 1, col);
			out_.Set(4, Scale - 2, col);
		}

		public void BlendLineSteepAndShallow(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			out_.Set(2, Scale - 1, col);
			out_.Set(3, Scale - 1, col);
			out_.Set(Scale - 1, 2, col);
			out_.Set(Scale - 1, 3, col);
			out_.Set(4, Scale - 1, col);
			AlphaBlend(2, 3, ref out_.Ref(3, 3), col);
		}

		public void BlendLineDiagonal(int col, OutputMatrix out_)
		{
			AlphaBlend(1, 8, ref out_.Ref(Scale - 1, Scale / 2), col);
			AlphaBlend(1, 8, ref out_.Ref(Scale - 2, Scale / 2 + 1), col);
			AlphaBlend(1, 8, ref out_.Ref(Scale - 3, Scale / 2 + 2), col);
			AlphaBlend(7, 8, ref out_.Ref(4, 3), col);
			AlphaBlend(7, 8, ref out_.Ref(3, 4), col);
			out_.Set(4, 4, col);
		}

		public void BlendCorner(int col, OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(86, 100, ref out_.Ref(4, 4), col); //exact: 0.8631434088
			AlphaBlend(23, 100, ref out_.Ref(4, 3), col); //0.2306749731
			AlphaBlend(23, 100, ref out_.Ref(3, 4), col); //0.2306749731
																										//alphaBlend(8, 1000, out.ref(4, 2), col); //0.008384061834 -> negligable
																										//alphaBlend(8, 1000, out.ref(2, 4), col); //0.008384061834
		}
	}
}
