using System.Linq;
using System.Runtime.CompilerServices;
using xBRZNet.Common;

namespace xBRZNet2.Scalers
{
	internal static class Rotator
	{
		public const int MaxRotations = 4; // Number of 90 degree rotations
		public const int MaxPositions = 9;

		// Cache the 4 rotations of the 9 positions, a to i.
		// a = 0, b = 1, c = 2,
		// d = 3, e = 4, f = 5,
		// g = 6, h = 7, i = 8;
		public static readonly int[] _ = new int[MaxRotations * MaxPositions];

		static Rotator()
		{
			var rotation = Enumerable.Range(0, MaxPositions).ToArray();
			var sideLength = IMath.Sqrt(MaxPositions);
			for (var rot = 0; rot < MaxRotations; rot++)
			{
				for (var pos = 0; pos < MaxPositions; pos++)
				{
					_[(pos * MaxRotations) + rot] = rotation[pos];
				}
				rotation = rotation.RotateClockwise(sideLength);
			}
		}

		//http://stackoverflow.com/a/38964502/294804
		private static int[] RotateClockwise(this int[] square1DMatrix, in int sideLength)
		{
			var size = sideLength;
			var result = new int[square1DMatrix.Length];

			for (var i = 0; i < size; ++i)
			{
				var offset = i * size;
				for (var j = 0; j < size; ++j)
				{
					result[offset + j] = square1DMatrix[(size - j - 1) * size + i];
				}
			}

			return result;
		}
	}
}
