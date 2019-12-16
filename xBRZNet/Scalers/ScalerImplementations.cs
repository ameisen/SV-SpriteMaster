using System.Runtime.CompilerServices;
using xBRZNet2.Common;

namespace xBRZNet2.Scalers
{
	internal abstract class IScaler
	{
		public readonly int Scale;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected IScaler(in int scale)
		{
			Scale = scale;
		}

		public abstract void BlendLineSteep(in int col, in OutputMatrix out_);
		public abstract void BlendLineSteepAndShallow(in int col, in OutputMatrix out_);
		public abstract void BlendLineShallow(in int col, in OutputMatrix out_);
		public abstract void BlendLineDiagonal(in int col, in OutputMatrix out_);
		public abstract void BlendCorner(in int col, in OutputMatrix out_);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static void AlphaBlend(in int n, in int m, ref int dstRef, in int col)
		{
			//assert n < 256 : "possible overflow of (col & redMask) * N";
			//assert m < 256 : "possible overflow of (col & redMask) * N + (dst & redMask) * (M - N)";
			//assert 0 < n && n < m : "0 < N && N < M";

			//this works because 8 upper bits are free
			var dst = dstRef;
			var alphaComponent = BlendComponent(ColorConstant.Mask.Alpha, n, m, dst, col);
			var redComponent = BlendComponent(ColorConstant.Mask.Red, n, m, dst, col);
			var greenComponent = BlendComponent(ColorConstant.Mask.Green, n, m, dst, col);
			var blueComponent = BlendComponent(ColorConstant.Mask.Blue, n, m, dst, col);
			var blend = (alphaComponent | redComponent | greenComponent | blueComponent);
			dstRef = unchecked((int)blend); // MJY: Added required cast but will throw an exception if the asserts at the top are not checked.
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint BlendComponent(in uint mask, in int n, in int m, in int inPixel, in int setPixel)
		{
			var inChan = unchecked((uint)inPixel) & mask;
			var setChan = unchecked((uint)setPixel) & mask;
			var blend = setChan * n + inChan * (m - n);
			var component = unchecked(((uint)(blend / m)) & mask);
			return component;
		}
	}

	internal sealed class Scaler2X : IScaler
	{
		public new const int Scale = 2;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Scaler2X() : base(Scale) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineShallow(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteep(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteepAndShallow(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(0, 1), col);
			AlphaBlend(5, 6, ref out_.Ref(1, 1), col); //[!] fixes 7/8 used in xBR
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineDiagonal(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 2, ref out_.Ref(1, 1), col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendCorner(in int col, in OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(21, 100, ref out_.Ref(1, 1), col); //exact: 1 - pi/4 = 0.2146018366
		}
	}

	internal sealed class Scaler3X : IScaler
	{
		public new const int Scale = 3;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Scaler3X() : base(Scale) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineShallow(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			out_.Set(Scale - 1, 2, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteep(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			out_.Set(2, Scale - 1, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteepAndShallow(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(2, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(0, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(2, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(1, 2), col);
			out_.Set(2, 2, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineDiagonal(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 8, ref out_.Ref(1, 2), col);
			AlphaBlend(1, 8, ref out_.Ref(2, 1), col);
			AlphaBlend(7, 8, ref out_.Ref(2, 2), col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendCorner(in int col, in OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(45, 100, ref out_.Ref(2, 2), col); //exact: 0.4545939598
																										//alphaBlend(14, 1000, out.ref(2, 1), col); //0.01413008627 -> negligable
																										//alphaBlend(14, 1000, out.ref(1, 2), col); //0.01413008627
		}
	}

	internal sealed class Scaler4X : IScaler
	{
		public new const int Scale = 4;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Scaler4X() : base(Scale) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineShallow(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(Scale - 1, 0), col);
			AlphaBlend(1, 4, ref out_.Ref(Scale - 2, 2), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 1, 1), col);
			AlphaBlend(3, 4, ref out_.Ref(Scale - 2, 3), col);
			out_.Set(Scale - 1, 2, col);
			out_.Set(Scale - 1, 3, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteep(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 4, ref out_.Ref(0, Scale - 1), col);
			AlphaBlend(1, 4, ref out_.Ref(2, Scale - 2), col);
			AlphaBlend(3, 4, ref out_.Ref(1, Scale - 1), col);
			AlphaBlend(3, 4, ref out_.Ref(3, Scale - 2), col);
			out_.Set(2, Scale - 1, col);
			out_.Set(3, Scale - 1, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteepAndShallow(in int col, in OutputMatrix out_)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineDiagonal(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 2, ref out_.Ref(Scale - 1, Scale / 2), col);
			AlphaBlend(1, 2, ref out_.Ref(Scale - 2, Scale / 2 + 1), col);
			out_.Set(Scale - 1, Scale - 1, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendCorner(in int col, in OutputMatrix out_)
		{
			//model a round corner
			AlphaBlend(68, 100, ref out_.Ref(3, 3), col); //exact: 0.6848532563
			AlphaBlend(9, 100, ref out_.Ref(3, 2), col); //0.08677704501
			AlphaBlend(9, 100, ref out_.Ref(2, 3), col); //0.08677704501
		}
	}

	internal sealed class Scaler5X : IScaler
	{
		public new const int Scale = 5;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Scaler5X() : base(Scale) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineShallow(in int col, in OutputMatrix out_)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteep(in int col, in OutputMatrix out_)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineSteepAndShallow(in int col, in OutputMatrix out_)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendLineDiagonal(in int col, in OutputMatrix out_)
		{
			AlphaBlend(1, 8, ref out_.Ref(Scale - 1, Scale / 2), col);
			AlphaBlend(1, 8, ref out_.Ref(Scale - 2, Scale / 2 + 1), col);
			AlphaBlend(1, 8, ref out_.Ref(Scale - 3, Scale / 2 + 2), col);
			AlphaBlend(7, 8, ref out_.Ref(4, 3), col);
			AlphaBlend(7, 8, ref out_.Ref(3, 4), col);
			out_.Set(4, 4, col);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override void BlendCorner(in int col, in OutputMatrix out_)
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
