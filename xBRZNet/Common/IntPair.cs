using System.Runtime.CompilerServices;

namespace xBRZNet2.Common
{
	internal struct IntPair
	{
		public int I;
		public int J;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IntPair(in int i, in int j)
		{
			I = i;
			J = j;
		}
	}
}
