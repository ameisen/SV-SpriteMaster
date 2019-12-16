using System;
using System.Runtime.CompilerServices;

namespace xBRZNet2
{
	public readonly struct Config : IEquatable<Config>
	{
		internal readonly bool WrappedX;
		internal readonly bool WrappedY;

		// These are the default values:
		internal readonly double LuminanceWeight;
		internal readonly double EqualColorTolerance;
		internal readonly double DominantDirectionThreshold;
		internal readonly double SteepDirectionThreshold;

		// Precalculated
		internal readonly double EqualColorTolerancePow2;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Config(
			bool wrappedX = false,
			bool wrappedY = false,
			double luminanceWeight = 1.0,
			double equalColorTolerance = 30.0,
			double dominantDirectionThreshold = 3.6,
			double steepDirectionThreshold = 2.2
		)
		{
			WrappedX = wrappedX;
			WrappedY = wrappedY;
			LuminanceWeight = luminanceWeight;
			EqualColorTolerance = equalColorTolerance;
			DominantDirectionThreshold = dominantDirectionThreshold;
			SteepDirectionThreshold = steepDirectionThreshold;

			EqualColorTolerancePow2 = Math.Pow(EqualColorTolerance, 2.0);
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is Config other)
			{
				return Equals(other);
			}
			return false;
		}

		public override readonly int GetHashCode()
		{
			int hash = 0;
			foreach (var field in typeof(Config).GetFields())
			{
				hash ^= field.GetValue(this).GetHashCode();
			}
			return hash;
		}

		public static bool operator ==(in Config left, in Config right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(in Config left, in Config right)
		{
			return !left.Equals(right);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception Ignored")]
		public readonly bool Equals(Config other)
		{
			try
			{
				foreach (var field in typeof(Config).GetFields())
				{
					var leftField = field.GetValue(this);
					var rightField = field.GetValue(other);
					// TODO possibly fall back on IComparable
					if (!leftField.Equals(rightField))
					{
						return false;
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
